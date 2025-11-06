using Solis.AgentePDV.Services;
using Microsoft.AspNetCore.Mvc;

namespace Solis.AgentePDV.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerifericosController : ControllerBase
{
    private readonly IPerifericoService _perifericoService;
    private readonly IImpressoraService _impressoraService;
    private readonly IGavetaService _gavetaService;
    private readonly ILogger<PerifericosController> _logger;

    public PerifericosController(
        IPerifericoService perifericoService,
        IImpressoraService impressoraService,
        IGavetaService gavetaService,
        ILogger<PerifericosController> logger)
    {
        _perifericoService = perifericoService;
        _impressoraService = impressoraService;
        _gavetaService = gavetaService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém status de todos os periféricos
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> ObterStatus()
    {
        var status = _perifericoService.ObterStatusPerifericos();
        return Ok(status);
    }

    /// <summary>
    /// Imprime um cupom fiscal
    /// </summary>
    [HttpPost("impressora/imprimir-cupom")]
    public async Task<ActionResult> ImprimirCupom([FromBody] object cupomData)
    {
        try
        {
            await _impressoraService.ImprimirCupomAsync(cupomData);
            _logger.LogInformation("Cupom impresso com sucesso");
            return Ok(new { message = "Cupom impresso com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir cupom");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Imprime um texto livre
    /// </summary>
    [HttpPost("impressora/imprimir-texto")]
    public async Task<ActionResult> ImprimirTexto([FromBody] string texto)
    {
        try
        {
            await _impressoraService.ImprimirTextoAsync(texto);
            _logger.LogInformation("Texto impresso com sucesso");
            return Ok(new { message = "Texto impresso com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir texto");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Abre a gaveta de dinheiro
    /// </summary>
    [HttpPost("gaveta/abrir")]
    public async Task<ActionResult> AbrirGaveta()
    {
        try
        {
            await _gavetaService.AbrirGavetaAsync();
            _logger.LogInformation("Gaveta aberta com sucesso");
            return Ok(new { message = "Gaveta aberta com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao abrir gaveta");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Testa a conexão com a impressora
    /// </summary>
    [HttpGet("impressora/testar")]
    public async Task<ActionResult> TestarImpressora()
    {
        try
        {
            var resultado = await _impressoraService.TestarConexaoAsync();
            return Ok(new { success = resultado, message = resultado ? "Impressora conectada" : "Impressora não conectada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar impressora");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}