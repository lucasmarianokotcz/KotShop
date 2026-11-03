namespace KotShop.Application.Produtos;

public sealed class ProdutoNaoEncontradoException(long id) : Exception($"Produto {id} não encontrado.");
