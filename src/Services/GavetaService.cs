using System.IO.Ports;

namespace Solis.AgentePDV.Services;

public class GavetaService : IGavetaService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GavetaService> _logger;

    public GavetaService(IConfiguration configuration, ILogger<GavetaService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task AbrirGavetaAsync()
    {
        try
        {
            var config = _configuration.GetSection("Perifericos:Gaveta");
            var habilitada = config.GetValue<bool>("Enabled");

            if (!habilitada)
            {
                _logger.LogWarning("Gaveta desabilitada na configuração");
                return;
            }

            var porta = config.GetValue<string>("Porta") ?? "COM1";
            var comandoHex = config.GetValue<string>("ComandoAbertura") ?? "1B700019FA";

            // Converter comando hexadecimal para bytes
            var comando = ConvertHexStringToByteArray(comandoHex);

            using var serialPort = new SerialPort(porta, 9600, Parity.None, 8, StopBits.One);
            serialPort.Open();

            await serialPort.BaseStream.WriteAsync(comando, 0, comando.Length);

            serialPort.Close();

            _logger.LogInformation("Gaveta aberta com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao abrir gaveta");
            throw;
        }
    }

    private byte[] ConvertHexStringToByteArray(string hex)
    {
        int numberChars = hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        
        for (int i = 0; i < numberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        
        return bytes;
    }
}