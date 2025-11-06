using Solis.AgentePDV.Models;

namespace Solis.AgentePDV.Services;

public interface IVendaService
{
    Task<Venda> CriarVendaAsync(Venda venda);
    Task<Venda?> ObterVendaPorIdAsync(Guid id);
    Task<IEnumerable<Venda>> ListarVendasPendentesAsync();
    Task FinalizarVendaAsync(Guid vendaId, List<VendaPagamento> pagamentos);
    Task CancelarVendaAsync(Guid vendaId, string motivo);
    Task<bool> SincronizarVendasAsync();
}

public interface IProdutoService
{
    Task<Produto?> BuscarPorCodigoBarrasAsync(string codigoBarras);
    Task<Produto?> BuscarPorIdAsync(Guid id);
    Task<IEnumerable<Produto>> BuscarPorNomeAsync(string termo);
    Task<IEnumerable<Produto>> ListarProdutosAsync(int skip, int take);
    Task SincronizarProdutosAsync();
    Task<bool> VerificarDisponibilidadeAsync(Guid produtoId, decimal quantidade);
}

public interface IPrecoService
{
    Task<ProdutoPreco?> ObterPrecoAtualAsync(Guid produtoId);
    Task AtualizarPrecoAsync(ProdutoPreco preco);
    Task SincronizarPrecosAsync();
}

public interface IPerifericoService
{
    object ObterStatusPerifericos();
}

public interface IImpressoraService
{
    Task ImprimirCupomAsync(object cupomData);
    Task ImprimirTextoAsync(string texto);
    Task<bool> TestarConexaoAsync();
}

public interface IGavetaService
{
    Task AbrirGavetaAsync();
}