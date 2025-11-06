using Microsoft.AspNetCore.Mvc;
using Solis.AgentePDV.Services;

namespace Solis.AgentePDV.Controllers;

[ApiController]
[Route("api/empresas")]
public class EmpresaController : ControllerBase
{
    private readonly EmpresaService _empresaService;
    private readonly ILogger<EmpresaController> _logger;

    public EmpresaController(
        EmpresaService empresaService,
        ILogger<EmpresaController> logger)
    {
        _empresaService = empresaService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/empresas
    /// Lista todas as empresas ativas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var empresas = await _empresaService.ListarEmpresasAtivasAsync();
            
            return Ok(new
            {
                total = empresas.Count,
                empresas = empresas.Select(e => new
                {
                    id = e.Id,
                    razaoSocial = e.RazaoSocial,
                    nomeFantasia = e.NomeFantasia,
                    cnpj = e.CNPJ,
                    inscricaoEstadual = e.InscricaoEstadual,
                    inscricaoMunicipal = e.InscricaoMunicipal,
                    logradouro = e.Logradouro,
                    numero = e.Numero,
                    complemento = e.Complemento,
                    bairro = e.Bairro,
                    cidade = e.Cidade,
                    uf = e.UF,
                    cep = e.CEP,
                    telefone = e.Telefone,
                    email = e.Email,
                    regimeTributario = e.RegimeTributario?.Descricao,
                    ativo = e.Ativo
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar empresas");
            return StatusCode(500, new { error = "Erro ao listar empresas" });
        }
    }

    /// <summary>
    /// GET /api/empresas/principal
    /// Obtém a empresa principal (para cupom fiscal)
    /// </summary>
    [HttpGet("principal")]
    public async Task<IActionResult> ObterPrincipal()
    {
        try
        {
            var empresa = await _empresaService.ObterEmpresaPrincipalAsync();
            
            if (empresa == null)
            {
                return NotFound(new { error = "Nenhuma empresa configurada" });
            }

            return Ok(new
            {
                id = empresa.Id,
                razaoSocial = empresa.RazaoSocial,
                nomeFantasia = empresa.NomeFantasia,
                cnpj = empresa.CNPJ,
                cnpjFormatado = FormatarCnpj(empresa.CNPJ),
                inscricaoEstadual = empresa.InscricaoEstadual,
                inscricaoMunicipal = empresa.InscricaoMunicipal,
                endereco = new
                {
                    logradouro = empresa.Logradouro,
                    numero = empresa.Numero,
                    complemento = empresa.Complemento,
                    bairro = empresa.Bairro,
                    cidade = empresa.Cidade,
                    uf = empresa.UF,
                    cep = empresa.CEP,
                    cepFormatado = FormatarCep(empresa.CEP),
                    completo = ObterEnderecoCompleto(empresa)
                },
                contato = new
                {
                    telefone = empresa.Telefone,
                    telefoneFormatado = FormatarTelefone(empresa.Telefone),
                    email = empresa.Email
                },
                regimeTributario = empresa.RegimeTributario?.Descricao,
                mensagemCupom = empresa.MensagemCupom,
                ativo = empresa.Ativo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter empresa principal");
            return StatusCode(500, new { error = "Erro ao obter empresa principal" });
        }
    }

    /// <summary>
    /// GET /api/empresas/{id}
    /// Obtém uma empresa específica
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        try
        {
            var empresa = await _empresaService.ObterEmpresaPorIdAsync(id);
            
            if (empresa == null)
            {
                return NotFound(new { error = "Empresa não encontrada" });
            }

            return Ok(new
            {
                id = empresa.Id,
                razaoSocial = empresa.RazaoSocial,
                nomeFantasia = empresa.NomeFantasia,
                cnpj = empresa.CNPJ,
                cnpjFormatado = FormatarCnpj(empresa.CNPJ),
                inscricaoEstadual = empresa.InscricaoEstadual,
                inscricaoMunicipal = empresa.InscricaoMunicipal,
                logradouro = empresa.Logradouro,
                numero = empresa.Numero,
                complemento = empresa.Complemento,
                bairro = empresa.Bairro,
                cidade = empresa.Cidade,
                uf = empresa.UF,
                cep = empresa.CEP,
                telefone = empresa.Telefone,
                email = empresa.Email,
                regimeTributario = empresa.RegimeTributario?.Descricao,
                ativo = empresa.Ativo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter empresa");
            return StatusCode(500, new { error = "Erro ao obter empresa" });
        }
    }

    /// <summary>
    /// POST /api/empresas/sincronizar
    /// Sincroniza empresas da API
    /// </summary>
    [HttpPost("sincronizar")]
    public async Task<IActionResult> Sincronizar()
    {
        try
        {
            var result = await _empresaService.SincronizarEmpresasAsync();
            
            if (result.Sucesso)
            {
                return Ok(new
                {
                    sucesso = true,
                    total = result.Total,
                    novos = result.Novos,
                    atualizados = result.Atualizados,
                    mensagem = result.Mensagem
                });
            }
            else
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = result.Mensagem
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar empresas");
            return StatusCode(500, new
            {
                sucesso = false,
                error = "Erro ao sincronizar empresas"
            });
        }
    }

    #region Métodos Auxiliares

    private string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14)
            return cnpj;

        return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }

    private string FormatarCep(string cep)
    {
        if (string.IsNullOrEmpty(cep) || cep.Length != 8)
            return cep;

        return $"{cep.Substring(0, 5)}-{cep.Substring(5, 3)}";
    }

    private string? FormatarTelefone(string? telefone)
    {
        if (string.IsNullOrEmpty(telefone))
            return telefone;

        var numeros = new string(telefone.Where(char.IsDigit).ToArray());

        if (numeros.Length == 11)
            return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 5)}-{numeros.Substring(7, 4)}";
        else if (numeros.Length == 10)
            return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 4)}-{numeros.Substring(6, 4)}";

        return telefone;
    }

    private string ObterEnderecoCompleto(Solis.AgentePDV.Models.Empresa empresa)
    {
        var endereco = $"{empresa.Logradouro}, {empresa.Numero}";
        
        if (!string.IsNullOrEmpty(empresa.Complemento))
            endereco += $", {empresa.Complemento}";
        
        endereco += $", {empresa.Bairro}, {empresa.Cidade}/{empresa.UF}, CEP {FormatarCep(empresa.CEP)}";
        
        return endereco;
    }

    #endregion
}
