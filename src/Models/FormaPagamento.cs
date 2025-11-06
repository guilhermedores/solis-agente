using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Representa uma forma de pagamento disponível no PDV
/// </summary>
public class FormaPagamento
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Descrição da forma de pagamento (ex: "Visa Crédito", "Mastercard Débito", "Dinheiro")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Descricao { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo da forma de pagamento
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Tipo { get; set; } = string.Empty; // CREDITO, DEBITO, DINHEIRO, PAGAMENTO_INSTANTANEO, VALE_ALIMENTACAO
    
    /// <summary>
    /// Identificador externo (da API/ERP)
    /// </summary>
    public Guid? ExternalId { get; set; }
    
    /// <summary>
    /// Código da forma de pagamento (para integração)
    /// </summary>
    [MaxLength(20)]
    public string? Codigo { get; set; }
    
    /// <summary>
    /// Se está ativa
    /// </summary>
    public bool Ativa { get; set; } = true;
    
    /// <summary>
    /// Permite troco (geralmente só dinheiro)
    /// </summary>
    public bool PermiteTroco { get; set; } = false;
    
    /// <summary>
    /// Número de parcelas (para crédito)
    /// </summary>
    public int? MaximoParcelas { get; set; }
    
    /// <summary>
    /// Taxa de juros (%) para parcelamento
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? TaxaJuros { get; set; }
    
    /// <summary>
    /// Requer integração com TEF
    /// </summary>
    public bool RequerTEF { get; set; } = false;
    
    /// <summary>
    /// Bandeira do cartão (se aplicável)
    /// </summary>
    [MaxLength(50)]
    public string? Bandeira { get; set; }
    
    /// <summary>
    /// Ordem de exibição
    /// </summary>
    public int Ordem { get; set; } = 0;
    
    /// <summary>
    /// Se foi sincronizado com a nuvem
    /// </summary>
    public bool Sincronizado { get; set; }
    
    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data de atualização
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navegação
    public virtual ICollection<VendaPagamento> Pagamentos { get; set; } = new List<VendaPagamento>();
}

/// <summary>
/// Tipos de forma de pagamento
/// </summary>
public static class TipoFormaPagamento
{
    public const string CREDITO = "CREDITO";
    public const string DEBITO = "DEBITO";
    public const string DINHEIRO = "DINHEIRO";
    public const string PAGAMENTO_INSTANTANEO = "PAGAMENTO_INSTANTANEO"; // PIX, QR Code, etc
    public const string VALE_ALIMENTACAO = "VALE_ALIMENTACAO";
    
    public static List<string> ObterTodos()
    {
        return new List<string>
        {
            CREDITO,
            DEBITO,
            DINHEIRO,
            PAGAMENTO_INSTANTANEO,
            VALE_ALIMENTACAO
        };
    }
    
    public static bool EhValido(string tipo)
    {
        return ObterTodos().Contains(tipo.ToUpper());
    }
}

/// <summary>
/// DTO para criar/atualizar forma de pagamento
/// </summary>
public class FormaPagamentoDto
{
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [MaxLength(100)]
    public string Descricao { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Tipo é obrigatório")]
    public string Tipo { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Codigo { get; set; }
    
    public bool Ativa { get; set; } = true;
    
    public bool PermiteTroco { get; set; } = false;
    
    [Range(1, 24, ErrorMessage = "Máximo de parcelas deve ser entre 1 e 24")]
    public int? MaximoParcelas { get; set; }
    
    [Range(0, 100, ErrorMessage = "Taxa de juros deve ser entre 0 e 100")]
    public decimal? TaxaJuros { get; set; }
    
    public bool RequerTEF { get; set; } = false;
    
    [MaxLength(50)]
    public string? Bandeira { get; set; }
    
    public int Ordem { get; set; } = 0;
}
