using Solis.AgentePDV.Services;

namespace Solis.AgentePDV.Security;

/// <summary>
/// DelegatingHandler que adiciona automaticamente o X-Tenant header baseado no token JWT armazenado
/// IMPORTANTE: Este token NÃO é usado para autenticação de usuário, apenas para vincular o agente ao tenant
/// </summary>
public class TenantHandler : DelegatingHandler
{
    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogger<TenantHandler> _logger;

    public TenantHandler(
        IConfiguracaoService configuracaoService,
        ILogger<TenantHandler> logger)
    {
        _configuracaoService = configuracaoService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtém configuração do agente (contém TenantId extraído do token JWT)
            var config = await _configuracaoService.ObterConfiguracaoAsync();

            if (config != null && !string.IsNullOrEmpty(config.TenantId))
            {
                // Adiciona header X-Tenant para identificar o tenant nas requisições à API
                request.Headers.TryAddWithoutValidation("X-Tenant", config.TenantId);

                _logger.LogDebug(
                    "[TenantHandler] X-Tenant header adicionado à requisição {Method} {Uri}. Tenant: {TenantId}",
                    request.Method,
                    request.RequestUri,
                    config.TenantId
                );
            }
            else
            {
                _logger.LogWarning(
                    "[TenantHandler] Agente não configurado (sem TenantId) para requisição {Method} {Uri}",
                    request.Method,
                    request.RequestUri
                );
            }

            // Envia a requisição
            var response = await base.SendAsync(request, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TenantHandler] Erro ao processar requisição {Method} {Uri}",
                request.Method,
                request.RequestUri
            );
            throw;
        }
    }
}
