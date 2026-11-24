using KotShop.Application.Categorias;
using Xunit;

namespace KotShop.Application.Tests.Categorias;

public sealed class CategoriaServiceTests
{
    [Fact]
    public async Task ListarRetornaResumosEEncaminhaCancelamento()
    {
        using var source = new CancellationTokenSource();
        IReadOnlyList<CategoriaResumo> expected = [new(5, "Livros"), new(9, "Música")];
        var repository = new Repository(expected);
        var service = new CategoriaService(repository);

        var result = await service.ListarAsync(source.Token);

        Assert.Equal(expected, result);
        Assert.Equal(source.Token, repository.Token);
    }

    [Fact]
    public async Task ListarPropagaFalhaDoRepositorio()
    {
        var failure = new InvalidOperationException("Falha na consulta");
        var service = new CategoriaService(new Repository([], failure));

        var result = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListarAsync());

        Assert.Same(failure, result);
    }

    private sealed class Repository(IReadOnlyList<CategoriaResumo> result, Exception? failure = null) : ICategoriaRepository
    {
        public CancellationToken Token { get; private set; }

        public Task<IReadOnlyList<CategoriaResumo>> ListarAsync(CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            return failure is null ? Task.FromResult(result) : Task.FromException<IReadOnlyList<CategoriaResumo>>(failure);
        }
    }
}
