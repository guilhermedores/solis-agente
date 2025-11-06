@echo off
echo ========================================
echo ATUALIZANDO AGENTE PDV
echo ========================================
echo.

echo [1/6] Verificando servico...
sc query SolisAgentePDV >nul 2>&1
if %errorlevel% equ 0 (
    echo Servico encontrado. Parando...
    net stop SolisAgentePDV
    timeout /t 3 /nobreak >nul
) else (
    echo Servico nao instalado. Sera instalado apos a compilacao.
)

echo.
echo [2/6] Compilando agente...
cd /d "%~dp0"
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish

if errorlevel 1 (
    echo ERRO: Falha na compilacao!
    pause
    exit /b 1
)

echo.
echo [3/6] Copiando arquivos de configuracao...
copy /Y "src\appsettings.json" "publish\"
copy /Y "src\appsettings.Production.json" "publish\"

echo.
echo [4/6] Copiando para pasta de instalacao...
if not exist "C:\Solis\AgentePDV\" mkdir "C:\Solis\AgentePDV"
xcopy /Y /E /I "publish\*" "C:\Solis\AgentePDV\"

echo.
echo [5/6] Verificando instalacao do servico...
sc query SolisAgentePDV >nul 2>&1
if %errorlevel% neq 0 (
    echo Instalando servico...
    sc create SolisAgentePDV binPath= "C:\Solis\AgentePDV\Solis.AgentePDV.exe" start= auto DisplayName= "Solis Agente PDV"
    sc description SolisAgentePDV "Agente local do Solis PDV para sincronizacao e gerenciamento de vendas offline"
    sc failure SolisAgentePDV reset= 86400 actions= restart/60000/restart/60000/restart/60000
    echo Servico instalado com sucesso!
)

echo.
echo [6/6] Iniciando servico...
net start SolisAgentePDV

echo.
echo Verificando status...
sc query SolisAgentePDV

echo.
echo ========================================
echo AGENTE ATUALIZADO COM SUCESSO!
echo ========================================
echo.
echo Dados e logs em: C:\ProgramData\Solis\
echo   - Banco de dados: C:\ProgramData\Solis\data\agente-pdv.db
echo   - Logs: C:\ProgramData\Solis\logs\
echo.
echo Configuracao ativa (appsettings.json):
type "C:\Solis\AgentePDV\appsettings.json" | findstr "BaseUrl"
echo.
if exist "C:\Solis\AgentePDV\appsettings.Production.json" (
    echo Configuracao de producao encontrada.
) else (
    echo AVISO: appsettings.Production.json nao encontrado.
)
echo.
pause
