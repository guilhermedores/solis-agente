using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;

namespace Solis.AgentePDV.Services;

/// <summary>
/// Interface para serviço de formas de pagamento (somente leitura e sincronização)
/// </summary>
public interface IFormaPagamentoService
{
    Task<FormaPagamento?> ObterPorIdAsync(Guid id);
    Task<List<FormaPagamento>> ListarAsync(bool? ativas = null, string? tipo = null);
    Task<List<FormaPagamento>> ListarAtivasAsync();
    Task SincronizarFormasPagamentoAsync(List<FormaPagamento> formasPagamento);
}

/// <summary>
/// Serviço para gerenciar formas de pagamento.
/// Criação/Atualização apenas via sincronização com a API central.
/// </summary>
public class FormaPagamentoService : IFormaPagamentoService
{
    private readonly LocalDbContext _context;
    private readonly ILogger<FormaPagamentoService> _logger;

    public FormaPagamentoService(
        LocalDbContext context,
        ILogger<FormaPagamentoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FormaPagamento?> ObterPorIdAsync(Guid id)
    {
        return await _context.FormasPagamento
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<List<FormaPagamento>> ListarAsync(bool? ativas = null, string? tipo = null)
    {
        var query = _context.FormasPagamento.AsNoTracking().AsQueryable();

        if (ativas.HasValue)
        {
            query = query.Where(f => f.Ativa == ativas.Value);
        }

        if (!string.IsNullOrEmpty(tipo))
        {
            query = query.Where(f => f.Tipo == tipo.ToUpper());
        }

        return await query
            .OrderBy(f => f.Ordem)
            .ThenBy(f => f.Descricao)
            .ToListAsync();
    }

    public async Task<List<FormaPagamento>> ListarAtivasAsync()
    {
        return await _context.FormasPagamento
            .AsNoTracking()
            .Where(f => f.Ativa)
            .OrderBy(f => f.Ordem)
            .ThenBy(f => f.Descricao)
            .ToListAsync();
    }

    /// <summary>
    /// Sincroniza formas de pagamento vindas da API central
    /// </summary>
    public async Task SincronizarFormasPagamentoAsync(List<FormaPagamento> formasPagamento)
    {
        _logger.LogInformation("Sincronizando {Count} formas de pagamento", formasPagamento.Count);

        foreach (var forma in formasPagamento)
        {
            var existente = await _context.FormasPagamento
                .FirstOrDefaultAsync(f => f.ExternalId == forma.ExternalId);

            if (existente != null)
            {
                // Atualizar
                existente.Descricao = forma.Descricao;
                existente.Tipo = forma.Tipo;
                existente.Codigo = forma.Codigo;
                existente.Ativa = forma.Ativa;
                existente.PermiteTroco = forma.PermiteTroco;
                existente.MaximoParcelas = forma.MaximoParcelas;
                existente.TaxaJuros = forma.TaxaJuros;
                existente.RequerTEF = forma.RequerTEF;
                existente.Bandeira = forma.Bandeira;
                existente.Ordem = forma.Ordem;
                existente.UpdatedAt = DateTime.UtcNow;
                existente.Sincronizado = true;
                
                _logger.LogInformation("Forma de pagamento atualizada: {Id} - {Descricao}", existente.Id, existente.Descricao);
            }
            else
            {
                // Inserir
                forma.Sincronizado = true;
                forma.CreatedAt = DateTime.UtcNow;
                forma.UpdatedAt = DateTime.UtcNow;
                _context.FormasPagamento.Add(forma);
                
                _logger.LogInformation("Forma de pagamento criada: {Id} - {Descricao}", forma.Id, forma.Descricao);
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Formas de pagamento sincronizadas com sucesso");
    }
}
