using KotShop.Domain.Produtos;
using Xunit;

namespace KotShop.Domain.Tests.Produtos;

public class PoliticaPagamentoTests
{
    [Fact]
    public void DescontoPixPreservaCentavosDoPrecoDoCatalogo() =>
        Assert.Equal(1799.91m, PoliticaPagamento.CalcularPrecoAVista(1999.90m));

    [Fact]
    public void ParcelamentoNaoAplicaDescontoExclusivoDoPix()
    {
        Assert.Equal(100m, PoliticaPagamento.CalcularValorParcela(1200m));
        Assert.Equal(1080m, PoliticaPagamento.CalcularPrecoAVista(1200m));
    }

    [Fact]
    public void CalculoNaoArredondaPrecoAntesDaApresentacao() =>
        Assert.Equal(0.045m, PoliticaPagamento.CalcularPrecoAVista(0.05m));

    [Fact]
    public void NaoAnunciaValoresNegativos()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PoliticaPagamento.CalcularPrecoAVista(-1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoliticaPagamento.CalcularValorParcela(-1m));
    }
}
