using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Cache local de produtos para operação offline
/// </summary>
public class Produto
{
    [Key]
    public Guid Id { get; set; }
    
    [MaxLength(50)]
    public string? CodigoBarras { get; set; }
    
    [MaxLength(20)]
    public string? CodigoInterno { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Descricao { get; set; }
    
    [MaxLength(8)]
    public string? NCM { get; set; }
    
    [MaxLength(7)]
    public string? CEST { get; set; }
    
    [MaxLength(10)]
    public string UnidadeMedida { get; set; } = "UN";
    
    public bool Ativo { get; set; } = true;
    
    // Relacionamentos
    public ProdutoPreco? PrecoAtual { get; set; }
    
    // Sincronização
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? SincronizadoEm { get; set; }
    
    /// <summary>
    /// Retorna o preço para venda
    /// </summary>
    [NotMapped]
    public decimal PrecoVenda => PrecoAtual?.PrecoVenda ?? 0;
}
