using Solis.AgentePDV.Services;

namespace Solis.AgentePDV;

public class SyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var syncEnabled = _configuration.GetValue<bool>("Sync:Enabled", true);
        var intervalSeconds = _configuration.GetValue<int>("Sync:IntervalSeconds", 300);

        if (!syncEnabled)
        {
            _logger.LogInformation("Sincronizacao automatica desabilitada");
            return;
        }

        _logger.LogInformation("Servico de sincronizacao iniciado. Intervalo: {Interval}s", intervalSeconds);

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("=== Iniciando ciclo de sincronizacao ===");

                using var scope = _scopeFactory.CreateScope();

                await SincronizarDadosFiscaisAsync(scope);
                await SincronizarProdutosAsync(scope);
                await SincronizarPrecosAsync(scope);
                await SincronizarVendasAsync(scope);

                _logger.LogInformation("=== Ciclo de sincronizacao concluido ===");

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Sincronizacao cancelada");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no servico de sincronizacao");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        _logger.LogInformation("Servico de sincronizacao finalizado");
    }

    private async Task SincronizarDadosFiscaisAsync(IServiceScope scope)
    {
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<SincronizacaoFiscalService>();
            await service.SincronizarDadosFiscaisAsync();
            _logger.LogInformation("[OK] Dados fiscais sincronizados");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AVISO] Falha ao sincronizar dados fiscais");
        }
    }

    private async Task SincronizarProdutosAsync(IServiceScope scope)
    {
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<IProdutoService>();
            await service.SincronizarProdutosAsync();
            _logger.LogInformation("[OK] Produtos sincronizados");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AVISO] Falha ao sincronizar produtos");
        }
    }

    private async Task SincronizarPrecosAsync(IServiceScope scope)
    {
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<IPrecoService>();
            await service.SincronizarPrecosAsync();
            _logger.LogInformation("[OK] Precos sincronizados");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AVISO] Falha ao sincronizar precos");
        }
    }

    private async Task SincronizarVendasAsync(IServiceScope scope)
    {
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<IVendaService>();
            var sincronizadas = await service.SincronizarVendasAsync();
            if (sincronizadas)
            {
                _logger.LogInformation("[OK] Vendas sincronizadas");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AVISO] Falha ao sincronizar vendas");
        }
    }
}