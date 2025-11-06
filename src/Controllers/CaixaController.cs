using Microsoft.AspNetCore.Mvc;
using Solis.AgentePDV.Models;
using Solis.AgentePDV.Services;

namespace Solis.AgentePDV.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaixaController : ControllerBase
{
    private readonly ICaixaService _caixaService;
    private readonly ILogger<CaixaController> _logger;

    public CaixaController(ICaixaService caixaService, ILogger<CaixaController> logger)
    {
        _caixaService = caixaService;
        _logger = logger;
    }

    /// <summary>
    /// Verifica se existe caixa aberto para um terminal
    /// </summary>
    [HttpGet("aberto/{numeroTerminal}")]
    public async Task<ActionResult<Caixa?>> ObterCaixaAberto(int numeroTerminal)
    {
        try
        {
            var caixa = await _caixaService.ObterCaixaAbertoAsync(numeroTerminal);
            
            // Retornar 200 com null em vez de 404 quando não há caixa aberto
            // Isso evita logs de erro desnecessários no console do navegador
            return Ok(caixa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter caixa aberto para terminal {Terminal}", numeroTerminal);
            return StatusCode(500, new { message = "Erro ao obter caixa aberto", error = ex.Message });
        }
    }

    /// <summary>
    /// Verifica se existe caixa aberto
    /// </summary>
    [HttpGet("verificar-aberto/{numeroTerminal}")]
    public async Task<ActionResult<bool>> VerificarCaixaAberto(int numeroTerminal)
    {
        try
        {
            var existe = await _caixaService.ExisteCaixaAbertoAsync(numeroTerminal);
            return Ok(new { numeroTerminal, caixaAberto = existe });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar caixa aberto para terminal {Terminal}", numeroTerminal);
            return StatusCode(500, new { message = "Erro ao verificar caixa", error = ex.Message });
        }
    }

    /// <summary>
    /// Abre um novo caixa
    /// </summary>
    [HttpPost("abrir")]
    public async Task<ActionResult<Caixa>> AbrirCaixa([FromBody] AberturaCaixaDto dto)
    {
        try
        {
            _logger.LogInformation("Solicitação de abertura de caixa - Terminal: {Terminal}, Operador: {Operador}", 
                dto.NumeroTerminal, dto.OperadorNome);

            var caixa = await _caixaService.AbrirCaixaAsync(dto);
            
            return CreatedAtAction(
                nameof(ObterResumoCaixa), 
                new { id = caixa.Id }, 
                caixa);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha na abertura de caixa - Terminal: {Terminal}", dto.NumeroTerminal);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao abrir caixa - Terminal: {Terminal}", dto.NumeroTerminal);
            return StatusCode(500, new { message = "Erro ao abrir caixa", error = ex.Message });
        }
    }

    /// <summary>
    /// Fecha um caixa aberto
    /// </summary>
    [HttpPost("fechar")]
    public async Task<ActionResult<Caixa>> FecharCaixa([FromBody] FechamentoCaixaDto dto)
    {
        try
        {
            _logger.LogInformation("Solicitação de fechamento de caixa {CaixaId}", dto.CaixaId);

            var caixa = await _caixaService.FecharCaixaAsync(dto);
            
            return Ok(caixa);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha no fechamento de caixa {CaixaId}", dto.CaixaId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fechar caixa {CaixaId}", dto.CaixaId);
            return StatusCode(500, new { message = "Erro ao fechar caixa", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém o resumo de um caixa específico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResumoCaixaDto>> ObterResumoCaixa(Guid id)
    {
        try
        {
            var resumo = await _caixaService.ObterResumoCaixaAsync(id);
            
            if (resumo == null)
            {
                return NotFound(new { message = $"Caixa {id} não encontrado" });
            }

            return Ok(resumo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter resumo do caixa {CaixaId}", id);
            return StatusCode(500, new { message = "Erro ao obter resumo do caixa", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista caixas com filtros opcionais
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ResumoCaixaDto>>> ListarCaixas(
        [FromQuery] int? numeroTerminal = null,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var caixas = await _caixaService.ListarCaixasAsync(numeroTerminal, dataInicio, dataFim);
            
            return Ok(new 
            { 
                total = caixas.Count,
                filtros = new 
                {
                    numeroTerminal,
                    dataInicio,
                    dataFim
                },
                caixas 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar caixas");
            return StatusCode(500, new { message = "Erro ao listar caixas", error = ex.Message });
        }
    }
}
