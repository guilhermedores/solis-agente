using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;

namespace Solis.AgentePDV.Services;

/// <summary>
/// Serviço para gerenciamento de preços locais
/// </summary>
public class PrecoService : IPrecoService
{
    private readonly LocalDbContext _context;
    private readonly ILogger<PrecoService> _logger;

    public PrecoService(LocalDbContext context, ILogger<PrecoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProdutoPreco?> ObterPrecoAtualAsync(Guid produtoId)
    {
        return await _context.ProdutoPrecos
            .Where(p => p.ProdutoId == produtoId && p.Ativo)
            .FirstOrDefaultAsync();
    }

    public async Task AtualizarPrecoAsync(ProdutoPreco preco)
    {
        preco.AtualizadoEm = DateTime.UtcNow;
        _context.ProdutoPrecos.Update(preco);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Preço atualizado: Produto {ProdutoId}, Preço: {Preco}", 
            preco.ProdutoId, preco.PrecoVenda);
    }

    public async Task SincronizarPrecosAsync()
    {
        _logger.LogInformation("Sincronizando preços com a nuvem...");
        
        // A sincronização real será feita através do Outbox
        // Este método apenas marca a data de sincronização
        
        var precos = await _context.ProdutoPrecos
            .Where(p => p.SincronizadoEm == null || p.AtualizadoEm > p.SincronizadoEm)
            .ToListAsync();
        
        _logger.LogInformation("Total de preços pendentes de sincronização: {Total}", precos.Count);
        
        // Aqui a API retornará os preços atualizados
        // Por enquanto apenas logamos
    }
}
