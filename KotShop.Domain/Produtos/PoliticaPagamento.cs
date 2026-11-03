namespace KotShop.Domain.Produtos;

public static class PoliticaPagamento
{
    public const int DescontoAVistaEmPorcentagem = 10;
    public const int MaximoDeParcelas = 12;

    public static decimal CalcularPrecoAVista(decimal preco)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(preco);
        return preco * (100 - DescontoAVistaEmPorcentagem) / 100;
    }

    public static decimal CalcularValorParcela(decimal preco)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(preco);
        return preco / MaximoDeParcelas;
    }
}
