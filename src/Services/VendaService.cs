using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;

namespace Solis.AgentePDV.Services;

public class VendaService : IVendaService
{
    private readonly LocalDbContext _context;
    private readonly IImpressoraService _impressoraService;
    private readonly IOutboxService _outboxService;
    private readonly IStatusVendaService _statusVendaService;
    private readonly ILogger<VendaService> _logger;

    public VendaService(
        LocalDbContext context,
        IImpressoraService impressoraService,
        IOutboxService outboxService,
        IStatusVendaService statusVendaService,
        ILogger<VendaService> logger)
    {
        _context = context;
        _impressoraService = impressoraService;
        _outboxService = outboxService;
        _statusVendaService = statusVendaService;
        _logger = logger;
    }

    public async Task<Venda> CriarVendaAsync(Venda venda)
    {
        venda.Id = Guid.NewGuid();
        venda.CreatedAt = DateTime.UtcNow;
        venda.UpdatedAt = DateTime.UtcNow;
        
        // Buscar status "ABERTA"
        var statusAberta = await _statusVendaService.ObterPorCodigoAsync("ABERTA");
        if (statusAberta == null)
        {
            throw new InvalidOperationException("Status 'ABERTA' não encontrado. Sincronize os dados primeiro.");
        }
        venda.StatusVendaId = statusAberta.Id;
        venda.Sincronizado = false;

        // Gerar número do cupom
        var ultimaVenda = await _context.Vendas
            .OrderByDescending(v => v.NumeroCupom)
            .FirstOrDefaultAsync();
        
        venda.NumeroCupom = (ultimaVenda?.NumeroCupom ?? 0) + 1;

        _context.Vendas.Add(venda);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Venda criada localmente: {VendaId} - Cupom: {NumeroCupom}", venda.Id, venda.NumeroCupom);

        // Adicionar ao Outbox para sincronização
        await _outboxService.EnqueueAsync(
            tipoEntidade: "Venda",
            operacao: "Create",
            entidadeId: venda.Id,
            entidade: venda,
            endpoint: "/api/vendas",
            metodo: "POST",
            prioridade: 5 // Vendas têm prioridade média
        );

        return venda;
    }

    public async Task<Venda?> ObterVendaPorIdAsync(Guid id)
    {
        return await _context.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Pagamentos)
                .ThenInclude(p => p.FormaPagamento)
            .Include(v => v.StatusVenda)
            .Include(v => v.Caixa)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Venda>> ListarVendasPendentesAsync()
    {
        return await _context.Vendas
            .Where(v => !v.Sincronizado)
            .Include(v => v.Itens)
            .Include(v => v.Pagamentos)
                .ThenInclude(p => p.FormaPagamento)
            .Include(v => v.StatusVenda)
            .Include(v => v.Caixa)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task FinalizarVendaAsync(Guid vendaId, List<VendaPagamento> pagamentos)
    {
        var venda = await ObterVendaPorIdAsync(vendaId);
        if (venda == null)
            throw new Exception("Venda não encontrada");

        // Carregar status atual
        await _context.Entry(venda).Reference(v => v.StatusVenda).LoadAsync();
        
        if (venda.StatusVenda?.Codigo != "ABERTA")
            throw new Exception("Venda já foi finalizada ou cancelada");

        // Adicionar pagamentos
        foreach (var pagamento in pagamentos)
        {
            pagamento.VendaId = vendaId;
            pagamento.CreatedAt = DateTime.UtcNow;
            _context.VendaPagamentos.Add(pagamento);
        }

        // Atualizar status da venda para FINALIZADA
        var statusFinalizada = await _statusVendaService.ObterPorCodigoAsync("FINALIZADA");
        if (statusFinalizada == null)
        {
            throw new InvalidOperationException("Status 'FINALIZADA' não encontrado. Sincronize os dados primeiro.");
        }
        venda.StatusVendaId = statusFinalizada.Id;
        venda.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Adicionar ao Outbox para sincronização com alta prioridade
        await _outboxService.EnqueueAsync(
            tipoEntidade: "Venda",
            operacao: "Finalizar",
            entidadeId: vendaId,
            entidade: new { VendaId = vendaId, Pagamentos = pagamentos },
            endpoint: $"/api/vendas/{vendaId}/finalizar",
            metodo: "POST",
            prioridade: 10 // Vendas finalizadas têm prioridade alta
        );

        // Imprimir cupom
        try
        {
            await _impressoraService.ImprimirCupomAsync(venda);
            _logger.LogInformation("Cupom impresso para venda {VendaId}", vendaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir cupom para venda {VendaId}", vendaId);
            // Não falhar a venda se a impressão falhar
        }
    }

    public async Task CancelarVendaAsync(Guid vendaId, string motivo)
    {
        var venda = await ObterVendaPorIdAsync(vendaId);
        if (venda == null)
            throw new Exception("Venda não encontrada");

        // Atualizar status da venda para CANCELADA
        var statusCancelada = await _statusVendaService.ObterPorCodigoAsync("CANCELADA");
        if (statusCancelada == null)
        {
            throw new InvalidOperationException("Status 'CANCELADA' não encontrado. Sincronize os dados primeiro.");
        }
        venda.StatusVendaId = statusCancelada.Id;
        venda.Observacoes = $"CANCELADA: {motivo}";
        venda.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Adicionar ao Outbox para sincronização
        await _outboxService.EnqueueAsync(
            tipoEntidade: "Venda",
            operacao: "Cancelar",
            entidadeId: vendaId,
            entidade: new { VendaId = vendaId, Motivo = motivo },
            endpoint: $"/api/vendas/{vendaId}/cancelar",
            metodo: "POST",
            prioridade: 8 // Cancelamentos têm prioridade média-alta
        );

        _logger.LogInformation("Venda cancelada: {VendaId} - Motivo: {Motivo}", vendaId, motivo);
    }

    public async Task<bool> SincronizarVendasAsync()
    {
        // Método mantido para compatibilidade, mas agora apenas verifica status
        var totalPendentes = await _outboxService.ObterTotalPendentesAsync();
        _logger.LogInformation("Total de mensagens pendentes no Outbox: {Total}", totalPendentes);
        return totalPendentes == 0;
    }
}