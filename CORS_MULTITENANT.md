# 🌐 Configuração Multi-Tenant CORS

## 📋 Visão Geral

O Agente PDV está configurado para suportar **multi-tenancy** através de subdomínios, permitindo que cada cliente tenha seu próprio subdomínio enquanto mantém a segurança CORS.

## 🔒 Como Funciona

### Modo Desenvolvimento
```
✅ TODAS as origens são permitidas
   - Postman
   - Insomnia
   - curl
   - Qualquer navegador
```

### Modo Produção
```
✅ Subdomínios do domínio configurado
   - cliente1.seudominio.com.br
   - cliente2.seudominio.com.br
   - loja-abc.seudominio.com.br
   - *.seudominio.com.br

✅ Localhost (para testes)
   - http://localhost:3000
   - http://localhost:8080
   - http://127.0.0.1:8080

❌ Outros domínios são BLOQUEADOS
```

## ⚙️ Configuração

### 1. appsettings.json (Desenvolvimento)
```json
{
  "CORS": {
    "AllowedDomain": "seudominio.com.br"
  }
}
```

### 2. appsettings.Production.json (Produção)
```json
{
  "CORS": {
    "AllowedDomain": "minhaempresa.com.br"
  }
}
```

### 3. Variável de Ambiente (Sobrescreve tudo)
```bash
CORS__AllowedDomain=minhaempresa.com.br
```

## 🎯 Exemplos de Uso

### Cenário 1: SaaS com Subdomínios
```
Domínio base: meupdv.com.br

Clientes:
✅ https://loja-joao.meupdv.com.br
✅ https://mercado-maria.meupdv.com.br
✅ https://restaurante-ze.meupdv.com.br
✅ https://farmacia-sul.meupdv.com.br

Configuração:
{
  "CORS": {
    "AllowedDomain": "meupdv.com.br"
  }
}
```

### Cenário 2: Múltiplos Domínios
Se precisar de múltiplos domínios, ajuste o código:

```csharp
var allowedDomains = builder.Configuration
    .GetSection("CORS:AllowedDomains")
    .Get<string[]>() ?? new[] { "seudominio.com.br" };

policy.SetIsOriginAllowed(origin =>
{
    var uri = new Uri(origin);
    
    // Localhost
    if (uri.Host == "localhost" || uri.Host == "127.0.0.1")
        return true;
    
    // Verificar múltiplos domínios
    foreach (var domain in allowedDomains)
    {
        if (uri.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase) || 
            uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase))
            return true;
    }
    
    return false;
});
```

E no appsettings.json:
```json
{
  "CORS": {
    "AllowedDomains": [
      "meupdv.com.br",
      "meupdv.net",
      "clientesespeciais.com"
    ]
  }
}
```

## 🧪 Testando

### Testar em Desenvolvimento
```bash
# Qualquer origem funciona
curl http://localhost:5000/health
```

### Testar em Produção
```bash
# Com origem válida (cliente1.meupdv.com.br)
curl -H "Origin: https://cliente1.meupdv.com.br" \
     http://localhost:5000/health -v

# Com origem inválida (outrosite.com)
curl -H "Origin: https://outrosite.com" \
     http://localhost:5000/health -v
```

## 🚀 Deploy

### Via Script de Instalação
```powershell
# Instalar e configurar domínio
.\instalar-agente-precompilado.ps1

# Depois editar manualmente:
notepad "C:\Solis\AgentePDV\src\appsettings.json"

# Ou via script:
$config = Get-Content "C:\Solis\AgentePDV\src\appsettings.json" | ConvertFrom-Json
$config.CORS.AllowedDomain = "meupdv.com.br"
$config | ConvertTo-Json -Depth 10 | Set-Content "C:\Solis\AgentePDV\src\appsettings.json"

# Reiniciar serviço
Restart-Service SolisAgentePDV
```

### Via Variável de Ambiente
```powershell
# Definir variável de ambiente do sistema
[System.Environment]::SetEnvironmentVariable(
    "CORS__AllowedDomain", 
    "meupdv.com.br", 
    [System.EnvironmentVariableTarget]::Machine
)

# Reiniciar serviço
Restart-Service SolisAgentePDV
```

## 🔐 Segurança

### ✅ Boas Práticas Implementadas
- Validação de origem por domínio
- Suporte a HTTPS em produção
- Credenciais permitidas para autenticação
- Logging de requisições CORS

### ⚠️ Atenções
- **Sempre use HTTPS em produção**
- **Configure certificado SSL válido**
- **Mantenha o domínio configurado atualizado**
- **Não use `AllowAnyOrigin()` em produção**

## 🆘 Troubleshooting

### Erro: "CORS policy blocked"
1. Verifique o domínio configurado:
   ```powershell
   Get-Content "C:\Solis\AgentePDV\src\appsettings.json" | ConvertFrom-Json | Select -ExpandProperty CORS
   ```

2. Verifique os logs:
   ```powershell
   Get-Content "C:\Solis\AgentePDV\logs\agente-pdv-$(Get-Date -Format 'yyyyMMdd').txt" -Tail 50
   ```

3. Teste a origem:
   ```bash
   curl -H "Origin: https://seu-cliente.seudominio.com.br" \
        http://localhost:5000/health -v
   ```

### Verificar Ambiente
```csharp
// No código, adicione log temporário
Log.Information("CORS AllowedDomain: {Domain}", 
    builder.Configuration["CORS:AllowedDomain"]);
Log.Information("Environment: {Env}", 
    builder.Environment.EnvironmentName);
```

## 📚 Referências

- [ASP.NET Core CORS](https://docs.microsoft.com/en-us/aspnet/core/security/cors)
- [Multi-Tenant Architecture](https://docs.microsoft.com/en-us/azure/architecture/guide/multitenant/overview)
- [CORS Best Practices](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
