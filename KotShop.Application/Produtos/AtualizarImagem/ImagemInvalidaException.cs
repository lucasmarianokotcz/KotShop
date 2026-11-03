namespace KotShop.Application.Produtos.AtualizarImagem;

public sealed class ImagemInvalidaException(string mensagem) : Exception(mensagem);
