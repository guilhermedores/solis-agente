using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Solis.AgentePDV.Services;

/// <summary>
/// Interface para o serviço de Outbox Pattern
/// </summary>
public interface IOutboxService
{
    Task EnqueueAsync<T>(string tipoEntidade, string operacao, Guid entidadeId, T entidade, string endpoint, string metodo = "POST", int prioridade = 0);
    Task<List<OutboxMessage>> ObterMensagensPendentesAsync(int limite = 100);
    Task MarcarComoProcessandoAsync(Guid messageId);
    Task MarcarComoEnviadoAsync(Guid messageId, int statusCode);
    Task MarcarComoErroAsync(Guid messageId, string erro, int? statusCode, DateTime? proximaTentativa);
    Task<int> ObterTotalPendentesAsync();
    Task<int> LimparMensagensAntigasAsync(int diasRetencao = 30);
}

/// <summary>
/// Serviço para gerenciar o Outbox Pattern de sincronização
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(IServiceScopeFactory scopeFactory, ILogger<OutboxService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Adiciona uma mensagem na fila de saída (outbox)
    /// </summary>
    public async Task EnqueueAsync<T>(
        string tipoEntidade, 
        string operacao, 
        Guid entidadeId, 
        T entidade, 
        string endpoint, 
        string metodo = "POST",
        int prioridade = 0)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            
            // Opções de serialização para evitar ciclos
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            };
            
            var message = new OutboxMessage
            {
                TipoEntidade = tipoEntidade,
                Operacao = operacao,
                EntidadeId = entidadeId,
                PayloadJson = JsonSerializer.Serialize(entidade, jsonOptions),
                EndpointApi = endpoint,
                MetodoHttp = metodo,
                Status = "Pendente",
                Prioridade = prioridade,
                CriadoEm = DateTime.UtcNow,
                ProximaTentativaEm = DateTime.UtcNow // Tentar imediatamente
            };

            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Mensagem adicionada ao Outbox: {TipoEntidade} {Operacao} {EntidadeId}", 
                tipoEntidade, operacao, entidadeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Erro ao adicionar mensagem ao Outbox: {TipoEntidade} {Operacao}", 
                tipoEntidade, operacao);
            throw;
        }
    }

    /// <summary>
    /// Obtém mensagens pendentes para processamento
    /// </summary>
    public async Task<List<OutboxMessage>> ObterMensagensPendentesAsync(int limite = 100)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        
        var agora = DateTime.UtcNow;
        
        return await context.OutboxMessages
            .Where(m => m.Status == "Pendente" 
                     && (m.ProximaTentativaEm == null || m.ProximaTentativaEm <= agora)
                     && m.TentativasEnvio < m.MaxTentativas)
            .OrderByDescending(m => m.Prioridade)
            .ThenBy(m => m.CriadoEm)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Marca mensagem como sendo processada
    /// </summary>
    public async Task MarcarComoProcessandoAsync(Guid messageId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        
        var message = await context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = "Processando";
            message.TentativasEnvio++;
            message.UltimaTentativaEm = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Marca mensagem como enviada com sucesso
    /// </summary>
    public async Task MarcarComoEnviadoAsync(Guid messageId, int statusCode)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        
        var message = await context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = "Enviado";
            message.EnviadoEm = DateTime.UtcNow;
            message.UltimoStatusCode = statusCode;
            message.UltimoErro = null;
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Mensagem enviada com sucesso: {MessageId} - {TipoEntidade}", 
                messageId, message.TipoEntidade);
        }
    }

    /// <summary>
    /// Marca mensagem com erro e calcula próxima tentativa (backoff exponencial)
    /// </summary>
    public async Task MarcarComoErroAsync(Guid messageId, string erro, int? statusCode, DateTime? proximaTentativa)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        
        var message = await context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = "Pendente"; // Volta para pendente para nova tentativa
            message.UltimoErro = erro;
            message.UltimoStatusCode = statusCode;
            
            // Backoff exponencial: 1min, 2min, 4min, 8min, 16min
            if (proximaTentativa.HasValue)
            {
                message.ProximaTentativaEm = proximaTentativa;
            }
            else
            {
                var delayMinutos = Math.Pow(2, message.TentativasEnvio - 1);
                message.ProximaTentativaEm = DateTime.UtcNow.AddMinutes(delayMinutos);
            }

            // Se atingiu máximo de tentativas, marcar como erro permanente
            if (message.TentativasEnvio >= message.MaxTentativas)
            {
                message.Status = "Erro";
                _logger.LogError(
                    "Mensagem falhou após {Tentativas} tentativas: {MessageId} - {Erro}", 
                    message.TentativasEnvio, messageId, erro);
            }

            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Obtém total de mensagens pendentes
    /// </summary>
    public async Task<int> ObterTotalPendentesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        return await context.OutboxMessages
            .CountAsync(m => m.Status == "Pendente" && m.TentativasEnvio < m.MaxTentativas);
    }

    /// <summary>
    /// Remove mensagens enviadas antigas para economizar espaço
    /// </summary>
    public async Task<int> LimparMensagensAntigasAsync(int diasRetencao = 30)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        
        var dataLimite = DateTime.UtcNow.AddDays(-diasRetencao);
        
        var mensagensAntigas = await context.OutboxMessages
            .Where(m => m.Status == "Enviado" && m.EnviadoEm < dataLimite)
            .ToListAsync();

        if (mensagensAntigas.Any())
        {
            context.OutboxMessages.RemoveRange(mensagensAntigas);
            await context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Removidas {Count} mensagens antigas do Outbox", 
                mensagensAntigas.Count);
        }

        return mensagensAntigas.Count;
    }
}
