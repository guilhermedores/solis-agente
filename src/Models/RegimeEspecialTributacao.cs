using System.ComponentModel.DataAnnotations;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Regime especial de tributação
/// </summary>
public class RegimeEspecialTributacao
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Código do regime especial
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Codigo { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome do regime especial
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição detalhada
    /// </summary>
    [MaxLength(500)]
    public string? Descricao { get; set; }
    
    public bool Ativo { get; set; } = true;
}
