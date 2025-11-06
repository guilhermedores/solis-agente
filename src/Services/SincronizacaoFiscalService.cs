using Solis.AgentePDV.Data;
using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;

namespace Solis.AgentePDV.Services;

/// <summary>
/// Serviço para sincronização de dados fiscais (Empresa, Regimes, Enquadramento)
/// </summary>
public class SincronizacaoFiscalService
{
    private readonly LocalDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SincronizacaoFiscalService> _logger;

    public SincronizacaoFiscalService(
        LocalDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<SincronizacaoFiscalService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SincronizarDadosFiscaisAsync()
    {
        var client = _httpClientFactory.CreateClient("SolisApi");

        try
        {
            _logger.LogInformation("Iniciando sincronizacao de dados fiscais...");

            var response = await client.GetAsync("/api/fiscal/sincronizacao");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API retornou status {StatusCode} ao buscar dados fiscais", response.StatusCode);
                return;
            }

            var dadosFiscais = await response.Content.ReadFromJsonAsync<DadosFiscaisSincronizacao>();

            if (dadosFiscais == null)
            {
                _logger.LogInformation("Nenhum dado fiscal retornado da API");
                return;
            }

            // Sincronizar Regimes Tributários
            if (dadosFiscais.RegimesTributarios != null)
            {
                foreach (var regime in dadosFiscais.RegimesTributarios)
                {
                    var local = await _context.RegimesTributarios
                        .FirstOrDefaultAsync(r => r.Id == regime.Id);

                    if (local == null)
                        _context.RegimesTributarios.Add(regime);
                    else
                    {
                        local.Codigo = regime.Codigo;
                        local.Nome = regime.Nome;
                        local.Descricao = regime.Descricao;
                        local.AliquotaPadrao = regime.AliquotaPadrao;
                        local.Ativo = regime.Ativo;
                    }
                }
            }

            // Sincronizar Enquadramentos
            if (dadosFiscais.Enquadramentos != null)
            {
                foreach (var enquadramento in dadosFiscais.Enquadramentos)
                {
                    var local = await _context.Enquadramentos
                        .FirstOrDefaultAsync(e => e.Id == enquadramento.Id);

                    if (local == null)
                        _context.Enquadramentos.Add(enquadramento);
                    else
                    {
                        local.Codigo = enquadramento.Codigo;
                        local.Nome = enquadramento.Nome;
                        local.Descricao = enquadramento.Descricao;
                        local.Ativo = enquadramento.Ativo;
                    }
                }
            }

            // Sincronizar Regimes Especiais de Tributação
            if (dadosFiscais.RegimesEspeciaisTributacao != null)
            {
                foreach (var regimeEspecial in dadosFiscais.RegimesEspeciaisTributacao)
                {
                    var local = await _context.RegimesEspeciaisTributacao
                        .FirstOrDefaultAsync(r => r.Id == regimeEspecial.Id);

                    if (local == null)
                        _context.RegimesEspeciaisTributacao.Add(regimeEspecial);
                    else
                    {
                        local.Codigo = regimeEspecial.Codigo;
                        local.Nome = regimeEspecial.Nome;
                        local.Descricao = regimeEspecial.Descricao;
                        local.Ativo = regimeEspecial.Ativo;
                    }
                }
            }

            // Sincronizar Empresa
            if (dadosFiscais.Empresa != null)
            {
                var empresaLocal = await _context.Empresas
                    .FirstOrDefaultAsync(e => e.Id == dadosFiscais.Empresa.Id);

                if (empresaLocal == null)
                    _context.Empresas.Add(dadosFiscais.Empresa);
                else
                {
                    empresaLocal.RazaoSocial = dadosFiscais.Empresa.RazaoSocial;
                    empresaLocal.NomeFantasia = dadosFiscais.Empresa.NomeFantasia;
                    empresaLocal.CNPJ = dadosFiscais.Empresa.CNPJ;
                    empresaLocal.InscricaoEstadual = dadosFiscais.Empresa.InscricaoEstadual;
                    empresaLocal.InscricaoMunicipal = dadosFiscais.Empresa.InscricaoMunicipal;
                    empresaLocal.Logradouro = dadosFiscais.Empresa.Logradouro;
                    empresaLocal.Numero = dadosFiscais.Empresa.Numero;
                    empresaLocal.Complemento = dadosFiscais.Empresa.Complemento;
                    empresaLocal.Bairro = dadosFiscais.Empresa.Bairro;
                    empresaLocal.CEP = dadosFiscais.Empresa.CEP;
                    empresaLocal.Cidade = dadosFiscais.Empresa.Cidade;
                    empresaLocal.UF = dadosFiscais.Empresa.UF;
                    empresaLocal.Telefone = dadosFiscais.Empresa.Telefone;
                    empresaLocal.Email = dadosFiscais.Empresa.Email;
                    empresaLocal.RegimeTributarioId = dadosFiscais.Empresa.RegimeTributarioId;
                    empresaLocal.EnquadramentoId = dadosFiscais.Empresa.EnquadramentoId;
                    empresaLocal.RegimeEspecialTributacaoId = dadosFiscais.Empresa.RegimeEspecialTributacaoId;
                    empresaLocal.CNAE = dadosFiscais.Empresa.CNAE;
                    empresaLocal.MensagemCupom = dadosFiscais.Empresa.MensagemCupom;
                    empresaLocal.Ativo = dadosFiscais.Empresa.Ativo;
                    empresaLocal.AtualizadoEm = DateTime.UtcNow;
                    empresaLocal.SincronizadoEm = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sincronizacao fiscal concluida: {Regimes} regimes, {Enquadramentos} enquadramentos, {RegimesEspeciais} regimes especiais",
                dadosFiscais.RegimesTributarios?.Count ?? 0,
                dadosFiscais.Enquadramentos?.Count ?? 0,
                dadosFiscais.RegimesEspeciaisTributacao?.Count ?? 0);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API nao disponivel. Sincronizacao fiscal sera tentada novamente no proximo ciclo");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout ao conectar com a API. Sincronizacao fiscal sera tentada novamente no proximo ciclo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar dados fiscais");
            throw;
        }
    }
}

/// <summary>
/// DTO para sincronização de dados fiscais
/// </summary>
public class DadosFiscaisSincronizacao
{
    public List<RegimeTributario>? RegimesTributarios { get; set; }
    public List<Enquadramento>? Enquadramentos { get; set; }
    public List<RegimeEspecialTributacao>? RegimesEspeciaisTributacao { get; set; }
    public Empresa? Empresa { get; set; }
}
