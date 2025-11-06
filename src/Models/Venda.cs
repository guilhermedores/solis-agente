using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Representa uma venda/cupom fiscal armazenada localmente
/// </summary>
public class Venda
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public long NumeroCupom { get; set; }
    
    public Guid? EstabelecimentoId { get; set; }
    
    public Guid? PdvId { get; set; }
    
    public Guid? UsuarioId { get; set; }
    
    /// <summary>
    /// ID do caixa (sessão) onde a venda foi realizada
    /// </summary>
    public Guid? CaixaId { get; set; }
    
    // Cliente
    public string? ClienteCpf { get; set; }
    public string? ClienteNome { get; set; }
    public string? ClienteEmail { get; set; }
    
    // Valores
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorBruto { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorDesconto { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorLiquido { get; set; }
    
    /// <summary>
    /// ID do status da venda (relacionamento com a tabela StatusVenda)
    /// </summary>
    [Required]
    public Guid StatusVendaId { get; set; }
    
    public string? Observacoes { get; set; }
    
    // Sincronização
    public bool Sincronizado { get; set; } = false;
    public DateTime? SincronizadoEm { get; set; }
    public int TentativasSync { get; set; } = 0;
    public string? ErroSync { get; set; }
    
    // Auditoria
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Relacionamentos
    public virtual ICollection<VendaItem> Itens { get; set; } = new List<VendaItem>();
    public virtual ICollection<VendaPagamento> Pagamentos { get; set; } = new List<VendaPagamento>();
    public virtual Caixa? Caixa { get; set; }
    
    [ForeignKey(nameof(StatusVendaId))]
    public virtual StatusVenda? StatusVenda { get; set; }
}

/// <summary>
/// Item de uma venda
/// </summary>
public class VendaItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid VendaId { get; set; }
    
    public Guid? ProdutoId { get; set; }
    
    public int Sequencia { get; set; }
    
    public string CodigoProduto { get; set; } = string.Empty;
    
    public string NomeProduto { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(10,3)")]
    public decimal Quantidade { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecoUnitario { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal DescontoItem { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Relacionamento
    [ForeignKey(nameof(VendaId))]
    public virtual Venda? Venda { get; set; }
}

/// <summary>
/// Pagamento de uma venda
/// </summary>
public class VendaPagamento
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid VendaId { get; set; }
    
    /// <summary>
    /// ID da forma de pagamento (relacionamento com a tabela FormasPagamento)
    /// </summary>
    [Required]
    public Guid FormaPagamentoId { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTroco { get; set; }
    
    /// <summary>
    /// Número de parcelas (para crédito)
    /// </summary>
    public int? Parcelas { get; set; }
    
    // Dados TEF
    public string? NSU { get; set; }
    public string? Autorizacao { get; set; }
    public string? Bandeira { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Relacionamentos
    [ForeignKey(nameof(VendaId))]
    public virtual Venda? Venda { get; set; }
    
    [ForeignKey(nameof(FormaPagamentoId))]
    public virtual FormaPagamento? FormaPagamento { get; set; }
}