using Microsoft.AspNetCore.Mvc;
using Solis.AgentePDV.Services;

namespace Solis.AgentePDV.Controllers;

/// <summary>
/// Controller para configuração inicial do agente
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        IConfiguracaoService configuracaoService,
        ILogger<ConfigController> logger)
    {
        _configuracaoService = configuracaoService;
        _logger = logger;
    }

    /// <summary>
    /// Configura o agente com token JWT gerado pela API Solis
    /// </summary>
    /// <param name="request">Token JWT e URL da API</param>
    /// <returns>Status da configuração</returns>
    [HttpPost("setup")]
    public async Task<IActionResult> ConfigurarToken([FromBody] SetupRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { error = "Token é obrigatório" });
            }

            if (string.IsNullOrWhiteSpace(request.ApiBaseUrl))
            {
                return BadRequest(new { error = "URL da API é obrigatória" });
            }

            // Valida formato da URL
            if (!Uri.TryCreate(request.ApiBaseUrl, UriKind.Absolute, out _))
            {
                return BadRequest(new { error = "URL da API inválida" });
            }

            _logger.LogInformation("[ConfigController] Configurando token do agente...");

            // Salva token no banco (decodifica e extrai informações)
            await _configuracaoService.SalvarTokenAsync(request.Token, request.ApiBaseUrl);

            // Obtém status após salvar
            var status = await _configuracaoService.ObterStatusConfiguracaoAsync();

            _logger.LogInformation(
                "[ConfigController] Agente configurado com sucesso. Tenant: {TenantId}",
                status.TenantId
            );

            return Ok(new
            {
                success = true,
                message = "Agente configurado com sucesso",
                tenantId = status.TenantId,
                nomeAgente = status.NomeAgente,
                tokenValidoAte = status.TokenValidoAte,
                apiBaseUrl = status.ApiBaseUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConfigController] Erro ao configurar token");
            
            return StatusCode(500, new
            {
                error = "Erro ao configurar token",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtém o status da configuração do agente
    /// </summary>
    /// <returns>Status detalhado da configuração</returns>
    [HttpGet("status")]
    public async Task<IActionResult> ObterStatus()
    {
        try
        {
            var status = await _configuracaoService.ObterStatusConfiguracaoAsync();

            return Ok(new
            {
                configurado = status.Configurado,
                tokenValido = status.TokenValido,
                mensagem = status.Mensagem,
                tenantId = status.TenantId,
                nomeAgente = status.NomeAgente,
                tokenValidoAte = status.TokenValidoAte,
                apiBaseUrl = status.ApiBaseUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConfigController] Erro ao obter status");
            
            return StatusCode(500, new
            {
                error = "Erro ao obter status",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Remove a configuração do agente (útil para reconfiguração)
    /// </summary>
    [HttpDelete("reset")]
    public async Task<IActionResult> ResetarConfiguracao()
    {
        try
        {
            _logger.LogWarning("[ConfigController] Resetando configuração do agente...");

            // TODO: Implementar lógica para remover token do banco
            // Por enquanto, apenas retorna sucesso

            return Ok(new
            {
                success = true,
                message = "Configuração resetada. Configure um novo token."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConfigController] Erro ao resetar configuração");
            
            return StatusCode(500, new
            {
                error = "Erro ao resetar configuração",
                details = ex.Message
            });
        }
    }
}

/// <summary>
/// Request para configuração do agente
/// </summary>
public class SetupRequest
{
    /// <summary>
    /// Token JWT gerado pela API Solis
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// URL base da API Solis (exemplo: http://localhost:3000)
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;
}
