using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solis.AgentePDV.Models;

/// <summary>
/// Mensagem do Outbox Pattern para garantir sincronização confiável com a nuvem
/// </summary>
public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Tipo da entidade: "Venda", "Produto", "VendaItem", etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TipoEntidade { get; set; } = string.Empty;
    
    /// <summary>
    /// Operação realizada: "Create", "Update", "Delete"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Operacao { get; set; } = string.Empty;
    
    /// <summary>
    /// ID da entidade relacionada
    /// </summary>
    public Guid EntidadeId { get; set; }
    
    /// <summary>
    /// Payload JSON da entidade
    /// </summary>
    [Required]
    [Column(TypeName = "TEXT")]
    public string PayloadJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Endpoint da API que deve receber a mensagem
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string EndpointApi { get; set; } = string.Empty;
    
    /// <summary>
    /// Método HTTP: GET, POST, PUT, DELETE
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string MetodoHttp { get; set; } = "POST";
    
    /// <summary>
    /// Status: "Pendente", "Processando", "Enviado", "Erro"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pendente";
    
    /// <summary>
    /// Número de tentativas de envio
    /// </summary>
    public int TentativasEnvio { get; set; } = 0;
    
    /// <summary>
    /// Máximo de tentativas antes de marcar como falha permanente
    /// </summary>
    public int MaxTentativas { get; set; } = 5;
    
    /// <summary>
    /// Última mensagem de erro (se houver)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? UltimoErro { get; set; }
    
    /// <summary>
    /// Código de status HTTP da última resposta
    /// </summary>
    public int? UltimoStatusCode { get; set; }
    
    /// <summary>
    /// Data/hora de criação da mensagem
    /// </summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data/hora da última tentativa de envio
    /// </summary>
    public DateTime? UltimaTentativaEm { get; set; }
    
    /// <summary>
    /// Data/hora em que foi enviado com sucesso
    /// </summary>
    public DateTime? EnviadoEm { get; set; }
    
    /// <summary>
    /// Próxima tentativa (para implementar backoff exponencial)
    /// </summary>
    public DateTime? ProximaTentativaEm { get; set; }
    
    /// <summary>
    /// Prioridade da mensagem (0 = normal, maior = mais prioritário)
    /// </summary>
    public int Prioridade { get; set; } = 0;
}
