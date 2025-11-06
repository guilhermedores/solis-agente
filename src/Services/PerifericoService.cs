namespace Solis.AgentePDV.Services;

public class PerifericoService : IPerifericoService
{
    private readonly IImpressoraService _impressoraService;
    private readonly IGavetaService _gavetaService;
    private readonly IConfiguration _configuration;

    public PerifericoService(
        IImpressoraService impressoraService,
        IGavetaService gavetaService,
        IConfiguration configuration)
    {
        _impressoraService = impressoraService;
        _gavetaService = gavetaService;
        _configuration = configuration;
    }

    public object ObterStatusPerifericos()
    {
        var impressoraConfig = _configuration.GetSection("Perifericos:Impressora");
        var gavetaConfig = _configuration.GetSection("Perifericos:Gaveta");
        var tefConfig = _configuration.GetSection("Perifericos:TEF");
        var satConfig = _configuration.GetSection("Perifericos:SAT");

        return new
        {
            impressora = new
            {
                habilitada = impressoraConfig.GetValue<bool>("Enabled"),
                tipo = impressoraConfig.GetValue<string>("Tipo"),
                porta = impressoraConfig.GetValue<string>("Porta"),
                status = "ok" // TODO: Verificar status real
            },
            gaveta = new
            {
                habilitada = gavetaConfig.GetValue<bool>("Enabled"),
                porta = gavetaConfig.GetValue<string>("Porta"),
                status = "ok"
            },
            tef = new
            {
                habilitada = tefConfig.GetValue<bool>("Enabled"),
                tipo = tefConfig.GetValue<string>("Tipo"),
                status = "desabilitado"
            },
            sat = new
            {
                habilitada = satConfig.GetValue<bool>("Enabled"),
                tipo = satConfig.GetValue<string>("Tipo"),
                status = "desabilitado"
            },
            timestamp = DateTime.UtcNow
        };
    }
}