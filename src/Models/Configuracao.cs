namespace Solis.AgentePDV.Models;

/// <summary>
/// Configuração do terminal/PDV
/// Armazena configurações locais do ponto de venda e vinculação com tenant
/// </summary>
public class Configuracao
{
    public int Id { get; set; }
    
    // Configurações gerais (chave-valor)
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    
    // Vinculação com tenant (NÃO é autenticação de usuário)
    /// <summary>
    /// Token JWT para vincular agente ao tenant (não é token de autenticação de usuário)
    /// Contém: tenant, agentName, type, validade
    /// </summary>
    public string? Token { get; set; }
    
    /// <summary>
    /// ID do tenant (cliente) ao qual este agente está vinculado
    /// Extraído do token JWT durante configuração inicial
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Data de expiração do token JWT de vinculação
    /// </summary>
    public DateTime? TokenValidoAte { get; set; }
    
    /// <summary>
    /// URL base da API Solis (exemplo: http://localhost:3000)
    /// </summary>
    public string? ApiBaseUrl { get; set; }
    
    /// <summary>
    /// Nome do agente/PDV (exemplo: "PDV Loja Centro")
    /// Extraído do token JWT durante configuração inicial
    /// </summary>
    public string? NomeAgente { get; set; }
    
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
