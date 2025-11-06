using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Solis.AgentePDV.Services
{
    /// <summary>
    /// Cliente HTTP configurado para comunicação com a API Solis
    /// Envia automaticamente o token JWT de autenticação do tenant
    /// </summary>
    public class SolisApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly string _tenantId;

        public SolisApiClient(string apiUrl, string token, string tenantId)
        {
            _token = token ?? throw new ArgumentNullException(nameof(token));
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Configura headers padrão
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Solis-AgentePDV/1.0");
            
            // Header com token JWT (API valida e extrai tenant)
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            
            // Header adicional com tenant (redundante, mas útil para logs)
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", _tenantId);
        }

        /// <summary>
        /// GET genérico
        /// </summary>
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[API] Erro GET {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// POST genérico
        /// </summary>
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[API] Erro POST {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// PUT genérico
        /// </summary>
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[API] Erro PUT {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// DELETE genérico
        /// </summary>
        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[API] Erro DELETE {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Testa conectividade com a API
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
