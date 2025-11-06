using Solis.AgentePDV.Models;
using Solis.AgentePDV.Services;
using Microsoft.AspNetCore.Mvc;

namespace Solis.AgentePDV.Controllers;

/// <summary>
/// Controller para consulta de produtos.
/// Criação/Atualização apenas via sincronização com a API central.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtoService;
    private readonly ILogger<ProdutosController> _logger;

    public ProdutosController(IProdutoService produtoService, ILogger<ProdutosController> logger)
    {
        _produtoService = produtoService;
        _logger = logger;
    }

    /// <summary>
    /// Busca produto por código de barras
    /// </summary>
    [HttpGet("codigo-barras/{codigoBarras}")]
    public async Task<ActionResult<Produto>> BuscarPorCodigoBarras(string codigoBarras)
    {
        var produto = await _produtoService.BuscarPorCodigoBarrasAsync(codigoBarras);
        if (produto == null)
            return NotFound(new { message = "Produto não encontrado" });
        
        return Ok(produto);
    }

    /// <summary>
    /// Busca produtos por nome
    /// </summary>
    [HttpGet("buscar")]
    public async Task<ActionResult<IEnumerable<Produto>>> BuscarPorNome([FromQuery] string termo)
    {
        var produtos = await _produtoService.BuscarPorNomeAsync(termo);
        return Ok(produtos);
    }

    /// <summary>
    /// Lista todos os produtos ativos
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Produto>>> ListarProdutos([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var produtos = await _produtoService.ListarProdutosAsync(skip, take);
        return Ok(produtos);
    }

    /// <summary>
    /// Sincroniza produtos da nuvem para o banco local
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult> SincronizarProdutos()
    {
        try
        {
            await _produtoService.SincronizarProdutosAsync();
            _logger.LogInformation("Produtos sincronizados com sucesso");
            return Ok(new { message = "Produtos sincronizados com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar produtos");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}