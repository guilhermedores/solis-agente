using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Representa um status de venda gerenciado via integração com a nuvem
/// Exemplos: ABERTA, FINALIZADA, CANCELADA, EM_PROCESSAMENTO, etc.
/// </summary>
public class StatusVenda
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Código único do status (ex: ABERTA, FINALIZADA, CANCELADA)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição do status
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Descricao { get; set; } = string.Empty;
    
    /// <summary>
    /// Cor para exibição na UI (hexadecimal)
    /// </summary>
    [MaxLength(7)]
    public string? Cor { get; set; }
    
    /// <summary>
    /// Indica se o status está ativo
    /// </summary>
    public bool Ativa { get; set; } = true;
    
    /// <summary>
    /// Ordem de exibição
    /// </summary>
    public int Ordem { get; set; }
    
    /// <summary>
    /// Indica se vendas neste status podem ser editadas
    /// </summary>
    public bool PermiteEdicao { get; set; } = false;
    
    /// <summary>
    /// Indica se vendas neste status podem ser canceladas
    /// </summary>
    public bool PermiteCancelamento { get; set; } = false;
    
    /// <summary>
    /// Indica se este é um status final (não permite mais alterações)
    /// </summary>
    public bool StatusFinal { get; set; } = false;
    
    // Sincronização
    public bool Sincronizado { get; set; } = false;
    public DateTime? SincronizadoEm { get; set; }
    
    // Auditoria
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Relacionamentos
    public virtual ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}
