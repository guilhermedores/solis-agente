using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;

namespace Solis.AgentePDV.Services;

public interface ICaixaService
{
    Task<Caixa?> ObterCaixaAbertoAsync(int numeroTerminal);
    Task<Caixa> AbrirCaixaAsync(AberturaCaixaDto dto);
    Task<Caixa> FecharCaixaAsync(FechamentoCaixaDto dto);
    Task<ResumoCaixaDto?> ObterResumoCaixaAsync(Guid caixaId);
    Task<List<ResumoCaixaDto>> ListarCaixasAsync(int? numeroTerminal = null, DateTime? dataInicio = null, DateTime? dataFim = null);
    Task<bool> ExisteCaixaAbertoAsync(int numeroTerminal);
}

public class CaixaService : ICaixaService
{
    private readonly LocalDbContext _context;
    private readonly ILogger<CaixaService> _logger;
    private readonly IOutboxService _outboxService;

    public CaixaService(
        LocalDbContext context,
        ILogger<CaixaService> logger,
        IOutboxService outboxService)
    {
        _context = context;
        _logger = logger;
        _outboxService = outboxService;
    }

    public async Task<Caixa?> ObterCaixaAbertoAsync(int numeroTerminal)
    {
        return await _context.Caixas
            .FirstOrDefaultAsync(c => c.NumeroTerminal == numeroTerminal && c.Status == "Aberto");
    }

    public async Task<bool> ExisteCaixaAbertoAsync(int numeroTerminal)
    {
        return await _context.Caixas
            .AnyAsync(c => c.NumeroTerminal == numeroTerminal && c.Status == "Aberto");
    }

    public async Task<Caixa> AbrirCaixaAsync(AberturaCaixaDto dto)
    {
        _logger.LogInformation("Abrindo caixa para terminal {Terminal}, operador: {Operador}", 
            dto.NumeroTerminal, dto.OperadorNome);

        // Validar se já existe caixa aberto para este terminal
        var caixaExistente = await ObterCaixaAbertoAsync(dto.NumeroTerminal);
        if (caixaExistente != null)
        {
            _logger.LogWarning("Tentativa de abrir caixa com caixa já aberto. Terminal: {Terminal}", dto.NumeroTerminal);
            throw new InvalidOperationException($"Já existe um caixa aberto para o terminal {dto.NumeroTerminal}");
        }

        var caixa = new Caixa
        {
            Id = Guid.NewGuid(),
            NumeroTerminal = dto.NumeroTerminal,
            OperadorId = dto.OperadorId,
            OperadorNome = dto.OperadorNome,
            DataAbertura = DateTime.UtcNow,
            ValorAbertura = dto.ValorAbertura,
            Status = "Aberto",
            Observacoes = dto.Observacoes,
            Sincronizado = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Caixas.Add(caixa);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Caixa {CaixaId} aberto com sucesso. Terminal: {Terminal}, Valor: {Valor}", 
            caixa.Id, caixa.NumeroTerminal, caixa.ValorAbertura);

        // Enviar para outbox para sincronização
        await _outboxService.EnqueueAsync(
            tipoEntidade: "Caixa",
            operacao: "Abertura",
            entidadeId: caixa.Id,
            entidade: caixa,
            endpoint: "/api/caixas",
            metodo: "POST"
        );

        return caixa;
    }

    public async Task<Caixa> FecharCaixaAsync(FechamentoCaixaDto dto)
    {
        _logger.LogInformation("Fechando caixa {CaixaId}", dto.CaixaId);

        var caixa = await _context.Caixas
            .Include(c => c.Vendas)
                .ThenInclude(v => v.Pagamentos)
                    .ThenInclude(p => p.FormaPagamento)
            .FirstOrDefaultAsync(c => c.Id == dto.CaixaId);

        if (caixa == null)
        {
            _logger.LogWarning("Caixa {CaixaId} não encontrado", dto.CaixaId);
            throw new InvalidOperationException("Caixa não encontrado");
        }

        if (caixa.Status != "Aberto")
        {
            _logger.LogWarning("Tentativa de fechar caixa que não está aberto. Status atual: {Status}", caixa.Status);
            throw new InvalidOperationException($"Caixa não pode ser fechado. Status atual: {caixa.Status}");
        }

        // Calcular totais de vendas - carregar status para filtrar
        foreach (var venda in caixa.Vendas)
        {
            await _context.Entry(venda).Reference(v => v.StatusVenda).LoadAsync();
        }
        
        var vendas = caixa.Vendas.Where(v => v.StatusVenda != null && v.StatusVenda.Codigo == "FINALIZADA").ToList();
        
        caixa.QuantidadeVendas = vendas.Count();
        caixa.TotalVendas = vendas.Sum(v => v.ValorLiquido);
        
        // Calcular totais por forma de pagamento usando o Tipo da forma de pagamento
        var todosPagamentos = vendas.SelectMany(v => v.Pagamentos).ToList();
        
        caixa.TotalDinheiro = todosPagamentos
            .Where(p => p.FormaPagamento != null && p.FormaPagamento.Tipo == TipoFormaPagamento.DINHEIRO)
            .Sum(p => p.Valor);
            
        caixa.TotalDebito = todosPagamentos
            .Where(p => p.FormaPagamento != null && p.FormaPagamento.Tipo == TipoFormaPagamento.DEBITO)
            .Sum(p => p.Valor);
            
        caixa.TotalCredito = todosPagamentos
            .Where(p => p.FormaPagamento != null && p.FormaPagamento.Tipo == TipoFormaPagamento.CREDITO)
            .Sum(p => p.Valor);
            
        caixa.TotalPix = todosPagamentos
            .Where(p => p.FormaPagamento != null && p.FormaPagamento.Tipo == TipoFormaPagamento.PAGAMENTO_INSTANTANEO)
            .Sum(p => p.Valor);
            
        caixa.TotalOutros = todosPagamentos
            .Where(p => p.FormaPagamento != null && p.FormaPagamento.Tipo == TipoFormaPagamento.VALE_ALIMENTACAO)
            .Sum(p => p.Valor);

        // Calcular diferença
        var valorEsperado = caixa.ValorAbertura + caixa.TotalDinheiro;
        caixa.Diferenca = dto.ValorFechamento - valorEsperado;

        // Atualizar dados de fechamento
        caixa.DataFechamento = DateTime.UtcNow;
        caixa.ValorFechamento = dto.ValorFechamento;
        caixa.Status = "Fechado";
        caixa.UpdatedAt = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(dto.Observacoes))
        {
            caixa.Observacoes = string.IsNullOrEmpty(caixa.Observacoes) 
                ? dto.Observacoes 
                : $"{caixa.Observacoes}\n[Fechamento] {dto.Observacoes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Caixa {CaixaId} fechado. Vendas: {Qtd}, Total: {Total}, Diferença: {Diferenca}", 
            caixa.Id, caixa.QuantidadeVendas, caixa.TotalVendas, caixa.Diferenca);

        // Enviar para outbox para sincronização
        await _outboxService.EnqueueAsync(
            tipoEntidade: "Caixa",
            operacao: "Fechamento",
            entidadeId: caixa.Id,
            entidade: caixa,
            endpoint: $"/api/caixas/{caixa.Id}",
            metodo: "PUT"
        );

        return caixa;
    }

    public async Task<ResumoCaixaDto?> ObterResumoCaixaAsync(Guid caixaId)
    {
        var caixa = await _context.Caixas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caixaId);

        if (caixa == null)
            return null;

        return MapearParaResumo(caixa);
    }

    public async Task<List<ResumoCaixaDto>> ListarCaixasAsync(
        int? numeroTerminal = null, 
        DateTime? dataInicio = null, 
        DateTime? dataFim = null)
    {
        var query = _context.Caixas.AsNoTracking().AsQueryable();

        if (numeroTerminal.HasValue)
        {
            query = query.Where(c => c.NumeroTerminal == numeroTerminal.Value);
        }

        if (dataInicio.HasValue)
        {
            query = query.Where(c => c.DataAbertura >= dataInicio.Value);
        }

        if (dataFim.HasValue)
        {
            var dataFimFinal = dataFim.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(c => c.DataAbertura <= dataFimFinal);
        }

        var caixas = await query
            .OrderByDescending(c => c.DataAbertura)
            .ToListAsync();

        return caixas.Select(MapearParaResumo).ToList();
    }

    private ResumoCaixaDto MapearParaResumo(Caixa caixa)
    {
        return new ResumoCaixaDto
        {
            Id = caixa.Id,
            NumeroTerminal = caixa.NumeroTerminal,
            OperadorNome = caixa.OperadorNome,
            DataAbertura = caixa.DataAbertura,
            DataFechamento = caixa.DataFechamento,
            ValorAbertura = caixa.ValorAbertura,
            ValorFechamento = caixa.ValorFechamento,
            TotalVendas = caixa.TotalVendas,
            TotalDinheiro = caixa.TotalDinheiro,
            TotalDebito = caixa.TotalDebito,
            TotalCredito = caixa.TotalCredito,
            TotalPix = caixa.TotalPix,
            TotalOutros = caixa.TotalOutros,
            QuantidadeVendas = caixa.QuantidadeVendas,
            Diferenca = caixa.Diferenca,
            Status = caixa.Status,
            ValorEsperado = caixa.ValorAbertura + caixa.TotalDinheiro,
            Observacoes = caixa.Observacoes
        };
    }
}
