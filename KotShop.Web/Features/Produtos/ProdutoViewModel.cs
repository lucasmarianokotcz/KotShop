using KotShop.Domain.Produtos;

namespace KotShop.Web.Features.Produtos
{
    public record ProdutoViewModel(
        long Id,
        string Nome,
        decimal Preco,
        string Categoria,
        string? UrlImagem = null)
    {
        public const int DescontoAVistaEmPorcentagem = PoliticaPagamento.DescontoAVistaEmPorcentagem;
        public const int MaximoDeParcelas = PoliticaPagamento.MaximoDeParcelas;

        public decimal Preco { private get; init; } = Preco;

        public string PrecoAVista => PoliticaPagamento.CalcularPrecoAVista(Preco).ToString("C2");
        public string PrecoParcela => PoliticaPagamento.CalcularValorParcela(Preco).ToString("C2");
        public string PrecoTotalAPrazo => Preco.ToString("C2");
        public string? UrlImagem { get; set; } = UrlImagem;
    }
}
