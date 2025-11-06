using System.ComponentModel.DataAnnotations;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Regime tributário da empresa
/// </summary>
public class RegimeTributario
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Código do regime (1, 2, 3, etc.)
    /// </summary>
    [Required]
    public int Codigo { get; set; }
    
    /// <summary>
    /// Nome do regime (Simples Nacional, Normal, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição detalhada
    /// </summary>
    [MaxLength(500)]
    public string? Descricao { get; set; }
    
    /// <summary>
    /// Alíquota padrão (se aplicável)
    /// </summary>
    public decimal? AliquotaPadrao { get; set; }
    
    public bool Ativo { get; set; } = true;
}
