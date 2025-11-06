using Microsoft.EntityFrameworkCore;
using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using System.Text.Json;

namespace Solis.AgentePDV.Services;

/// <summary>
/// Serviço de sincronização de empresas com a API
/// </summary>
public class EmpresaService
{
    private readonly LocalDbContext _context;
    private readonly IConfiguracaoService _configuracaoService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmpresaService> _logger;

    public EmpresaService(
        LocalDbContext context,
        IConfiguracaoService configuracaoService,
        IHttpClientFactory httpClientFactory,
        ILogger<EmpresaService> logger)
    {
        _context = context;
        _configuracaoService = configuracaoService;
        _httpClient = httpClientFactory.CreateClient("SolisApi");
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza empresas da API
    /// </summary>
    public async Task<SyncEmpresasResult> SincronizarEmpresasAsync()
    {
        try
        {
            var tenantId = _configuracaoService.ObterTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Agente não configurado. Não é possível sincronizar empresas.");
                return new SyncEmpresasResult
                {
                    Sucesso = false,
                    Mensagem = "Agente não configurado"
                };
            }

            // Fazer requisição para API (GET /api/empresas)
            var response = await _httpClient.GetAsync("/api/empresas?ativo=true");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SyncEmpresasResponse>();
            
            if (result == null || result.Empresas == null)
            {
                throw new Exception("Falha ao sincronizar empresas");
            }

            // Processar empresas recebidas
            var total = 0;
            var novos = 0;
            var atualizados = 0;

            foreach (var empresaDto in result.Empresas)
            {
                var empresaExistente = await _context.Empresas
                    .FirstOrDefaultAsync(e => e.Id == Guid.Parse(empresaDto.Id));

                if (empresaExistente == null)
                {
                    // Nova empresa
                    var novaEmpresa = new Empresa
                    {
                        Id = Guid.Parse(empresaDto.Id),
                        RazaoSocial = empresaDto.RazaoSocial,
                        NomeFantasia = empresaDto.NomeFantasia,
                        CNPJ = LimparCnpj(empresaDto.Cnpj),
                        InscricaoEstadual = empresaDto.InscricaoEstadual,
                        InscricaoMunicipal = empresaDto.InscricaoMunicipal,
                        Logradouro = empresaDto.Logradouro,
                        Numero = empresaDto.Numero,
                        Complemento = empresaDto.Complemento,
                        Bairro = empresaDto.Bairro,
                        Cidade = empresaDto.Cidade,
                        UF = empresaDto.Uf,
                        CEP = LimparCep(empresaDto.Cep),
                        Telefone = empresaDto.Telefone,
                        Email = empresaDto.Email,
                        RegimeTributarioId = MapearRegimeTributario(empresaDto.RegimeTributario),
                        Ativo = true,
                        SincronizadoEm = DateTime.Now
                    };

                    _context.Empresas.Add(novaEmpresa);
                    novos++;
                }
                else
                {
                    // Atualizar empresa existente
                    empresaExistente.RazaoSocial = empresaDto.RazaoSocial;
                    empresaExistente.NomeFantasia = empresaDto.NomeFantasia;
                    empresaExistente.CNPJ = LimparCnpj(empresaDto.Cnpj);
                    empresaExistente.InscricaoEstadual = empresaDto.InscricaoEstadual;
                    empresaExistente.InscricaoMunicipal = empresaDto.InscricaoMunicipal;
                    empresaExistente.Logradouro = empresaDto.Logradouro;
                    empresaExistente.Numero = empresaDto.Numero;
                    empresaExistente.Complemento = empresaDto.Complemento;
                    empresaExistente.Bairro = empresaDto.Bairro;
                    empresaExistente.Cidade = empresaDto.Cidade;
                    empresaExistente.UF = empresaDto.Uf;
                    empresaExistente.CEP = LimparCep(empresaDto.Cep);
                    empresaExistente.Telefone = empresaDto.Telefone;
                    empresaExistente.Email = empresaDto.Email;
                    empresaExistente.RegimeTributarioId = MapearRegimeTributario(empresaDto.RegimeTributario);
                    empresaExistente.AtualizadoEm = DateTime.Now;
                    empresaExistente.SincronizadoEm = DateTime.Now;

                    atualizados++;
                }

                total++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Empresas sincronizadas: {Total} total ({Novos} novos, {Atualizados} atualizados)",
                total, novos, atualizados);

            return new SyncEmpresasResult
            {
                Sucesso = true,
                Total = total,
                Novos = novos,
                Atualizados = atualizados,
                Mensagem = $"Sincronização concluída: {total} empresas ({novos} novas, {atualizados} atualizadas)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar empresas");
            return new SyncEmpresasResult
            {
                Sucesso = false,
                Mensagem = $"Erro: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Obter empresa principal (primeira ativa)
    /// </summary>
    public async Task<Empresa?> ObterEmpresaPrincipalAsync()
    {
        return await _context.Empresas
            .Include(e => e.RegimeTributario)
            .Include(e => e.Enquadramento)
            .Include(e => e.RegimeEspecialTributacao)
            .Where(e => e.Ativo)
            .OrderBy(e => e.CriadoEm)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Listar todas as empresas ativas
    /// </summary>
    public async Task<List<Empresa>> ListarEmpresasAtivasAsync()
    {
        return await _context.Empresas
            .Include(e => e.RegimeTributario)
            .Where(e => e.Ativo)
            .OrderBy(e => e.RazaoSocial)
            .ToListAsync();
    }

    /// <summary>
    /// Buscar empresa por ID
    /// </summary>
    public async Task<Empresa?> ObterEmpresaPorIdAsync(Guid id)
    {
        return await _context.Empresas
            .Include(e => e.RegimeTributario)
            .Include(e => e.Enquadramento)
            .Include(e => e.RegimeEspecialTributacao)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Remove formatação do CNPJ (deixa apenas números)
    /// </summary>
    private string LimparCnpj(string cnpj)
    {
        return new string(cnpj.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Remove formatação do CEP (deixa apenas números)
    /// </summary>
    private string LimparCep(string cep)
    {
        return new string(cep.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Mapeia string do regime tributário para ID
    /// </summary>
    private int MapearRegimeTributario(string? regime)
    {
        return regime?.ToLower() switch
        {
            "simples_nacional" => 1,
            "lucro_presumido" => 2,
            "lucro_real" => 3,
            _ => 1 // Default: Simples Nacional
        };
    }
}

#region DTOs

public class SyncEmpresasResponse
{
    public List<EmpresaDto> Empresas { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
}

public class EmpresaDto
{
    public string Id { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Site { get; set; }
    public string? RegimeTributario { get; set; }
    public string? Logo { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SyncEmpresasResult
{
    public bool Sucesso { get; set; }
    public int Total { get; set; }
    public int Novos { get; set; }
    public int Atualizados { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

#endregion
