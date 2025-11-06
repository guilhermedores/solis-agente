using Microsoft.AspNetCore.Mvc;

namespace Solis.AgentePDV.Controllers;

/// <summary>
/// Controller para verificar status de conexões do sistema
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StatusController> _logger;

    public StatusController(
        IHttpClientFactory httpClientFactory,
        ILogger<StatusController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Retorna status de todas as conexões do sistema
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SystemStatus>> GetStatus()
    {
        var agenteStatus = new ConnectionStatus
        {
            Name = "Agente PDV",
            Connected = true,
            LastCheck = DateTime.UtcNow
        };

        var apiStatus = await CheckApiConnectionAsync();

        return Ok(new SystemStatus
        {
            Agent = agenteStatus,
            Api = apiStatus,
            Timestamp = DateTime.UtcNow
        });
    }

    private async Task<ConnectionStatus> CheckApiConnectionAsync()
    {
        var status = new ConnectionStatus
        {
            Name = "API Solis",
            Connected = false,
            LastCheck = DateTime.UtcNow
        };

        try
        {
            var client = _httpClientFactory.CreateClient("SolisApi");
            
            // Timeout curto para não travar a UI
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            
            // Tenta fazer uma requisição simples
            var response = await client.GetAsync("/api/health", cts.Token);
            
            status.Connected = response.IsSuccessStatusCode;
            status.StatusCode = (int)response.StatusCode;
            
            if (status.Connected)
            {
                status.Message = "Conectado";
            }
            else
            {
                status.Message = $"API retornou status {response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            status.Connected = false;
            status.Message = "Timeout ao conectar com a API";
            _logger.LogWarning("Timeout ao verificar status da API");
        }
        catch (HttpRequestException ex)
        {
            status.Connected = false;
            status.Message = "API indisponível";
            _logger.LogWarning(ex, "Erro ao verificar status da API");
        }
        catch (Exception ex)
        {
            status.Connected = false;
            status.Message = "Erro ao verificar conexão";
            _logger.LogError(ex, "Erro inesperado ao verificar status da API");
        }

        return status;
    }
}

/// <summary>
/// Status geral do sistema
/// </summary>
public class SystemStatus
{
    public ConnectionStatus Agent { get; set; } = new();
    public ConnectionStatus Api { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Status de uma conexão específica
/// </summary>
public class ConnectionStatus
{
    public string Name { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public string? Message { get; set; }
    public int? StatusCode { get; set; }
    public DateTime LastCheck { get; set; }
}
