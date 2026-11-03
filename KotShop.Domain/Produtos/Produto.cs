using KotShop.Domain.Categorias;

namespace KotShop.Domain.Produtos;

public sealed class Produto
{
    public const int TamanhoMaximoNome = 255;
    public const int TamanhoMaximoUrlImagem = 1000;

    private Produto() { }

    public Produto(string nome, decimal preco, string descricao, long categoriaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ArgumentException($"O nome deve ter até {TamanhoMaximoNome} caracteres.", nameof(nome));
        ArgumentOutOfRangeException.ThrowIfNegative(preco);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoriaId);

        Nome = nome.Trim();
        Preco = preco;
        Descricao = descricao.Trim();
        CategoriaId = categoriaId;
    }

    public long Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public decimal Preco { get; private set; }
    public string Descricao { get; private set; } = null!;
    public string? UrlImagem { get; private set; }
    public long CategoriaId { get; private set; }
    public Categoria Categoria { get; private set; } = null!;

    public void DefinirImagem(string? urlImagem)
    {
        if (urlImagem is not null &&
            (urlImagem.Length > TamanhoMaximoUrlImagem ||
             !Uri.TryCreate(urlImagem, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new ArgumentException($"A imagem deve possuir uma URL HTTP ou HTTPS de até {TamanhoMaximoUrlImagem} caracteres.", nameof(urlImagem));

        UrlImagem = urlImagem;
    }
}
