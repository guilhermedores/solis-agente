using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;

namespace Solis.AgentePDV.Services;

/// <summary>
/// Serviço para gerenciar configurações do agente, incluindo vinculação com tenant via token JWT
/// IMPORTANTE: O token JWT serve apenas para vincular o agente ao tenant, NÃO é token de autenticação de usuário
/// </summary>
public interface IConfiguracaoService
{
    Task<Configuracao?> ObterConfiguracaoAsync();
    Task SalvarTokenAsync(string token, string apiBaseUrl);
    Task<bool> TemTokenValidoAsync();
    string? ObterToken();
    string? ObterTenantId();
    Task<ConfiguracaoStatus> ObterStatusConfiguracaoAsync();
}

public class ConfiguracaoService : IConfiguracaoService
{
    private readonly LocalDbContext _context;
    private readonly ILogger<ConfiguracaoService> _logger;
    private Configuracao? _configuracaoCache;
    private DateTime _cacheExpiracao = DateTime.MinValue;
    private readonly TimeSpan _cacheDuracao = TimeSpan.FromMinutes(5);

    public ConfiguracaoService(LocalDbContext context, ILogger<ConfiguracaoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtém a configuração do agente (com cache de 5 minutos)
    /// </summary>
    public async Task<Configuracao?> ObterConfiguracaoAsync()
    {
        // Verifica cache
        if (_configuracaoCache != null && DateTime.UtcNow < _cacheExpiracao)
        {
            return _configuracaoCache;
        }

        // Busca do banco - pega o primeiro registro que tem token configurado
        var config = await _context.Configuracoes
            .Where(c => !string.IsNullOrEmpty(c.Token))
            .OrderByDescending(c => c.AtualizadoEm)
            .FirstOrDefaultAsync();

        // Atualiza cache
        _configuracaoCache = config;
        _cacheExpiracao = DateTime.UtcNow.Add(_cacheDuracao);

        return config;
    }

    /// <summary>
    /// Salva o token JWT no banco de dados
    /// Decodifica o token para extrair tenant, nome do agente e data de expiração
    /// </summary>
    public async Task SalvarTokenAsync(string token, string apiBaseUrl)
    {
        try
        {
            // Decodifica o token JWT (sem validar assinatura, apenas para extrair claims)
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Extrai informações do token
            var tenantId = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant")?.Value;
            var agentName = jwtToken.Claims.FirstOrDefault(c => c.Type == "agentName")?.Value;
            var expirationUnix = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new InvalidOperationException("Token JWT não contém claim 'tenant'");
            }

            // Converte timestamp Unix para DateTime
            DateTime? validoAte = null;
            if (long.TryParse(expirationUnix, out var expUnix))
            {
                validoAte = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            }

            // Verifica se já existe configuração
            var config = await _context.Configuracoes
                .OrderByDescending(c => c.AtualizadoEm)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                // Cria nova configuração
                config = new Configuracao
                {
                    Chave = "CONFIGURACAO_GERAL",
                    Valor = "Configuracao inicial do agente",
                    Token = token,
                    TenantId = tenantId,
                    TokenValidoAte = validoAte,
                    ApiBaseUrl = apiBaseUrl,
                    NomeAgente = agentName ?? "Agente PDV",
                    CriadoEm = DateTime.UtcNow,
                    AtualizadoEm = DateTime.UtcNow
                };
                _context.Configuracoes.Add(config);
            }
            else
            {
                // Atualiza configuração existente
                config.Token = token;
                config.TenantId = tenantId;
                config.TokenValidoAte = validoAte;
                config.ApiBaseUrl = apiBaseUrl;
                config.NomeAgente = agentName ?? config.NomeAgente ?? "Agente PDV";
                config.AtualizadoEm = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Invalida cache
            _configuracaoCache = null;

            _logger.LogInformation(
                "[ConfiguracaoService] Token salvo com sucesso. Tenant: {TenantId}, Valido até: {ValidoAte}",
                tenantId,
                validoAte?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConfiguracaoService] Erro ao salvar token");
            throw;
        }
    }

    /// <summary>
    /// Verifica se existe um token válido configurado
    /// </summary>
    public async Task<bool> TemTokenValidoAsync()
    {
        var config = await ObterConfiguracaoAsync();

        if (config == null || string.IsNullOrEmpty(config.Token))
        {
            return false;
        }

        // Verifica se o token não expirou
        if (config.TokenValidoAte.HasValue && config.TokenValidoAte.Value < DateTime.UtcNow)
        {
            _logger.LogWarning(
                "[ConfiguracaoService] Token expirado. Expirou em: {DataExpiracao}",
                config.TokenValidoAte.Value
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// Obtém o token JWT (do cache se disponível)
    /// </summary>
    public string? ObterToken()
    {
        // Tenta obter do cache primeiro
        if (_configuracaoCache != null && DateTime.UtcNow < _cacheExpiracao)
        {
            return _configuracaoCache.Token;
        }

        // Busca síncrona do banco (use com cuidado)
        var config = _context.Configuracoes
            .Where(c => !string.IsNullOrEmpty(c.Token))
            .OrderByDescending(c => c.AtualizadoEm)
            .FirstOrDefault();

        return config?.Token;
    }

    /// <summary>
    /// Obtém o TenantId (do cache se disponível)
    /// </summary>
    public string? ObterTenantId()
    {
        // Tenta obter do cache primeiro
        if (_configuracaoCache != null && DateTime.UtcNow < _cacheExpiracao)
        {
            return _configuracaoCache.TenantId;
        }

        // Busca síncrona do banco (use com cuidado)
        var config = _context.Configuracoes
            .Where(c => !string.IsNullOrEmpty(c.TenantId))
            .OrderByDescending(c => c.AtualizadoEm)
            .FirstOrDefault();

        return config?.TenantId;
    }

    /// <summary>
    /// Obtém o status completo da configuração do agente
    /// </summary>
    public async Task<ConfiguracaoStatus> ObterStatusConfiguracaoAsync()
    {
        var config = await ObterConfiguracaoAsync();

        if (config == null || string.IsNullOrEmpty(config.Token))
        {
            return new ConfiguracaoStatus
            {
                Configurado = false,
                Mensagem = "Agente não configurado. É necessário configurar o token de autenticação.",
                TokenValido = false
            };
        }

        var tokenValido = await TemTokenValidoAsync();

        if (!tokenValido)
        {
            return new ConfiguracaoStatus
            {
                Configurado = true,
                TokenValido = false,
                Mensagem = "Token expirado ou inválido. É necessário gerar um novo token.",
                TenantId = config.TenantId,
                NomeAgente = config.NomeAgente,
                TokenValidoAte = config.TokenValidoAte,
                ApiBaseUrl = config.ApiBaseUrl
            };
        }

        return new ConfiguracaoStatus
        {
            Configurado = true,
            TokenValido = true,
            Mensagem = "Agente configurado e conectado com sucesso.",
            TenantId = config.TenantId,
            NomeAgente = config.NomeAgente,
            TokenValidoAte = config.TokenValidoAte,
            ApiBaseUrl = config.ApiBaseUrl
        };
    }
}

/// <summary>
/// Representa o status da configuração do agente
/// </summary>
public class ConfiguracaoStatus
{
    public bool Configurado { get; set; }
    public bool TokenValido { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? NomeAgente { get; set; }
    public DateTime? TokenValidoAte { get; set; }
    public string? ApiBaseUrl { get; set; }
}
