# Script para desinstalar o Agente PDV como Servico Windows
# IMPORTANTE: Execute como Administrador

Write-Host "=== DESINSTALAR SERVICO AGENTE PDV ===" -ForegroundColor Cyan
Write-Host ""

# Verificar se esta rodando como Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERRO: Este script precisa ser executado como Administrador!" -ForegroundColor Red
    Write-Host "Clique com botao direito no PowerShell e selecione 'Executar como Administrador'" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

$serviceName = "SolisAgentePDV"

# Verificar se o servico existe
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "AVISO: Servico '$serviceName' nao encontrado." -ForegroundColor Yellow
    Write-Host "O servico pode ja ter sido removido." -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

Write-Host "[INFO] Servico encontrado:" -ForegroundColor Yellow
Write-Host "  Nome: $($service.Name)"
Write-Host "  Status: $($service.Status)"
Write-Host ""

# Parar o servico se estiver rodando
if ($service.Status -eq 'Running') {
    Write-Host "[INFO] Parando servico..." -ForegroundColor Yellow
    try {
        Stop-Service -Name $serviceName -Force -ErrorAction Stop
        Write-Host "[OK] Servico parado com sucesso!" -ForegroundColor Green
    } catch {
        Write-Host "ERRO: Falha ao parar o servico: $_" -ForegroundColor Red
        Write-Host "Tentando forcar a parada..." -ForegroundColor Yellow
        sc.exe stop $serviceName
    }
    Start-Sleep -Seconds 3
}

Write-Host ""
Write-Host "[INFO] Removendo servico..." -ForegroundColor Yellow

# Remover o servico
$result = sc.exe delete $serviceName

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Servico removido com sucesso!" -ForegroundColor Green
} else {
    Write-Host "ERRO: Falha ao remover o servico!" -ForegroundColor Red
    Write-Host $result
    exit 1
}

Write-Host ""
Write-Host "=== LIMPEZA OPCIONAL ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "O servico foi desinstalado, mas os arquivos do agente ainda estao em:" -ForegroundColor Yellow
Write-Host "  C:\Solis\AgentePDV" -ForegroundColor White
Write-Host ""

$removeFiles = Read-Host "Deseja remover TODOS os arquivos do agente? (S/N)"

if ($removeFiles -eq 'S' -or $removeFiles -eq 's') {
    Write-Host ""
    $keepData = Read-Host "Deseja manter os dados (banco de dados e configuracao)? (S/N)"
    
    if ($keepData -eq 'S' -or $keepData -eq 's') {
        Write-Host ""
        Write-Host "[INFO] Criando backup dos dados..." -ForegroundColor Yellow
        
        $backupPath = "$env:USERPROFILE\Desktop\SolisAgentePDV_Backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
        
        Copy-Item "C:\Solis\AgentePDV\data\*" -Destination $backupPath -Recurse -Force
        
        Write-Host "[OK] Backup criado em: $backupPath" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "[INFO] Removendo arquivos do agente..." -ForegroundColor Yellow
    Remove-Item "C:\Solis\AgentePDV" -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "[OK] Arquivos removidos!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[INFO] Arquivos mantidos em C:\Solis\AgentePDV" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Para remover manualmente:" -ForegroundColor Cyan
    Write-Host "  Remove-Item 'C:\Solis\AgentePDV' -Recurse -Force" -ForegroundColor White
}

Write-Host ""
Write-Host "[OK] Desinstalacao concluida!" -ForegroundColor Green
Write-Host ""
