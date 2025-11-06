using Microsoft.EntityFrameworkCore;
using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;

namespace Solis.AgentePDV.Services;

/// <summary>
/// Interface para serviço de status de venda (SOMENTE LEITURA)
/// Os status são gerenciados via integração com a nuvem
/// </summary>
public interface IStatusVendaService
{
    Task<List<StatusVenda>> ListarAsync(bool? ativas = null);
    Task<StatusVenda?> ObterPorIdAsync(Guid id);
    Task<StatusVenda?> ObterPorCodigoAsync(string codigo);
    Task<List<StatusVenda>> ObterAtivasAsync();
}

/// <summary>
/// Serviço SOMENTE LEITURA para status de venda
/// Os status são sincronizados automaticamente via integração
/// </summary>
public class StatusVendaService : IStatusVendaService
{
    private readonly LocalDbContext _context;
    private readonly ILogger<StatusVendaService> _logger;

    public StatusVendaService(LocalDbContext context, ILogger<StatusVendaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<StatusVenda>> ListarAsync(bool? ativas = null)
    {
        try
        {
            var query = _context.StatusVendas.AsQueryable();

            if (ativas.HasValue)
            {
                query = query.Where(s => s.Ativa == ativas.Value);
            }

            return await query
                .OrderBy(s => s.Ordem)
                .ThenBy(s => s.Descricao)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar status de venda");
            throw;
        }
    }

    public async Task<StatusVenda?> ObterPorIdAsync(Guid id)
    {
        try
        {
            return await _context.StatusVendas
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status de venda por ID: {StatusId}", id);
            throw;
        }
    }

    public async Task<StatusVenda?> ObterPorCodigoAsync(string codigo)
    {
        try
        {
            return await _context.StatusVendas
                .FirstOrDefaultAsync(s => s.Codigo == codigo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status de venda por código: {Codigo}", codigo);
            throw;
        }
    }

    public async Task<List<StatusVenda>> ObterAtivasAsync()
    {
        try
        {
            return await _context.StatusVendas
                .Where(s => s.Ativa)
                .OrderBy(s => s.Ordem)
                .ThenBy(s => s.Descricao)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status de venda ativas");
            throw;
        }
    }
}
