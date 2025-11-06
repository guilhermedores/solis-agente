using System.ComponentModel.DataAnnotations;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Dados da empresa para emissão de cupom fiscal
/// </summary>
public class Empresa
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Razão Social
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string RazaoSocial { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome Fantasia
    /// </summary>
    [MaxLength(200)]
    public string? NomeFantasia { get; set; }
    
    /// <summary>
    /// CNPJ (somente números)
    /// </summary>
    [Required]
    [MaxLength(14)]
    public string CNPJ { get; set; } = string.Empty;
    
    /// <summary>
    /// Inscrição Estadual
    /// </summary>
    [MaxLength(20)]
    public string? InscricaoEstadual { get; set; }
    
    /// <summary>
    /// Inscrição Municipal
    /// </summary>
    [MaxLength(20)]
    public string? InscricaoMunicipal { get; set; }
    
    // Endereço
    [Required]
    [MaxLength(200)]
    public string Logradouro { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string Numero { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Complemento { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Bairro { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(8)]
    public string CEP { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Cidade { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2)]
    public string UF { get; set; } = string.Empty;
    
    // Contato
    [MaxLength(20)]
    public string? Telefone { get; set; }
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    // Regime Tributário
    public int RegimeTributarioId { get; set; } = 1;
    public RegimeTributario? RegimeTributario { get; set; }
    
    // Enquadramento
    public int? EnquadramentoId { get; set; }
    public Enquadramento? Enquadramento { get; set; }
    
    // Regime Especial de Tributação
    public int? RegimeEspecialTributacaoId { get; set; }
    public RegimeEspecialTributacao? RegimeEspecialTributacao { get; set; }
    
    /// <summary>
    /// CNAE principal
    /// </summary>
    [MaxLength(10)]
    public string? CNAE { get; set; }
    
    /// <summary>
    /// Mensagem adicional no cupom
    /// </summary>
    [MaxLength(500)]
    public string? MensagemCupom { get; set; }
    
    public bool Ativo { get; set; } = true;
    
    // Sincronização
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? SincronizadoEm { get; set; }
}
