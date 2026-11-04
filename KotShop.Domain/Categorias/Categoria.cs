using KotShop.Domain.Produtos;

namespace KotShop.Domain.Categorias;

public sealed class Categoria
{
    public const int TamanhoMaximoNome = 255;

    private Categoria() { }

    public Categoria(string nome)
        => Renomear(nome);

    public void Renomear(string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ArgumentException($"O nome deve ter até {TamanhoMaximoNome} caracteres.", nameof(nome));
        Nome = nome.Trim();
    }

    public long Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public ICollection<Produto> Produtos { get; private set; } = [];
}
