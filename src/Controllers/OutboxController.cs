using Solis.AgentePDV.Services;
using Microsoft.AspNetCore.Mvc;

namespace Solis.AgentePDV.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutboxController : ControllerBase
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<OutboxController> _logger;

    public OutboxController(IOutboxService outboxService, ILogger<OutboxController> logger)
    {
        _outboxService = outboxService;
        _logger = logger;
    }

    /// <summary>
    /// Obter estatísticas do Outbox
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var totalPendentes = await _outboxService.ObterTotalPendentesAsync();
            
            return Ok(new
            {
                totalPendentes,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas do Outbox");
            return StatusCode(500, new { error = "Erro ao obter estatísticas" });
        }
    }

    /// <summary>
    /// Listar mensagens pendentes
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] int limit = 100)
    {
        try
        {
            var mensagens = await _outboxService.ObterMensagensPendentesAsync(limit);
            
            return Ok(new
            {
                total = mensagens.Count,
                mensagens = mensagens.Select(m => new
                {
                    m.Id,
                    m.TipoEntidade,
                    m.Operacao,
                    m.EntidadeId,
                    m.EndpointApi,
                    m.Status,
                    m.TentativasEnvio,
                    m.MaxTentativas,
                    m.UltimoErro,
                    m.CriadoEm,
                    m.ProximaTentativaEm,
                    m.Prioridade
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar mensagens pendentes");
            return StatusCode(500, new { error = "Erro ao listar mensagens" });
        }
    }

    /// <summary>
    /// Limpar mensagens antigas enviadas
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> Cleanup([FromQuery] int dias = 30)
    {
        try
        {
            var removed = await _outboxService.LimparMensagensAntigasAsync(dias);
            
            return Ok(new
            {
                removidas = removed,
                diasRetencao = dias,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar mensagens antigas");
            return StatusCode(500, new { error = "Erro ao limpar mensagens" });
        }
    }
}
