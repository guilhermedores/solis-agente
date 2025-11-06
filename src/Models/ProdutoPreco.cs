using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Preço de venda do produto (sincronizado da nuvem)
/// Mantém histórico de preços para controle
/// </summary>
public class ProdutoPreco
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// ID do produto
    /// </summary>
    [Required]
    public Guid ProdutoId { get; set; }
    
    /// <summary>
    /// Navegação para o produto
    /// </summary>
    public Produto? Produto { get; set; }
    
    /// <summary>
    /// Preço de venda
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoVenda { get; set; }
    
    /// <summary>
    /// Preço está ativo
    /// </summary>
    public bool Ativo { get; set; } = true;
    
    /// <summary>
    /// Data de criação do registro
    /// </summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data da última sincronização com a nuvem
    /// </summary>
    public DateTime? SincronizadoEm { get; set; }
}
