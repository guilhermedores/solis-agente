namespace Solis.AgentePDV.Extensions;

/// <summary>
/// Extensões para configuração de CORS (Cross-Origin Resource Sharing)
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Adiciona configuração de CORS para multi-tenant
    /// </summary>
    public static IServiceCollection AddSolisCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowPWA", policy =>
            {
                if (environment.IsDevelopment())
                {
                    ConfigureDevelopmentCors(policy);
                }
                else
                {
                    ConfigureProductionCors(policy, configuration);
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Configura CORS para ambiente de desenvolvimento (permite todas as origens)
    /// </summary>
    private static void ConfigureDevelopmentCors(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy)
    {
        // Desenvolvimento: Liberar todas as origens (Postman, testes, etc)
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    }

    /// <summary>
    /// Configura CORS para ambiente de produção (multi-tenant com validação de subdomínios)
    /// </summary>
    private static void ConfigureProductionCors(
        Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, 
        IConfiguration configuration)
    {
        // Produção: Validar subdomínios específicos
        policy.SetIsOriginAllowed(origin => IsOriginAllowed(origin, configuration))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    }

    /// <summary>
    /// Valida se a origem é permitida baseado no domínio configurado
    /// </summary>
    private static bool IsOriginAllowed(string? origin, IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(origin))
            return false;

        try
        {
            var uri = new Uri(origin);
            
            // Permitir localhost para testes
            if (IsLocalhost(uri.Host))
                return true;
            
            // Permitir qualquer subdomínio do domínio configurado
            var allowedDomain = configuration["CORS:AllowedDomain"] ?? "seudominio.com.br";
            return IsSubdomainOf(uri.Host, allowedDomain);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica se o host é localhost
    /// </summary>
    private static bool IsLocalhost(string host)
    {
        return host == "localhost" || 
               host == "127.0.0.1" || 
               host == "::1";
    }

    /// <summary>
    /// Verifica se o host é um subdomínio do domínio permitido
    /// </summary>
    private static bool IsSubdomainOf(string host, string allowedDomain)
    {
        return host.EndsWith("." + allowedDomain, StringComparison.OrdinalIgnoreCase) || 
               host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase);
    }
}
