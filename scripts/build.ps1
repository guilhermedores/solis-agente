# Script para compilar o Solis Agente PDV em arquivo unico

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "`n$Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERRO] $Message" -ForegroundColor Red
}

Clear-Host
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "        SOLIS - AGENTE PDV" -ForegroundColor Cyan
Write-Host "        Build Script" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

Write-Step "Verificando .NET SDK..."
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   .NET SDK: $dotnetVersion" -ForegroundColor Gray
        Write-Success ".NET SDK encontrado"
    } else {
        Write-Error ".NET SDK nao instalado!"
        Write-Host "   Baixe em: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Error ".NET SDK nao instalado!"
    exit 1
}

$scriptDir = Split-Path -Parent $PSCommandPath
$projectPath = Split-Path -Parent $scriptDir
$csprojPath = Join-Path $projectPath "Solis.AgentePDV.csproj"

if (-not (Test-Path $csprojPath)) {
    Write-Error "Projeto nao encontrado: $csprojPath"
    exit 1
}

Write-Success "Projeto encontrado: $csprojPath"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectPath "bin\$Configuration\net8.0\win-x64\publish"
}

Write-Step "Limpando builds anteriores..."
$cleanOutput = dotnet clean $csprojPath -c $Configuration 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Success "Build limpo"
} else {
    Write-Warning "Falha ao limpar: $cleanOutput"
}

Write-Step "Compilando aplicacao..."
Write-Host "   Configuracao: $Configuration" -ForegroundColor Gray
Write-Host "   Plataforma: Windows x64" -ForegroundColor Gray
Write-Host "   Modo: Self-contained (arquivo unico)" -ForegroundColor Gray
Write-Host ""

$publishOutput = dotnet publish $csprojPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $OutputPath `
    2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao!"
    Write-Host $publishOutput -ForegroundColor Red
    exit 1
}

Write-Success "Compilacao concluida!"

Write-Step "Verificando arquivos gerados..."
$exePath = Join-Path $OutputPath "Solis.AgentePDV.exe"
if (Test-Path $exePath) {
    $exeSize = (Get-Item $exePath).Length
    $exeSizeMB = [math]::Round($exeSize / 1MB, 2)
    Write-Success "Executavel gerado: $exePath"
    Write-Host "   Tamanho: $exeSizeMB MB" -ForegroundColor Gray
    
    # Listar outros arquivos importantes
    $files = Get-ChildItem $OutputPath -File | Where-Object { $_.Extension -ne '.pdb' }
    Write-Host "`n   Arquivos na pasta publish:" -ForegroundColor Gray
    foreach ($file in $files) {
        $sizeMB = [math]::Round($file.Length / 1MB, 2)
        Write-Host "     - $($file.Name) ($sizeMB MB)" -ForegroundColor DarkGray
    }
    
    # Verificar pasta src
    $srcPath = Join-Path $OutputPath "src"
    if (Test-Path $srcPath) {
        Write-Host "     - src\ (pasta de configuracao)" -ForegroundColor DarkGray
    }
} else {
    Write-Error "Executavel nao encontrado!"
    exit 1
}

Write-Host "`n===============================================" -ForegroundColor Green
Write-Host "  BUILD CONCLUIDO!" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host "`nArquivos gerados em:" -ForegroundColor Cyan
Write-Host "   $OutputPath" -ForegroundColor White
Write-Host "`nProximos passos:" -ForegroundColor Cyan
Write-Host "   1. Testar localmente: .\Solis.AgentePDV.exe" -ForegroundColor Gray
Write-Host "   2. Instalar como servico: .\scripts\instalar-agente-precompilado.ps1" -ForegroundColor Gray
Write-Host "   3. Distribuir: Copie a pasta 'publish' para outras maquinas" -ForegroundColor Gray
