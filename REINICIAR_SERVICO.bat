@echo off
echo ========================================
echo REINICIANDO SERVICO AGENTE PDV
echo ========================================
echo.

echo Verificando se o servico existe...
sc query SolisAgentePDV >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: Servico nao instalado!
    echo Execute ATUALIZAR_AGENTE.bat como Administrador primeiro.
    echo.
    pause
    exit /b 1
)

echo Parando servico...
net stop SolisAgentePDV
timeout /t 3 /nobreak >nul

echo.
echo Iniciando servico...
net start SolisAgentePDV

echo.
echo Verificando status...
sc query SolisAgentePDV

echo.
echo ========================================
echo SERVICO REINICIADO!
echo ========================================
echo.
echo Testando API do agente em 3 segundos...
timeout /t 3 /nobreak >nul

curl -X GET http://localhost:5000/health 2>nul
if %errorlevel% equ 0 (
    echo.
    echo API respondendo corretamente!
) else (
    echo.
    echo AVISO: API nao respondeu. Verifique os logs.
)

echo.
echo.
pause
