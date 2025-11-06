using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Solis.AgentePDV.Services;

public class ProdutoService : IProdutoService
{
    private readonly LocalDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogger<ProdutoService> _logger;

    public ProdutoService(
        LocalDbContext context, 
        IHttpClientFactory httpClientFactory, 
        IConfiguracaoService configuracaoService,
        ILogger<ProdutoService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuracaoService = configuracaoService;
        _logger = logger;
    }

    public async Task<Produto?> BuscarPorCodigoBarrasAsync(string codigoBarras)
    {
        var produto = await _context.Produtos
            .AsNoTracking()
            .Include(p => p.PrecoAtual)
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras && p.Ativo);
        if (produto != null) _logger.LogInformation("Produto encontrado: {CodigoBarras} - {Nome}", codigoBarras, produto.Nome);
        else _logger.LogWarning("Produto nao encontrado: {CodigoBarras}", codigoBarras);
        return produto;
    }

    public async Task<Produto?> BuscarPorIdAsync(Guid id)
    {
        return await _context.Produtos
            .AsNoTracking()
            .Include(p => p.PrecoAtual)
            .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);
    }

    public async Task<IEnumerable<Produto>> BuscarPorNomeAsync(string termo)
    {
        return await _context.Produtos
            .AsNoTracking()
            .Include(p => p.PrecoAtual)
            .Where(p => EF.Functions.Like(p.Nome, $"%{termo}%") && p.Ativo)
            .OrderBy(p => p.Nome)
            .Take(20)
            .ToListAsync();
    }

    public async Task<IEnumerable<Produto>> ListarProdutosAsync(int skip, int take)
    {
        return await _context.Produtos
            .AsNoTracking()
            .Include(p => p.PrecoAtual)
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> VerificarDisponibilidadeAsync(Guid produtoId, decimal quantidade)
    {
        var produto = await BuscarPorIdAsync(produtoId);
        if (produto == null || !produto.Ativo) return false;
        return true;
    }

    public async Task SincronizarProdutosAsync()
    {
        var client = _httpClientFactory.CreateClient("SolisApi");
        try
        {
            _logger.LogInformation("Iniciando sincronizacao de produtos...");
            
            // Obtém tenant da configuração (já foi extraído do token JWT)
            var tenantId = _configuracaoService.ObterTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Agente não configurado. TenantId não encontrado. Sincronização cancelada.");
                return;
            }

            var response = await client.GetAsync($"/api/produtos?tenant={tenantId}&limit=1000");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API retornou status {StatusCode} ao buscar produtos", response.StatusCode);
                return;
            }
            
            var responseData = await response.Content.ReadFromJsonAsync<ApiProdutosResponse>();
            if (responseData?.Produtos == null || responseData.Produtos.Count == 0)
            {
                _logger.LogInformation("Nenhum produto retornado da API");
                return;
            }

            var produtosSincronizados = 0;
            var precosSincronizados = 0;

            foreach (var produtoApi in responseData.Produtos)
            {
                // Converter ID string para Guid
                if (!Guid.TryParse(produtoApi.Id, out var produtoId))
                {
                    _logger.LogWarning("ID invalido para produto: {Id}", produtoApi.Id);
                    continue;
                }

                // Sincronizar produto
                var produtoLocal = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == produtoId);
                if (produtoLocal == null)
                {
                    _context.Produtos.Add(new Produto
                    {
                        Id = produtoId,
                        Nome = produtoApi.Nome,
                        Descricao = produtoApi.Descricao,
                        CodigoBarras = produtoApi.CodigoBarras,
                        CodigoInterno = produtoApi.CodigoInterno,
                        NCM = produtoApi.Ncm,
                        CEST = produtoApi.Cest,
                        UnidadeMedida = produtoApi.UnidadeMedida,
                        Ativo = produtoApi.Ativo,
                        CriadoEm = produtoApi.CreatedAt,
                        AtualizadoEm = produtoApi.UpdatedAt,
                        SincronizadoEm = DateTime.UtcNow
                    });
                    produtosSincronizados++;
                }
                else
                {
                    produtoLocal.Nome = produtoApi.Nome;
                    produtoLocal.Descricao = produtoApi.Descricao;
                    produtoLocal.CodigoBarras = produtoApi.CodigoBarras;
                    produtoLocal.CodigoInterno = produtoApi.CodigoInterno;
                    produtoLocal.NCM = produtoApi.Ncm;
                    produtoLocal.CEST = produtoApi.Cest;
                    produtoLocal.UnidadeMedida = produtoApi.UnidadeMedida;
                    produtoLocal.Ativo = produtoApi.Ativo;
                    produtoLocal.AtualizadoEm = DateTime.UtcNow;
                    produtoLocal.SincronizadoEm = DateTime.UtcNow;
                    produtosSincronizados++;
                }

                // Sincronizar preço (se disponível)
                if (produtoApi.PrecoVenda.HasValue && produtoApi.PrecoVenda.Value > 0)
                {
                    var precoLocal = await _context.ProdutoPrecos
                        .FirstOrDefaultAsync(p => p.ProdutoId == produtoId && p.Ativo);
                    
                    if (precoLocal == null)
                    {
                        _context.ProdutoPrecos.Add(new ProdutoPreco
                        {
                            Id = Guid.NewGuid(),
                            ProdutoId = produtoId,
                            PrecoVenda = produtoApi.PrecoVenda.Value,
                            Ativo = true,
                            CriadoEm = DateTime.UtcNow,
                            AtualizadoEm = DateTime.UtcNow,
                            SincronizadoEm = DateTime.UtcNow
                        });
                        precosSincronizados++;
                    }
                    else if (precoLocal.PrecoVenda != produtoApi.PrecoVenda.Value)
                    {
                        precoLocal.PrecoVenda = produtoApi.PrecoVenda.Value;
                        precoLocal.AtualizadoEm = DateTime.UtcNow;
                        precoLocal.SincronizadoEm = DateTime.UtcNow;
                        precosSincronizados++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sincronizacao concluida: {Produtos} produtos, {Precos} precos atualizados", 
                produtosSincronizados, precosSincronizados);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API nao disponivel. Sincronizacao de produtos sera tentada novamente no proximo ciclo");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout ao conectar com a API. Sincronizacao de produtos sera tentada novamente no proximo ciclo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar produtos");
            throw;
        }
    }
}

// DTO para resposta da API
public class ApiProdutosResponse
{
    [JsonPropertyName("produtos")]
    public List<ApiProduto> Produtos { get; set; } = new();
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class ApiProduto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("codigo_barras")]
    public string? CodigoBarras { get; set; }
    
    [JsonPropertyName("codigo_interno")]
    public string? CodigoInterno { get; set; }
    
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [JsonPropertyName("descricao")]
    public string? Descricao { get; set; }
    
    [JsonPropertyName("ncm")]
    public string? Ncm { get; set; }
    
    [JsonPropertyName("cest")]
    public string? Cest { get; set; }
    
    [JsonPropertyName("unidade_medida")]
    public string UnidadeMedida { get; set; } = "UN";
    
    [JsonPropertyName("ativo")]
    public bool Ativo { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [JsonPropertyName("preco_venda")]
    public decimal? PrecoVenda { get; set; }
}