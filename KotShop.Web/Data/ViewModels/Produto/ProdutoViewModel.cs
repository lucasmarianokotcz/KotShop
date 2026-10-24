namespace KotShop.Web.Data.ViewModels.Produto
{
    public record ProdutoViewModel(
        long Id,
        string Nome,
        decimal Preco,
        string Categoria)
    {
        public const int DescontoAVistaEmPorcentagem = 10;
        public const int MaximoDeParcelas = 12;

        public decimal Preco { private get; init; } = Preco;

        public string PrecoAVista => (Preco * (100 - DescontoAVistaEmPorcentagem) / 100).ToString("C2");
        public string PrecoParcela => (Preco / MaximoDeParcelas).ToString("C2");
        public string PrecoTotalAPrazo => Preco.ToString("C2");
    }
}
