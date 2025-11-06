# 🔐 Autenticação JWT Multi-Tenant - Agente PDV

Sistema de autenticação seguro que impede alteração manual do tenant pelo usuário.

## 📋 Visão Geral

O sistema usa **tokens JWT assinados** para garantir que:
1. ✅ O tenant está embutido no token e não pode ser alterado
2. ✅ O token é armazenado em arquivo com permissões restritas
3. ✅ Qualquer tentativa de modificação invalida a autenticação
4. ✅ Tokens têm validade configurável (padrão: 10 anos)

---

## 🔄 Fluxo de Autenticação

```
┌─────────────────┐
│  Administrador  │
└────────┬────────┘
         │ 1. Solicita token via API
         ▼
┌─────────────────────────────┐
│  POST /api/auth/generate-   │
│       agent-token           │
│  {                          │
│    tenantId: "demo",        │
│    adminKey: "secret"       │
│  }                          │
└────────┬────────────────────┘
         │ 2. API gera JWT com tenant embutido
         │    (assinado com chave secreta)
         ▼
┌─────────────────────────────┐
│  Token JWT                  │
│  eyJhbGciOiJIUzI1NiIsIn...  │
│                             │
│  Payload:                   │
│  {                          │
│    tenant: "demo",          │
│    type: "agente-pdv",      │
│    exp: 1735689600          │
│  }                          │
└────────┬────────────────────┘
         │ 3. Administrador copia token
         ▼
┌─────────────────────────────┐
│  Script PowerShell          │
│  instalar-agente.ps1        │
│                             │
│  - Valida formato JWT       │
│  - Decodifica e mostra info │
│  - Salva em arquivo JSON    │
│  - Aplica permissões        │
└────────┬────────────────────┘
         │ 4. Config salva em arquivo
         ▼
┌─────────────────────────────┐
│  agent.config.json          │
│  C:\Solis\AgentePDV\config\ │
│                             │
│  {                          │
│    token: "eyJ...",         │
│    tenantId: "demo",        │
│    apiUrl: "..."            │
│  }                          │
│  (Permissões restritas:     │
│   Admin + System apenas)    │
└────────┬────────────────────┘
         │ 5. Agente inicia
         ▼
┌─────────────────────────────┐
│  TenantTokenManager.cs      │
│                             │
│  - Lê config do arquivo     │
│  - Valida assinatura JWT    │
│  - Extrai tenant            │
└────────┬────────────────────┘
         │ 6. Configura HttpClient
         ▼
┌─────────────────────────────┐
│  SolisApiClient.cs          │
│                             │
│  Headers:                   │
│  - Authorization: Bearer    │
│    eyJhbGciOiJIUzI1NiI...   │
│  - X-Tenant: demo           │
└────────┬────────────────────┘
         │ 7. Toda requisição envia token
         ▼
┌─────────────────────────────┐
│  API Solis (Next.js)        │
│  middleware.ts              │
│                             │
│  - Valida JWT               │
│  - Extrai tenant            │
│  - Seta contexto            │
└─────────────────────────────┘
```

---

## 🚀 Passo a Passo

### 1️⃣ Gerar Token (Administrador)

```bash
curl -X POST http://localhost:3000/api/auth/generate-agent-token \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "demo",
    "adminKey": "your-admin-secret-key",
    "expiresInDays": 3650,
    "agentName": "PDV Loja Centro"
  }'
```

**Resposta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tenantId": "demo",
  "expiresAt": "2034-11-03T12:00:00.000Z",
  "instructions": "..."
}
```

### 2️⃣ Instalar Agente (PDV)

Execute como **Administrador**:

```powershell
cd agente-pdv\scripts
.\instalar-agente-jwt.ps1 -Token "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." -ApiUrl "http://localhost:3000"
```

Ou interativo:
```powershell
.\instalar-agente-jwt.ps1
# Script solicitará o token
```

**Saída:**
```
=== INSTALADOR AGENTE PDV SOLIS ===

[1/5] Validando token JWT...
  ✓ Token válido!
  Tenant: demo
  Tipo: agente-pdv
  Nome: PDV Loja Centro
  Expira em: 03/11/2034 12:00:00

[2/5] Criando estrutura de diretórios...
  ✓ Criado: C:\Program Files\Solis\AgentePDV
  ✓ Criado: C:\ProgramData\Solis\AgentePDV

[3/5] Salvando configuração segura no Registry...
  ✓ Token criptografado e salvo com segurança
  ✓ Configuração protegida contra alteração manual

[4/5] Criando arquivo de configuração...
  ✓ appsettings.json criado

[5/5] Testando conectividade com a API...
  ✓ Conexão estabelecida com sucesso!
  Tenant: demo
  Isolamento: SCHEMA - tenant_demo

=== INSTALAÇÃO CONCLUÍDA ===
```

### 3️⃣ Uso no Código C#

**Program.cs:**
```csharp
using Solis.AgentePDV.Security;
using Solis.AgentePDV.Services;

// Carrega configuração
var jwtSecret = builder.Configuration["JWT_SECRET"]!;
var apiUrl = builder.Configuration["ApiUrl"]!;

// Inicializa gerenciador de token
var tokenManager = new TenantTokenManager(jwtSecret, apiUrl);

// Valida e obtém tenant
if (!tokenManager.HasValidToken())
{
    Console.WriteLine("Token inválido ou não encontrado!");
    Console.WriteLine("Execute o instalador primeiro.");
    Environment.Exit(1);
}

var tenantId = tokenManager.GetTenantId();
var token = tokenManager.GetToken()!;

Console.WriteLine($"Agente iniciado para tenant: {tenantId}");

// Configura HttpClient
var apiClient = new SolisApiClient(apiUrl, token, tenantId);

// Testa conexão
var isConnected = await apiClient.TestConnectionAsync();
if (!isConnected)
{
    Console.WriteLine("Falha ao conectar com a API!");
}

// Usa normalmente
var produtos = await apiClient.GetAsync<ProdutosResponse>("/api/produtos");
```

---

## 🔒 Segurança

### Token JWT
```json
{
  "tenant": "demo",
  "type": "agente-pdv",
  "agentName": "PDV Loja Centro",
  "iat": 1699000000,
  "exp": 2014360000,
  "iss": "solis-api",
  "aud": "solis-agente"
}
```

**Assinado com HMAC-SHA256:**
- Chave: `JWT_SECRET` do .env
- Qualquer alteração no payload invalida a assinatura
- API verifica assinatura em toda requisição

### Criptografia DPAPI

**Windows Data Protection API:**
- Criptografa token com chave da máquina
- Apenas a máquina instalada pode descriptografar
- Adiciona entropy aleatória para maior segurança

**Registry:**
```
HKLM\SOFTWARE\Solis\AgentePDV
  - TenantToken: [Base64 do token criptografado]
  - TokenEntropy: [Base64 do entropy]
  - ApiUrl: http://localhost:3000
  - TenantId: demo (apenas informativo)
```

### Validações na API

**middleware.ts (Next.js):**
```typescript
import jwt from 'jsonwebtoken'

export async function middleware(request: NextRequest) {
  const authHeader = request.headers.get('authorization')
  
  if (!authHeader?.startsWith('Bearer ')) {
    return NextResponse.json({ error: 'Token não fornecido' }, { status: 401 })
  }

  const token = authHeader.substring(7)
  
  try {
    const decoded = jwt.verify(token, process.env.JWT_SECRET!, {
      issuer: 'solis-api',
      audience: 'solis-agente'
    }) as any
    
    const tenant = decoded.tenant
    
    // Seta tenant no contexto da requisição
    request.headers.set('x-tenant-validated', tenant)
    
    return NextResponse.next()
  } catch (error) {
    return NextResponse.json({ error: 'Token inválido' }, { status: 401 })
  }
}
```

---

## ⚙️ Configuração

### API (solis-api/.env.local)

```bash
# Chave secreta para assinar JWT (MUDE EM PRODUÇÃO!)
JWT_SECRET=your-super-secret-jwt-key-change-in-production

# Chave administrativa para gerar tokens (MUDE EM PRODUÇÃO!)
ADMIN_SECRET_KEY=your-admin-secret-key-change-in-production
```

### Agente (agente-pdv/appsettings.json)

```json
{
  "ApiUrl": "http://localhost:3000",
  "JWT_SECRET": "your-super-secret-jwt-key-change-in-production",
  "LogLevel": "Information"
}
```

**⚠️ IMPORTANTE:** `JWT_SECRET` deve ser o **mesmo** na API e no Agente!

---

## 🛡️ Proteções Implementadas

| Ameaça | Proteção | Como Funciona |
|--------|----------|---------------|
| **Alteração manual do tenant** | Token JWT assinado | Qualquer alteração no payload invalida a assinatura |
| **Roubo do token do Registry** | DPAPI + entropy | Token criptografado, só esta máquina pode descriptografar |
| **Replay de token em outra máquina** | DPAPI LocalMachine | Token criptografado não funciona em outro computador |
| **Token expirado** | Validação de `exp` | API rejeita tokens expirados |
| **Geração não autorizada de tokens** | Admin Secret Key | Apenas quem tem a chave pode gerar tokens |
| **Man-in-the-middle** | HTTPS em produção | Comunicação criptografada (TODO) |
| **Força bruta na API** | Rate limiting | Limita tentativas de requisição (TODO) |

---

## 🧪 Testando

### 1. Gerar token de teste

```bash
curl -X POST http://localhost:3000/api/auth/generate-agent-token \
  -H "Content-Type: application/json" \
  -d '{"tenantId":"demo","adminKey":"your-admin-secret-key","expiresInDays":1}'
```

### 2. Testar validação

```bash
# Token válido
curl http://localhost:3000/api/health \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Token inválido (deve retornar 401)
curl http://localhost:3000/api/health \
  -H "Authorization: Bearer token-invalido"
```

### 3. Testar tentativa de alteração

**❌ Tentativa de fraude:**
1. Usuário pega token do Registry
2. Descriptografa (consegue, é sua máquina)
3. Decodifica JWT e altera tenant: `demo` → `cliente1`
4. Codifica novamente
5. Criptografa e salva no Registry

**✅ Resultado:**
- Token com payload alterado tem assinatura inválida
- API rejeita com `401 Unauthorized`
- Agente não funciona

---

## 📚 Referências

- [JWT.io](https://jwt.io/) - Debug de tokens JWT
- [DPAPI](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata) - Criptografia Windows
- [jsonwebtoken (npm)](https://www.npmjs.com/package/jsonwebtoken) - Lib JWT Node.js
- [System.IdentityModel.Tokens.Jwt (NuGet)](https://www.nuget.org/packages/System.IdentityModel.Tokens.Jwt/) - Lib JWT .NET

---

## 🚀 Próximos Passos

- [ ] Implementar HTTPS em produção
- [ ] Adicionar rate limiting na API
- [ ] Criar renovação automática de tokens
- [ ] Dashboard admin para gerenciar tokens
- [ ] Logs de auditoria de acesso
- [ ] Suporte a múltiplos PDVs por tenant
