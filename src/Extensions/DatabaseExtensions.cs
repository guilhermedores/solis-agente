using Solis.AgentePDV.Data;
using Serilog;

namespace Solis.AgentePDV.Extensions;

/// <summary>
/// Extensões para configuração e inicialização do banco de dados
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Garante que o banco de dados local está criado e configurado corretamente
    /// </summary>
    public static void EnsureLocalDatabaseCreated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        
        try
        {
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var localDb = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            
            // Obter caminho do banco de dados
            var connectionString = configuration.GetConnectionString("LocalDb") ?? "Data Source=C:\\ProgramData\\Solis\\data\\agente-pdv.db";
            Log.Information("Connection String: {ConnectionString}", connectionString);
            
            var dbPath = ExtractDatabasePath(connectionString);
            
            Log.Information("Caminho do banco de dados: {DbPath}", dbPath ?? "NULL");
            
            if (!string.IsNullOrEmpty(dbPath))
            {
                // Criar diretório se necessário
                EnsureDirectoryExists(dbPath);
            }
            
            // Criar/atualizar schema do banco de dados
            var created = localDb.Database.EnsureCreated();
            
            if (created)
            {
                Log.Information("Banco de dados local criado com sucesso com todas as tabelas");
                
                // Seed de dados iniciais
                SeedInitialData(localDb);
            }
            else
            {
                Log.Information("Banco de dados local já existia, schema validado");
            }
            
            // Log informações do banco
            LogDatabaseInfo(dbPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao inicializar banco de dados local");
            throw;
        }
    }
    
    /// <summary>
    /// Popula dados iniciais no banco de dados
    /// </summary>
    private static void SeedInitialData(LocalDbContext context)
    {
        try
        {
            // Seed de Configurações
            if (!context.Configuracoes.Any())
            {
                var configuracoes = new[]
                {
                    new Solis.AgentePDV.Models.Configuracao
                    {
                        Chave = "NumeroTerminal",
                        Valor = "1",
                        CriadoEm = DateTime.UtcNow,
                        AtualizadoEm = DateTime.UtcNow
                    },
                    new Solis.AgentePDV.Models.Configuracao
                    {
                        Chave = "NomeTerminal",
                        Valor = "PDV 01",
                        CriadoEm = DateTime.UtcNow,
                        AtualizadoEm = DateTime.UtcNow
                    }
                };
                
                context.Configuracoes.AddRange(configuracoes);
                context.SaveChanges();
                Log.Information("Configurações iniciais criadas no banco de dados");
            }

            // Seed de StatusVenda
            if (!context.StatusVendas.Any())
            {
                var statusVendas = new[]
                {
                    new Solis.AgentePDV.Models.StatusVenda
                    {
                        Id = Guid.NewGuid(),
                        Codigo = "ABERTA",
                        Descricao = "Aberta",
                        Cor = "#FFA500",
                        Ativa = true,
                        Ordem = 1,
                        PermiteEdicao = true,
                        PermiteCancelamento = true,
                        StatusFinal = false,
                        Sincronizado = true,
                        SincronizadoEm = DateTime.UtcNow
                    },
                    new Solis.AgentePDV.Models.StatusVenda
                    {
                        Id = Guid.NewGuid(),
                        Codigo = "FINALIZADA",
                        Descricao = "Finalizada",
                        Cor = "#28A745",
                        Ativa = true,
                        Ordem = 2,
                        PermiteEdicao = false,
                        PermiteCancelamento = true,
                        StatusFinal = true,
                        Sincronizado = true,
                        SincronizadoEm = DateTime.UtcNow
                    },
                    new Solis.AgentePDV.Models.StatusVenda
                    {
                        Id = Guid.NewGuid(),
                        Codigo = "CANCELADA",
                        Descricao = "Cancelada",
                        Cor = "#DC3545",
                        Ativa = true,
                        Ordem = 3,
                        PermiteEdicao = false,
                        PermiteCancelamento = false,
                        StatusFinal = true,
                        Sincronizado = true,
                        SincronizadoEm = DateTime.UtcNow
                    }
                };
                
                context.StatusVendas.AddRange(statusVendas);
                context.SaveChanges();
                
                Log.Information("Status de venda iniciais criados: {Count} registros", statusVendas.Length);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao popular dados iniciais");
            // Não falha a inicialização se o seed falhar
        }
    }
    
    /// <summary>
    /// Extrai o caminho do arquivo de banco da connection string
    /// </summary>
    private static string? ExtractDatabasePath(string connectionString)
    {
        var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=(.+?)(;|$)");
        return match.Success ? match.Groups[1].Value : null;
    }
    
    /// <summary>
    /// Garante que o diretório do banco de dados existe
    /// </summary>
    private static void EnsureDirectoryExists(string dbPath)
    {
        var dbDirectory = Path.GetDirectoryName(dbPath);
        
        if (string.IsNullOrEmpty(dbDirectory) || Directory.Exists(dbDirectory))
            return;
            
        Directory.CreateDirectory(dbDirectory);
        Log.Information("Diretório de dados criado: {DbDirectory}", dbDirectory);
        
        // Configurar permissões no Windows
        if (OperatingSystem.IsWindows())
        {
            ConfigureWindowsPermissions(dbDirectory);
        }
    }
    
    /// <summary>
    /// Configura permissões de acesso no Windows
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ConfigureWindowsPermissions(string directory)
    {
        try
        {
            var dirInfo = new DirectoryInfo(directory);
            var security = dirInfo.GetAccessControl();
            
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                "Todos",
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.InheritanceFlags.ContainerInherit | 
                System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                System.Security.AccessControl.PropagationFlags.None,
                System.Security.AccessControl.AccessControlType.Allow));
                
            dirInfo.SetAccessControl(security);
            Log.Information("Permissões configuradas para: {Directory}", directory);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Não foi possível configurar permissões. Execute como Administrador ou configure manualmente.");
        }
    }
    
    /// <summary>
    /// Garante que o arquivo de banco de dados existe
    /// </summary>
    private static void EnsureFileExists(string dbPath)
    {
        if (File.Exists(dbPath))
            return;
            
        File.WriteAllBytes(dbPath, Array.Empty<byte>());
        Log.Information("Arquivo de banco criado: {DbPath}", dbPath);
    }
    
    /// <summary>
    /// Loga informações sobre o banco de dados
    /// </summary>
    private static void LogDatabaseInfo(string? dbPath)
    {
        if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            return;
            
        var fileInfo = new FileInfo(dbPath);
        Log.Information("Banco de dados: {DbPath} ({Size} KB)", 
            dbPath, 
            Math.Round(fileInfo.Length / 1024.0, 2));
    }
}
