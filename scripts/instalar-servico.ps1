# Script para instalar o Agente PDV como Servico Windows
# IMPORTANTE: Execute como Administrador

Write-Host "=== INSTALAR AGENTE PDV COMO SERVICO WINDOWS ===" -ForegroundColor Cyan
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
$serviceDisplayName = "Solis Agente PDV"
$serviceDescription = "Agente local do Solis PDV para sincronizacao e gerenciamento de vendas offline"
$executablePath = "C:\Solis\AgentePDV\Solis.AgentePDV.exe"

# Verificar se o executavel existe
if (-not (Test-Path $executablePath)) {
    Write-Host "ERRO: Executavel nao encontrado em: $executablePath" -ForegroundColor Red
    Write-Host "Execute primeiro o script de instalacao (instalar-agente.ps1)" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "[INFO] Verificando se o servico ja existe..." -ForegroundColor Yellow

# Verificar se o servico ja existe
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "[INFO] Servico ja existe. Parando e removendo..." -ForegroundColor Yellow
    
    # Parar o servico se estiver rodando
    if ($existingService.Status -eq 'Running') {
        Write-Host "[INFO] Parando servico..." -ForegroundColor Yellow
        Stop-Service -Name $serviceName -Force
        Start-Sleep -Seconds 2
    }
    
    # Remover o servico
    Write-Host "[INFO] Removendo servico existente..." -ForegroundColor Yellow
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
}

Write-Host ""
Write-Host "[INFO] Criando servico Windows..." -ForegroundColor Yellow

# Criar o servico
$createResult = sc.exe create $serviceName `
    binPath= $executablePath `
    start= auto `
    DisplayName= $serviceDisplayName

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha ao criar o servico!" -ForegroundColor Red
    Write-Host $createResult
    exit 1
}

# Configurar descricao
sc.exe description $serviceName $serviceDescription

# Configurar recuperacao em caso de falha
Write-Host "[INFO] Configurando recuperacao automatica..." -ForegroundColor Yellow
sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

Write-Host ""
Write-Host "[OK] Servico criado com sucesso!" -ForegroundColor Green
Write-Host ""

# Perguntar se deseja iniciar o servico
$start = Read-Host "Deseja iniciar o servico agora? (S/N)"

if ($start -eq 'S' -or $start -eq 's') {
    Write-Host ""
    Write-Host "[INFO] Iniciando servico..." -ForegroundColor Yellow
    
    Start-Service -Name $serviceName
    Start-Sleep -Seconds 3
    
    $service = Get-Service -Name $serviceName
    
    if ($service.Status -eq 'Running') {
        Write-Host ""
        Write-Host "[OK] Servico iniciado com sucesso!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Status do servico:" -ForegroundColor Cyan
        Write-Host "  Nome: $($service.Name)"
        Write-Host "  Nome de exibicao: $($service.DisplayName)"
        Write-Host "  Status: $($service.Status)"
        Write-Host "  Tipo de inicio: Automatico"
        Write-Host ""
        Write-Host "O servico sera iniciado automaticamente na proxima inicializacao do Windows." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "AVISO: O servico foi criado mas nao esta rodando." -ForegroundColor Yellow
        Write-Host "Status: $($service.Status)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Para ver os logs de erro:" -ForegroundColor Cyan
        Write-Host "  Get-EventLog -LogName Application -Source $serviceName -Newest 10" -ForegroundColor White
    }
} else {
    Write-Host ""
    Write-Host "[INFO] Servico criado mas nao iniciado." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Para iniciar o servico manualmente:" -ForegroundColor Cyan
    Write-Host "  Start-Service -Name $serviceName" -ForegroundColor White
    Write-Host ""
    Write-Host "Ou use o Gerenciador de Servicos do Windows (services.msc)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== COMANDOS UTEIS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Verificar status:" -ForegroundColor Yellow
Write-Host "  Get-Service -Name $serviceName" -ForegroundColor White
Write-Host ""
Write-Host "Parar servico:" -ForegroundColor Yellow
Write-Host "  Stop-Service -Name $serviceName" -ForegroundColor White
Write-Host ""
Write-Host "Iniciar servico:" -ForegroundColor Yellow
Write-Host "  Start-Service -Name $serviceName" -ForegroundColor White
Write-Host ""
Write-Host "Reiniciar servico:" -ForegroundColor Yellow
Write-Host "  Restart-Service -Name $serviceName" -ForegroundColor White
Write-Host ""
Write-Host "Ver logs:" -ForegroundColor Yellow
Write-Host "  Get-Content 'C:\ProgramData\Solis\logs\agente-pdv-*.txt' -Tail 50" -ForegroundColor White
Write-Host ""
Write-Host "Ver banco de dados:" -ForegroundColor Yellow
Write-Host "  C:\ProgramData\Solis\data\agente-pdv.db" -ForegroundColor White
Write-Host ""
