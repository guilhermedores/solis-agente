using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Solis.AgentePDV.Security
{
    /// <summary>
    /// Configuração do agente salva em arquivo JSON
    /// </summary>
    public class AgentConfig
    {
        public string ApiUrl { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string Token { get; set; } = "";
        public string InstalledAt { get; set; } = "";
        public string AgentName { get; set; } = "";
    }

    /// <summary>
    /// Gerencia token JWT do tenant com segurança
    /// O tenant está embutido no token e não pode ser alterado sem invalidá-lo
    /// </summary>
    public class TenantTokenManager
    {
        private const string CONFIG_DIR = @"C:\Solis\AgentePDV\data";
        private const string CONFIG_FILE = "agent.config.json";
        
        // Chave secreta compartilhada com a API (deve estar no appsettings.json)
        private readonly string _jwtSecret;
        private readonly string _apiUrl;
        private readonly string _configPath;

        public TenantTokenManager(string jwtSecret, string apiUrl)
        {
            _jwtSecret = jwtSecret ?? throw new ArgumentNullException(nameof(jwtSecret));
            _apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
            _configPath = Path.Combine(CONFIG_DIR, CONFIG_FILE);
        }

        /// <summary>
        /// Salva o token JWT no arquivo de configuração
        /// </summary>
        public void SaveToken(string token, string tenantId, string agentName = "")
        {
            try
            {
                // Cria diretório se não existir
                if (!Directory.Exists(CONFIG_DIR))
                    Directory.CreateDirectory(CONFIG_DIR);

                var config = new AgentConfig
                {
                    ApiUrl = _apiUrl,
                    TenantId = tenantId,
                    Token = token,
                    InstalledAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    AgentName = agentName
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                File.WriteAllText(_configPath, json, Encoding.UTF8);

                Console.WriteLine($"[TenantToken] Token salvo com sucesso em: {_configPath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao salvar token: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Recupera o token do arquivo de configuração
        /// </summary>
        public string? GetToken()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return null;

                var json = File.ReadAllText(_configPath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<AgentConfig>(json);

                return config?.Token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TenantToken] Erro ao recuperar token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtém toda a configuração do agente
        /// </summary>
        public AgentConfig? GetConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return null;

                var json = File.ReadAllText(_configPath, Encoding.UTF8);
                return JsonSerializer.Deserialize<AgentConfig>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TenantToken] Erro ao recuperar configuração: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Valida o token e extrai o tenant
        /// </summary>
        public (bool IsValid, string? TenantId, string? ErrorMessage) ValidateAndExtractTenant(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "solis-api",
                    ValidateAudience = true,
                    ValidAudience = "solis-agente",
                    ValidateLifetime = true, // Verifica expiração
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                
                // Extrai tenant do claim
                var tenantClaim = principal.FindFirst("tenant");
                if (tenantClaim == null)
                    return (false, null, "Token não contém informação de tenant");

                return (true, tenantClaim.Value, null);
            }
            catch (SecurityTokenExpiredException)
            {
                return (false, null, "Token expirado. Reinstale o agente ou solicite novo token.");
            }
            catch (SecurityTokenException ex)
            {
                return (false, null, $"Token inválido: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Erro ao validar token: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém o tenant do token armazenado (valida antes)
        /// </summary>
        public string GetTenantId()
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Token não encontrado. Execute o instalador primeiro.");

            var (isValid, tenantId, errorMessage) = ValidateAndExtractTenant(token);
            
            if (!isValid)
                throw new InvalidOperationException($"Token inválido: {errorMessage}");

            return tenantId!;
        }

        /// <summary>
        /// Remove o arquivo de configuração
        /// </summary>
        public void RemoveToken()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    File.Delete(_configPath);
                    Console.WriteLine("[TenantToken] Configuração removida");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TenantToken] Erro ao remover configuração: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica se existe um token válido configurado
        /// </summary>
        public bool HasValidToken()
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
                return false;

            var (isValid, _, _) = ValidateAndExtractTenant(token);
            return isValid;
        }
    }
}
