using System.IO.Ports;
using System.Text;

namespace Solis.AgentePDV.Services;

public class ImpressoraService : IImpressoraService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImpressoraService> _logger;
    private SerialPort? _serialPort;

    public ImpressoraService(IConfiguration configuration, ILogger<ImpressoraService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        InicializarPorta();
    }

    private void InicializarPorta()
    {
        try
        {
            var config = _configuration.GetSection("Perifericos:Impressora");
            var habilitada = config.GetValue<bool>("Enabled");

            if (!habilitada)
            {
                _logger.LogInformation("Impressora desabilitada na configuração");
                return;
            }

            var porta = config.GetValue<string>("Porta") ?? "COM1";
            var baudRate = config.GetValue<int>("BaudRate", 9600);

            _serialPort = new SerialPort(porta, baudRate, Parity.None, 8, StopBits.One);
            
            _logger.LogInformation("Impressora configurada: Porta={Porta}, BaudRate={BaudRate}", porta, baudRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar porta serial da impressora");
        }
    }

    public async Task ImprimirCupomAsync(object cupomData)
    {
        try
        {
            var cupomTexto = GerarCupomTexto(cupomData);
            await ImprimirTextoAsync(cupomTexto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir cupom");
            throw;
        }
    }

    public async Task ImprimirTextoAsync(string texto)
    {
        if (_serialPort == null)
        {
            _logger.LogWarning("Tentativa de impressão com impressora desabilitada ou não configurada");
            // Simular impressão para desenvolvimento
            _logger.LogInformation("SIMULAÇÃO DE IMPRESSÃO:\n{Texto}", texto);
            await Task.CompletedTask;
            return;
        }

        try
        {
            if (!_serialPort.IsOpen)
                _serialPort.Open();

            var bytes = Encoding.UTF8.GetBytes(texto);
            await _serialPort.BaseStream.WriteAsync(bytes, 0, bytes.Length);

            // Comandos ESC/POS para cortar papel
            var cortarPapel = new byte[] { 0x1D, 0x56, 0x00 }; // GS V 0
            await _serialPort.BaseStream.WriteAsync(cortarPapel, 0, cortarPapel.Length);

            _logger.LogInformation("Texto impresso com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar dados para impressora");
            throw;
        }
        finally
        {
            if (_serialPort?.IsOpen == true)
                _serialPort.Close();
        }
    }

    public async Task<bool> TestarConexaoAsync()
    {
        if (_serialPort == null)
            return false;

        try
        {
            if (!_serialPort.IsOpen)
                _serialPort.Open();

            // Enviar comando de teste ESC/POS
            var testeCmd = new byte[] { 0x1B, 0x40 }; // ESC @ (inicializar impressora)
            await _serialPort.BaseStream.WriteAsync(testeCmd, 0, testeCmd.Length);

            _serialPort.Close();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar conexão com impressora");
            return false;
        }
    }

    private string GerarCupomTexto(object cupomData)
    {
        // TODO: Implementar formatação completa do cupom
        var sb = new StringBuilder();
        
        sb.AppendLine("========================================");
        sb.AppendLine("        SISTEMA SOLIS PDV");
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("----------------------------------------");
        sb.AppendLine("ITEM  DESCRICAO       QTD  VL.UNIT  TOTAL");
        sb.AppendLine("----------------------------------------");
        
        // Aqui você adicionaria os itens da venda
        
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"TOTAL: R$ 0,00");
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine();

        return sb.ToString();
    }
}