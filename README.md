# 🖥️ Solis - Agente PDV

Serviço local .NET 8 que gerencia periféricos e opera como ponte entre o PWA e a API Nuvem.

## 🎯 Propósito

O Solis Agente PDV foi projetado para ser o **controlador principal** do terminal de caixa, permitindo:

- ✅ **Operação Offline** - Funciona sem conexão com a internet
- ✅ **Gerenciamento de Periféricos** - Impressoras, gaveta, TEF, SAT
- ✅ **Banco Local (SQLite)** - Armazena vendas localmente
- ✅ **Sincronização Automática** - Envia dados para nuvem quando online
- ✅ **Windows Service** - Roda como serviço do Windows

## 🏗️ Arquitetura

```
PWA (localhost:8080)
        ↓
   HTTP REST API
        ↓
Agente PDV (localhost:5000)
        ↓
   ┌────┴────┬───────┬────────┐
   ↓         ↓       ↓        ↓
SQLite   Impressora Gaveta  API Nuvem
(local)   (Serial)  (Serial) (HTTP)
```

## 📦 Tecnologias

- **.NET 8.0** - Framework
- **ASP.NET Core Web API** - Endpoints HTTP
- **Entity Framework Core** - ORM
- **SQLite** - Banco de dados local
- **Serilog** - Logging
- **System.IO.Ports** - Comunicação serial

## 🚀 Como Executar

### Desenvolvimento (Console)

```powershell
cd agente-pdv/src
dotnet restore
dotnet run
```

### Produção (Windows Service)

**📖 Guia Completo:** Veja [INSTALACAO_WINDOWS.md](./INSTALACAO_WINDOWS.md) para instruções detalhadas.

**⚡ Instalação Rápida:**

```powershell
# Execute o script de instalação automatizado (como Administrador)
.\scripts\instalar-agente.ps1
```

**🔧 Instalação Manual:**

```powershell
# Publicar
dotnet publish -c Release -o "C:\Solis\AgentePDV"

# Instalar como serviço
sc create "SolisAgentePDV" `
  binPath= "C:\Solis\AgentePDV\Solis.AgentePDV.exe" `
  start= auto `
  DisplayName= "Solis - Agente PDV"

# Iniciar
sc start SolisAgentePDV
```

### Docker

```powershell
cd agente-pdv
docker build -t solis-agente-pdv .
docker run -p 5000:5000 -v ./data:/app/data solis-agente-pdv
```

## 📡 Endpoints API

### Vendas

```http
POST   /api/vendas                 # Criar venda
GET    /api/vendas/{id}            # Obter venda
GET    /api/vendas/pendentes       # Listar vendas não sincronizadas
POST   /api/vendas/{id}/finalizar  # Finalizar venda e imprimir cupom
POST   /api/vendas/{id}/cancelar   # Cancelar venda
```

### Produtos

```http
GET    /api/produtos                           # Listar produtos
GET    /api/produtos/codigo-barras/{codigo}    # Buscar por código
GET    /api/produtos/buscar?termo={termo}      # Buscar por nome
POST   /api/produtos/sync                       # Sincronizar produtos
```

### Periféricos

```http
GET    /api/perifericos/status                    # Status de todos periféricos
POST   /api/perifericos/impressora/imprimir-cupom # Imprimir cupom
POST   /api/perifericos/impressora/imprimir-texto # Imprimir texto livre
POST   /api/perifericos/gaveta/abrir             # Abrir gaveta
GET    /api/perifericos/impressora/testar        # Testar impressora
```

### Health Check

```http
GET    /health                     # Status do serviço
```

## ⚙️ Configuração

Edite `appsettings.json`:

### Banco de Dados

```json
{
  "ConnectionStrings": {
    "LocalDb": "Data Source=agente-pdv.db"
  }
}
```

### API Nuvem

```json
{
  "SolisApi": {
    "BaseUrl": "http://solis-api:3000",
    "Timeout": 30
  }
}
```

### Sincronização

```json
{
  "Sync": {
    "IntervalSeconds": 300,
    "Enabled": true,
    "MaxRetries": 5
  }
}
```

### Periféricos

```json
{
  "Perifericos": {
    "Impressora": {
      "Enabled": true,
      "Tipo": "Termica",
      "Porta": "COM1",
      "BaudRate": 9600
    },
    "Gaveta": {
      "Enabled": true,
      "Porta": "COM1",
      "ComandoAbertura": "1B700019FA"
    }
  }
}
```

## 🗄️ Banco de Dados Local

O Agente usa SQLite para armazenar:

- **Vendas** - Cupons fiscais
- **Vendas Itens** - Itens das vendas
- **Vendas Pagamentos** - Pagamentos
- **Produtos** - Cache de produtos

### Localização

- Windows: `C:\ProgramData\SolisAgentePDV\agente-pdv.db`
- Linux/Docker: `/app/agente-pdv.db`

## 🔄 Sincronização

O agente sincroniza automaticamente:

1. **Vendas** → API Nuvem (a cada 5 minutos)
2. **Produtos** ← API Nuvem (atualiza cache)

### Modo Offline

Quando sem conexão:
- ✅ Vendas são salvas localmente
- ✅ Produtos vêm do cache local
- ✅ Sincronização acontece quando conexão retorna
- ✅ Fila de retry automática

## 🖨️ Impressoras Suportadas

### Impressoras Térmicas (ESC/POS)

- Elgin i7, i8, i9
- Bematech MP-4200 TH
- Epson TM-T20, TM-T88
- Daruma DR-800
- Qualquer impressora ESC/POS compatível

### Configuração

1. Conecte a impressora na porta serial (COM1, COM2, etc.)
2. Configure em `appsettings.json`
3. Teste com: `POST /api/perifericos/impressora/testar`

## 💰 Gaveta de Dinheiro

### Compatibilidade

- Gavetas com acionamento por pulso elétrico
- Conexão via impressora (RJ11/RJ12)
- Comando padrão: ESC p (1B 70 00 19 FA)

### Configuração

```json
{
  "Gaveta": {
    "Enabled": true,
    "Porta": "COM1",
    "ComandoAbertura": "1B700019FA"
  }
}
```

## 🔐 Segurança

- ✅ CORS configurado para aceitar apenas PWA local
- ✅ Logs de auditoria de todas as operações
- ✅ Banco local criptografado (opcional)
- ✅ Sincronização com autenticação JWT

## 📊 Logs

Logs são salvos em `logs/agente-pdv-YYYYMMDD.txt`

```powershell
# Ver logs em tempo real
tail -f logs/agente-pdv-20251027.txt

# Windows PowerShell
Get-Content logs\agente-pdv-20251027.txt -Wait
```

## 🐛 Troubleshooting

### Porta serial não encontrada

```
Erro: O sistema não pode encontrar o arquivo especificado
```

**Solução**: Verifique se a porta COM existe em Device Manager

### Sem conexão com API Nuvem

```
Erro: Sem conexão com a nuvem
```

**Solução**: Vendas ficam na fila local e sincronizam automaticamente quando conexão retornar

### Impressora não responde

1. Verifique se está ligada e conectada
2. Teste com: `GET /api/perifericos/impressora/testar`
3. Verifique configuração da porta serial
4. Teste impressão direta pelo Windows

## 📝 Próximas Funcionalidades

- [ ] Integração TEF (Sitef, PayGo)
- [ ] Integração SAT Fiscal
- [ ] Integração MFe (NFC-e)
- [ ] Suporte a balança
- [ ] Leitor de código de barras USB
- [ ] Backup automático do banco local
- [ ] Interface de configuração web

## 📚 Documentação Adicional

- [Protocolo ESC/POS](https://reference.epson-biz.com/modules/ref_escpos/index.php)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Windows Services](https://learn.microsoft.com/dotnet/core/extensions/windows-service)

---

**Versão**: 1.0.0  
**Última atualização**: Outubro 2025
