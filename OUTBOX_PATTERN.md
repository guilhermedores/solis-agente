# 📦 Outbox Pattern - Arquitetura de Sincronização

## Visão Geral

O **Solis Agente PDV** agora utiliza o **Outbox Pattern** para garantir sincronização confiável com a API na nuvem. Esta é uma arquitetura robusta que garante que nenhuma operação seja perdida, mesmo em caso de falhas de rede.

## 🎯 Problema Resolvido

**Antes (Conexão Direta):**
- ❌ Agente PDV conectava direto no banco PostgreSQL da nuvem
- ❌ Falhas de rede causavam perda de dados
- ❌ Sem controle de retry
- ❌ Difícil auditoria e troubleshooting
- ❌ Dependência forte de conectividade

**Agora (Outbox Pattern + API):**
- ✅ Toda comunicação via REST API
- ✅ Mensagens armazenadas localmente antes do envio
- ✅ Retry automático com backoff exponencial
- ✅ Auditoria completa de todas as operações
- ✅ Funciona offline, sincroniza quando online

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────────────────┐
│                    Agente PDV (Local)                    │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  ┌──────────┐                                            │
│  │  PWA/UI  │                                            │
│  └────┬─────┘                                            │
│       │                                                   │
│       v                                                   │
│  ┌──────────────┐        ┌─────────────────┐           │
│  │ VendaService │───────>│  OutboxService  │           │
│  └──────────────┘        └────────┬────────┘           │
│                                    │                     │
│                                    v                     │
│                          ┌──────────────────┐           │
│                          │   SQLite Local   │           │
│                          │                  │           │
│                          │  • Vendas        │           │
│                          │  • Produtos      │           │
│                          │  • OutboxMessages│           │
│                          └──────────────────┘           │
│                                    ^                     │
│                                    │                     │
│                          ┌─────────┴────────┐           │
│                          │ OutboxProcessor  │           │
│                          │ (Background)     │           │
│                          └─────────┬────────┘           │
└──────────────────────────────────┬─┬────────────────────┘
                                    │ │ HTTP/REST
                                    │ │ Retry c/ Backoff
                                    v v
                          ┌───────────────────┐
                          │   Solis API       │
                          │   (Node.js)       │
                          └─────────┬─────────┘
                                    │
                                    v
                          ┌───────────────────┐
                          │   PostgreSQL      │
                          │   (Cloud DB)      │
                          └───────────────────┘
```

## 📊 Fluxo de Dados

### 1. Criação de Venda

```csharp
// 1. PWA chama o endpoint
POST /api/vendas

// 2. VendaService salva localmente
await _context.Vendas.Add(venda);
await _context.SaveChangesAsync();

// 3. VendaService adiciona ao Outbox
await _outboxService.EnqueueAsync(
    tipoEntidade: "Venda",
    operacao: "Create",
    entidadeId: venda.Id,
    entidade: venda,
    endpoint: "/api/vendas",
    metodo: "POST",
    prioridade: 5
);

// 4. Retorna sucesso imediatamente para o PWA
return Ok(venda);
```

### 2. Processamento do Outbox

```csharp
// OutboxProcessorService (roda em background a cada 10 segundos)

// 1. Busca mensagens pendentes
var mensagens = await _outboxService.ObterMensagensPendentesAsync(50);

// 2. Para cada mensagem
foreach (var msg in mensagens) {
    // 3. Marca como "Processando"
    await _outboxService.MarcarComoProcessandoAsync(msg.Id);
    
    // 4. Tenta enviar para API
    var response = await httpClient.PostAsync(msg.EndpointApi, msg.PayloadJson);
    
    if (response.IsSuccessStatusCode) {
        // 5. Marca como "Enviado"
        await _outboxService.MarcarComoEnviadoAsync(msg.Id, 200);
    } else {
        // 6. Marca como "Erro" e agenda retry
        await _outboxService.MarcarComoErroAsync(
            msg.Id, 
            error, 
            statusCode, 
            proximaTentativa
        );
    }
}
```

## 🗄️ Modelo de Dados - OutboxMessage

```sql
CREATE TABLE OutboxMessages (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY,
    TipoEntidade        VARCHAR(50)    NOT NULL,  -- "Venda", "Produto", etc.
    Operacao            VARCHAR(20)    NOT NULL,  -- "Create", "Update", "Delete"
    EntidadeId          UNIQUEIDENTIFIER NOT NULL,
    PayloadJson         TEXT           NOT NULL,  -- Dados completos em JSON
    EndpointApi         VARCHAR(500)   NOT NULL,  -- "/api/vendas"
    MetodoHttp          VARCHAR(10)    NOT NULL,  -- "POST", "PUT", "DELETE"
    Status              VARCHAR(20)    NOT NULL,  -- "Pendente", "Enviado", "Erro"
    TentativasEnvio     INT            DEFAULT 0,
    MaxTentativas       INT            DEFAULT 5,
    UltimoErro          TEXT           NULL,
    UltimoStatusCode    INT            NULL,
    CriadoEm            DATETIME       NOT NULL,
    UltimaTentativaEm   DATETIME       NULL,
    EnviadoEm           DATETIME       NULL,
    ProximaTentativaEm  DATETIME       NULL,
    Prioridade          INT            DEFAULT 0
);

-- Índices para performance
CREATE INDEX IX_OutboxMessages_Status ON OutboxMessages(Status);
CREATE INDEX IX_OutboxMessages_ProximaTentativa 
    ON OutboxMessages(Status, ProximaTentativaEm, Prioridade);
```

## ⚙️ Configuração

### appsettings.json

```json
{
  "Outbox": {
    "IntervaloSegundos": 10,    // Frequência de processamento
    "DiasRetencao": 30,          // Tempo de retenção de mensagens enviadas
    "LoteMaximo": 50             // Máximo de mensagens por ciclo
  },
  "SolisApi": {
    "BaseUrl": "http://api.seuservidor.com",
    "Timeout": 30,
    "RetryAttempts": 3
  }
}
```

## 🔄 Estratégia de Retry

### Backoff Exponencial

Quando uma mensagem falha, o tempo até a próxima tentativa aumenta exponencialmente:

```
Tentativa 1: imediato
Tentativa 2: 1 minuto depois
Tentativa 3: 2 minutos depois
Tentativa 4: 4 minutos depois
Tentativa 5: 8 minutos depois
Tentativa 6: 16 minutos depois (máximo)
```

```csharp
var delayMinutos = Math.Pow(2, message.TentativasEnvio - 1);
message.ProximaTentativaEm = DateTime.UtcNow.AddMinutes(delayMinutos);
```

### Estados das Mensagens

```
Pendente    → Primeira vez ou aguardando retry
Processando → Sendo enviada neste momento
Enviado     → Enviada com sucesso (HTTP 2xx)
Erro        → Falhou após todas as tentativas
```

## 🎚️ Prioridades

Mensagens têm prioridades para garantir que operações críticas sejam processadas primeiro:

```csharp
Prioridade 10: Venda Finalizada (crítico)
Prioridade 8:  Cancelamento de Venda
Prioridade 5:  Criação de Venda
Prioridade 3:  Atualização de Produto
Prioridade 0:  Outras operações (padrão)
```

## 📡 Endpoints de Monitoramento

### Ver Estatísticas

```bash
GET http://localhost:5000/api/outbox/stats

Response:
{
  "totalPendentes": 15,
  "timestamp": "2025-10-27T10:30:00Z"
}
```

### Listar Mensagens Pendentes

```bash
GET http://localhost:5000/api/outbox/pending?limit=100

Response:
{
  "total": 15,
  "mensagens": [
    {
      "id": "guid",
      "tipoEntidade": "Venda",
      "operacao": "Create",
      "entidadeId": "guid",
      "endpointApi": "/api/vendas",
      "status": "Pendente",
      "tentativasEnvio": 2,
      "maxTentativas": 5,
      "ultimoErro": "Connection timeout",
      "criadoEm": "2025-10-27T10:00:00Z",
      "proximaTentativaEm": "2025-10-27T10:32:00Z",
      "prioridade": 5
    }
  ]
}
```

### Limpar Mensagens Antigas

```bash
POST http://localhost:5000/api/outbox/cleanup?dias=30

Response:
{
  "removidas": 142,
  "diasRetencao": 30,
  "timestamp": "2025-10-27T10:30:00Z"
}
```

## 🔍 Troubleshooting

### Ver mensagens com erro

```sql
SELECT * FROM OutboxMessages 
WHERE Status = 'Erro' 
ORDER BY CriadoEm DESC;
```

### Reprocessar mensagem específica

```sql
-- Resetar mensagem para retry
UPDATE OutboxMessages 
SET Status = 'Pendente',
    TentativasEnvio = 0,
    ProximaTentativaEm = datetime('now')
WHERE Id = 'guid-da-mensagem';
```

### Ver estatísticas

```sql
SELECT 
    Status,
    COUNT(*) as Total,
    AVG(TentativasEnvio) as MediaTentativas
FROM OutboxMessages
GROUP BY Status;
```

## ✅ Vantagens do Outbox Pattern

1. **Confiabilidade**
   - Nenhuma operação é perdida
   - Transação local garante consistência

2. **Resiliência**
   - Funciona offline
   - Retry automático
   - Degradação graceful

3. **Auditoria**
   - Histórico completo de operações
   - Rastreabilidade de erros
   - Debugging facilitado

4. **Performance**
   - Resposta imediata ao usuário
   - Processamento assíncrono
   - Batch de mensagens

5. **Flexibilidade**
   - Fácil adicionar novos tipos de sincronização
   - Priorização de operações críticas
   - Configuração por ambiente

## 🚀 Próximos Passos

### Na Solis API (a implementar):

```javascript
// POST /api/vendas
router.post('/vendas', async (req, res) => {
  const venda = req.body;
  
  // Validar
  if (!venda.Id || !venda.NumeroCupom) {
    return res.status(400).json({ error: 'Dados inválidos' });
  }
  
  // Salvar no PostgreSQL
  await db.vendas.create(venda);
  
  // Retornar sucesso
  res.status(201).json({ 
    success: true, 
    vendaId: venda.Id 
  });
});
```

## 📚 Referências

- [Outbox Pattern - Microsoft](https://docs.microsoft.com/en-us/azure/architecture/patterns/outbox)
- [Transactional Outbox - Chris Richardson](https://microservices.io/patterns/data/transactional-outbox.html)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)
