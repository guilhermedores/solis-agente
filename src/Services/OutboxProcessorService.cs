using Solis.AgentePDV.Services;
using System.Net.Http.Json;

namespace Solis.AgentePDV;

/// <summary>
/// Background Service que processa as mensagens do Outbox
/// Envia para a API na nuvem de forma assíncrona e confiável
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _intervaloProcessamento;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        // Intervalo configurável (padrão: 10 segundos)
        var intervalSeconds = configuration.GetValue<int>("Outbox:IntervaloSegundos", 10);
        _intervaloProcessamento = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service iniciado");

        // Aguardar 10 segundos antes de iniciar (para garantir que a aplicação está pronta)
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar Outbox");
            }

            await Task.Delay(_intervaloProcessamento, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Service finalizado");
    }

    private async Task ProcessarOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("SolisApi");

        // Obter mensagens pendentes
        var mensagens = await outboxService.ObterMensagensPendentesAsync(limite: 50);

        if (mensagens.Count == 0)
        {
            return; // Nada para processar
        }

        _logger.LogInformation("Processando {Count} mensagens do Outbox", mensagens.Count);

        foreach (var mensagem in mensagens)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await outboxService.MarcarComoProcessandoAsync(mensagem.Id);

                // Enviar para API
                var sucesso = await EnviarParaApiAsync(httpClient, mensagem, cancellationToken);

                if (sucesso)
                {
                    await outboxService.MarcarComoEnviadoAsync(mensagem.Id, 200);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, 
                    "Erro HTTP ao enviar mensagem {MessageId}: {Message}", 
                    mensagem.Id, ex.Message);
                
                var statusCode = ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : 0;
                await outboxService.MarcarComoErroAsync(
                    mensagem.Id, 
                    ex.Message, 
                    statusCode, 
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Erro ao processar mensagem {MessageId}", 
                    mensagem.Id);
                
                await outboxService.MarcarComoErroAsync(
                    mensagem.Id, 
                    ex.Message, 
                    null, 
                    null);
            }
        }

        // Limpar mensagens antigas a cada 100 execuções
        if (Random.Shared.Next(100) == 0)
        {
            var diasRetencao = _configuration.GetValue<int>("Outbox:DiasRetencao", 30);
            await outboxService.LimparMensagensAntigasAsync(diasRetencao);
        }
    }

    private async Task<bool> EnviarParaApiAsync(
        HttpClient httpClient, 
        Models.OutboxMessage mensagem, 
        CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response;

            // Criar request com o payload JSON
            var content = new StringContent(
                mensagem.PayloadJson, 
                System.Text.Encoding.UTF8, 
                "application/json");

            // Enviar conforme método HTTP
            response = mensagem.MetodoHttp.ToUpper() switch
            {
                "POST" => await httpClient.PostAsync(mensagem.EndpointApi, content, cancellationToken),
                "PUT" => await httpClient.PutAsync(mensagem.EndpointApi, content, cancellationToken),
                "PATCH" => await httpClient.PatchAsync(mensagem.EndpointApi, content, cancellationToken),
                "DELETE" => await httpClient.DeleteAsync(mensagem.EndpointApi, cancellationToken),
                _ => throw new InvalidOperationException($"Método HTTP não suportado: {mensagem.MetodoHttp}")
            };

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Mensagem enviada com sucesso: {MessageId} - {Endpoint} - Status: {StatusCode}",
                    mensagem.Id, mensagem.EndpointApi, response.StatusCode);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Falha ao enviar mensagem {MessageId}: Status {StatusCode} - {Error}",
                    mensagem.Id, response.StatusCode, errorContent);
                
                throw new HttpRequestException(
                    $"Status {response.StatusCode}: {errorContent}", 
                    null, 
                    response.StatusCode);
            }
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("Timeout ao enviar mensagem {MessageId}: {Message}", 
                mensagem.Id, ex.Message);
            throw new HttpRequestException("Timeout na requisição", ex);
        }
    }
}
