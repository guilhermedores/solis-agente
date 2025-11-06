using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Representa uma sessão de caixa (turno de trabalho)
/// </summary>
public class Caixa
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Número do PDV/Terminal
    /// </summary>
    [Required]
    public int NumeroTerminal { get; set; }
    
    /// <summary>
    /// ID do operador que abriu o caixa
    /// </summary>
    public string? OperadorId { get; set; }
    
    /// <summary>
    /// Nome do operador
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string OperadorNome { get; set; } = string.Empty;
    
    /// <summary>
    /// Data/hora de abertura do caixa
    /// </summary>
    [Required]
    public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Valor inicial informado na abertura (fundo de troco)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorAbertura { get; set; }
    
    /// <summary>
    /// Data/hora de fechamento do caixa
    /// </summary>
    public DateTime? DataFechamento { get; set; }
    
    /// <summary>
    /// Valor informado no fechamento
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ValorFechamento { get; set; }
    
    /// <summary>
    /// Total de vendas realizadas (calculado)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalVendas { get; set; }
    
    /// <summary>
    /// Total em dinheiro
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDinheiro { get; set; }
    
    /// <summary>
    /// Total em cartão de débito
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDebito { get; set; }
    
    /// <summary>
    /// Total em cartão de crédito
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCredito { get; set; }
    
    /// <summary>
    /// Total em PIX
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPix { get; set; }
    
    /// <summary>
    /// Total de outras formas de pagamento
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalOutros { get; set; }
    
    /// <summary>
    /// Quantidade de vendas realizadas
    /// </summary>
    public int QuantidadeVendas { get; set; }
    
    /// <summary>
    /// Diferença encontrada no fechamento (pode ser sangria/suprimento)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Diferenca { get; set; }
    
    /// <summary>
    /// Status do caixa
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Aberto"; // Aberto, Fechado
    
    /// <summary>
    /// Observações sobre o caixa
    /// </summary>
    [MaxLength(1000)]
    public string? Observacoes { get; set; }
    
    /// <summary>
    /// Se já foi sincronizado com a nuvem
    /// </summary>
    public bool Sincronizado { get; set; }
    
    /// <summary>
    /// Data de criação do registro
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navegação para vendas
    public virtual ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}

/// <summary>
/// DTO para abertura de caixa
/// </summary>
public class AberturaCaixaDto
{
    [Required(ErrorMessage = "Número do terminal é obrigatório")]
    public int NumeroTerminal { get; set; }
    
    [Required(ErrorMessage = "Nome do operador é obrigatório")]
    [MaxLength(200)]
    public string OperadorNome { get; set; } = string.Empty;
    
    public string? OperadorId { get; set; }
    
    [Required(ErrorMessage = "Valor de abertura é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "Valor de abertura deve ser maior ou igual a zero")]
    public decimal ValorAbertura { get; set; }
    
    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}

/// <summary>
/// DTO para fechamento de caixa
/// </summary>
public class FechamentoCaixaDto
{
    [Required(ErrorMessage = "ID do caixa é obrigatório")]
    public Guid CaixaId { get; set; }
    
    [Required(ErrorMessage = "Valor de fechamento é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "Valor de fechamento deve ser maior ou igual a zero")]
    public decimal ValorFechamento { get; set; }
    
    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}

/// <summary>
/// DTO com resumo do caixa
/// </summary>
public class ResumoCaixaDto
{
    public Guid Id { get; set; }
    public int NumeroTerminal { get; set; }
    public string OperadorNome { get; set; } = string.Empty;
    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
    public decimal ValorAbertura { get; set; }
    public decimal? ValorFechamento { get; set; }
    public decimal TotalVendas { get; set; }
    public decimal TotalDinheiro { get; set; }
    public decimal TotalDebito { get; set; }
    public decimal TotalCredito { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalOutros { get; set; }
    public int QuantidadeVendas { get; set; }
    public decimal? Diferenca { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ValorEsperado { get; set; } // ValorAbertura + TotalVendas
    public string? Observacoes { get; set; }
}
