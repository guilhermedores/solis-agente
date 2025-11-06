using Solis.AgentePDV.Models;
using Solis.AgentePDV.Services;
using Microsoft.AspNetCore.Mvc;

namespace Solis.AgentePDV.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendasController : ControllerBase
{
    private readonly IVendaService _vendaService;
    private readonly ILogger<VendasController> _logger;

    public VendasController(IVendaService vendaService, ILogger<VendasController> logger)
    {
        _vendaService = vendaService;
        _logger = logger;
    }

    /// <summary>
    /// Cria uma nova venda
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Venda>> CriarVenda([FromBody] Venda venda)
    {
        try
        {
            var vendaCriada = await _vendaService.CriarVendaAsync(venda);
            _logger.LogInformation("Venda criada: {VendaId}", vendaCriada.Id);
            return CreatedAtAction(nameof(ObterVenda), new { id = vendaCriada.Id }, vendaCriada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar venda");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém uma venda por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Venda>> ObterVenda(Guid id)
    {
        var venda = await _vendaService.ObterVendaPorIdAsync(id);
        if (venda == null)
            return NotFound();
        
        return Ok(venda);
    }

    /// <summary>
    /// Lista vendas pendentes de sincronização
    /// </summary>
    [HttpGet("pendentes")]
    public async Task<ActionResult<IEnumerable<Venda>>> ListarVendasPendentes()
    {
        var vendas = await _vendaService.ListarVendasPendentesAsync();
        return Ok(vendas);
    }

    /// <summary>
    /// Finaliza uma venda (registra pagamentos e imprime cupom)
    /// </summary>
    [HttpPost("{id}/finalizar")]
    public async Task<ActionResult> FinalizarVenda(Guid id, [FromBody] List<VendaPagamento> pagamentos)
    {
        try
        {
            await _vendaService.FinalizarVendaAsync(id, pagamentos);
            _logger.LogInformation("Venda finalizada: {VendaId}", id);
            return Ok(new { message = "Venda finalizada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar venda {VendaId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela uma venda
    /// </summary>
    [HttpPost("{id}/cancelar")]
    public async Task<ActionResult> CancelarVenda(Guid id, [FromBody] string motivo)
    {
        try
        {
            await _vendaService.CancelarVendaAsync(id, motivo);
            _logger.LogInformation("Venda cancelada: {VendaId}", id);
            return Ok(new { message = "Venda cancelada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar venda {VendaId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}