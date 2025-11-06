using Microsoft.AspNetCore.Mvc;
using Solis.AgentePDV.Models;
using Solis.AgentePDV.Services;

namespace Solis.AgentePDV.Controllers;

/// <summary>
/// Controller para consulta de formas de pagamento.
/// Criação/Atualização apenas via sincronização com a API central.
/// </summary>
[ApiController]
[Route("api/formas-pagamento")]
public class FormaPagamentoController : ControllerBase
{
    private readonly IFormaPagamentoService _service;
    private readonly ILogger<FormaPagamentoController> _logger;

    public FormaPagamentoController(
        IFormaPagamentoService service,
        ILogger<FormaPagamentoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as formas de pagamento
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FormaPagamento>>> Listar(
        [FromQuery] bool? ativas = null,
        [FromQuery] string? tipo = null)
    {
        try
        {
            var formas = await _service.ListarAsync(ativas, tipo);
            
            return Ok(new
            {
                total = formas.Count,
                filtros = new { ativas, tipo },
                tiposDisponiveis = TipoFormaPagamento.ObterTodos(),
                formasPagamento = formas
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar formas de pagamento");
            return StatusCode(500, new { message = "Erro ao listar formas de pagamento", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista apenas as formas de pagamento ativas
    /// </summary>
    [HttpGet("ativas")]
    public async Task<ActionResult<List<FormaPagamento>>> ListarAtivas()
    {
        try
        {
            var formas = await _service.ListarAtivasAsync();
            return Ok(formas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar formas de pagamento ativas");
            return StatusCode(500, new { message = "Erro ao listar formas de pagamento", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém uma forma de pagamento por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FormaPagamento>> ObterPorId(Guid id)
    {
        try
        {
            var forma = await _service.ObterPorIdAsync(id);
            
            if (forma == null)
            {
                return NotFound(new { message = $"Forma de pagamento {id} não encontrada" });
            }

            return Ok(forma);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter forma de pagamento {Id}", id);
            return StatusCode(500, new { message = "Erro ao obter forma de pagamento", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém os tipos de forma de pagamento disponíveis
    /// </summary>
    [HttpGet("tipos")]
    public ActionResult<List<string>> ObterTipos()
    {
        return Ok(new
        {
            tipos = TipoFormaPagamento.ObterTodos(),
            descricoes = new
            {
                CREDITO = "Cartão de Crédito",
                DEBITO = "Cartão de Débito",
                DINHEIRO = "Dinheiro",
                PAGAMENTO_INSTANTANEO = "PIX / Pagamento Instantâneo",
                VALE_ALIMENTACAO = "Vale Refeição / Alimentação"
            }
        });
    }
}
