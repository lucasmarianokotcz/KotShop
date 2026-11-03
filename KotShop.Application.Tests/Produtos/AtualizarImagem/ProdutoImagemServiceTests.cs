using KotShop.Application.Abstractions;
using KotShop.Application.Produtos;
using KotShop.Application.Produtos.AtualizarImagem;
using KotShop.Application.Produtos.ConsultarCatalogo;
using KotShop.Domain.Produtos;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KotShop.Application.Tests.Produtos.AtualizarImagem;

public class ProdutoImagemServiceTests
{
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0];

    public static TheoryData<byte[], string, string> FormatosAceitos => new()
    {
        { Png, ".png", "image/png" },
        { [255, 216, 255, 224], ".jpg", "image/jpeg" },
        { [82, 73, 70, 70, 0, 0, 0, 0, 87, 69, 66, 80], ".webp", "image/webp" }
    };

    [Theory]
    [MemberData(nameof(FormatosAceitos))]
    public async Task SalvaUrlSomenteAposEnviarConteudoCompleto(byte[] bytes, string extensao, string contentType)
    {
        var fixture = new Fixture();
        using var stream = new MemoryStream(bytes);

        var result = await fixture.Service.AtualizarAsync(42, stream);

        Assert.Equal(42, result.Id);
        Assert.Equal(fixture.Storage.Url, result.UrlImagem);
        Assert.Equal(result.UrlImagem, fixture.Repository.UrlPersistida);
        Assert.Equal(bytes, fixture.Storage.ConteudoEnviado);
        Assert.StartsWith("produtos/42/", fixture.Storage.KeyEnviada);
        Assert.EndsWith(extensao, fixture.Storage.KeyEnviada);
        Assert.Equal(contentType, fixture.Storage.ContentType);
        Assert.True(fixture.Repository.ArquivoJaEnviadoAoSalvar);
        Assert.Empty(fixture.Storage.KeysRemovidas);
        Assert.True(stream.CanRead); // Caller owns the input stream.
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1, 2, 3 })]
    [InlineData(new byte[] { 82, 73, 70, 70, 0, 0, 0, 0, 78, 79, 80, 69 })]
    public async Task RejeitaArquivoInvalidoAntesDeAcessarBancoOuStorage(byte[] bytes)
    {
        var fixture = new Fixture();
        await Assert.ThrowsAsync<ImagemInvalidaException>(() => fixture.Service.AtualizarAsync(42, new MemoryStream(bytes)));
        Assert.Equal(0, fixture.Repository.Leituras);
        Assert.Null(fixture.Storage.KeyEnviada);
        Assert.Equal(0, fixture.Repository.Gravacoes);
    }

    [Fact]
    public async Task LimitaLeituraRealMesmoQuandoStreamNaoInformaTamanho()
    {
        var fixture = new Fixture();
        using var stream = new StreamSemSeek(new byte[ProdutoImagemService.TamanhoMaximoImagem + 65536]);

        await Assert.ThrowsAsync<ImagemInvalidaException>(() => fixture.Service.AtualizarAsync(42, stream));

        Assert.Equal(ProdutoImagemService.TamanhoMaximoImagem + 1, stream.BytesLidos);
        Assert.Null(fixture.Storage.KeyEnviada);
        Assert.Equal(0, fixture.Repository.Leituras);
    }

    [Fact]
    public async Task AceitaImagemNoLimiteExato()
    {
        var fixture = new Fixture();
        var bytes = new byte[ProdutoImagemService.TamanhoMaximoImagem];
        Png.CopyTo(bytes, 0);
        using var stream = new StreamSemSeek(bytes);

        await fixture.Service.AtualizarAsync(42, stream);

        Assert.Equal(bytes.Length, fixture.Storage.ConteudoEnviado!.Length);
        Assert.Equal(1, fixture.Repository.Gravacoes);
    }

    [Fact]
    public async Task ProdutoInexistenteNaoCriaObjetoOrfao()
    {
        var fixture = new Fixture();
        fixture.Repository.Produto = null;
        await Assert.ThrowsAsync<ProdutoNaoEncontradoException>(() => fixture.Service.AtualizarAsync(42, new MemoryStream(Png)));
        Assert.Null(fixture.Storage.KeyEnviada);
        Assert.Equal(0, fixture.Repository.Gravacoes);
    }

    [Fact]
    public async Task FalhaNoStoragePreservaUrlAnteriorENaoTentaSalvar()
    {
        var fixture = new Fixture();
        var error = new IOException("Storage indisponível");
        fixture.Storage.ErroUpload = error;

        var thrown = await Assert.ThrowsAsync<IOException>(() => fixture.Service.AtualizarAsync(42, new MemoryStream(Png)));

        Assert.Same(error, thrown);
        Assert.Equal(Fixture.UrlAnterior, fixture.Repository.UrlPersistida);
        Assert.Equal(0, fixture.Repository.Gravacoes);
        Assert.Empty(fixture.Storage.KeysRemovidas);
    }

    [Fact]
    public async Task FalhaNoBancoRemoveSomenteOObjetoNovoEPreservaUrlPersistida()
    {
        var fixture = new Fixture();
        fixture.Repository.ErroGravacao = new InvalidOperationException("Banco indisponível");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.AtualizarAsync(42, new MemoryStream(Png)));

        Assert.Equal(Fixture.UrlAnterior, fixture.Repository.UrlPersistida);
        Assert.Equal([fixture.Storage.KeyEnviada!], fixture.Storage.KeysRemovidas);
    }

    [Fact]
    public async Task ExclusaoConcorrenteDoProdutoCompensaUpload()
    {
        var fixture = new Fixture();
        fixture.Repository.ErroGravacao = new ProdutoNaoEncontradoException(42);
        await Assert.ThrowsAsync<ProdutoNaoEncontradoException>(() => fixture.Service.AtualizarAsync(42, new MemoryStream(Png)));
        Assert.Equal([fixture.Storage.KeyEnviada!], fixture.Storage.KeysRemovidas);
    }

    [Fact]
    public async Task ErroNaCompensacaoNaoEscondeErroOriginalERegistraObjetoPendente()
    {
        var fixture = new Fixture();
        var persistenceError = new InvalidOperationException("Erro original");
        var cleanupError = new IOException("Falha ao remover objeto");
        fixture.Repository.ErroGravacao = persistenceError;
        fixture.Storage.ErroExclusao = cleanupError;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.AtualizarAsync(42, new MemoryStream(Png)));

        Assert.Same(persistenceError, thrown);
        Assert.Same(cleanupError, fixture.Logger.Error);
        Assert.Contains(fixture.Storage.KeyEnviada!, fixture.Logger.Message);
    }

    [Fact]
    public async Task CancelamentoDepoisDoUploadAindaRemoveObjeto()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Storage.DepoisDoUpload = cancellation.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Service.AtualizarAsync(42, new MemoryStream(Png), cancellation.Token));

        Assert.Equal([fixture.Storage.KeyEnviada!], fixture.Storage.KeysRemovidas);
        Assert.False(fixture.Storage.TokenExclusao.IsCancellationRequested);
        Assert.Equal(Fixture.UrlAnterior, fixture.Repository.UrlPersistida);
    }

    [Fact]
    public async Task CancelamentoInicialNaoAcessaDependencias()
    {
        var fixture = new Fixture();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Service.AtualizarAsync(42, new MemoryStream(Png), new CancellationToken(true)));
        Assert.Equal(0, fixture.Repository.Leituras);
        Assert.Null(fixture.Storage.KeyEnviada);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    [InlineData(null)]
    public async Task UrlInvalidaRetornadaPeloStorageNaoEntraNoBancoEObjetoEhRemovido(string? url)
    {
        var fixture = new Fixture();
        fixture.Storage.Url = url!;
        await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.AtualizarAsync(42, new MemoryStream(Png)));
        Assert.Equal(0, fixture.Repository.Gravacoes);
        Assert.Equal([fixture.Storage.KeyEnviada!], fixture.Storage.KeysRemovidas);
    }

    private sealed class Fixture
    {
        public const string UrlAnterior = "https://cdn.example.com/anterior.png";
        public StorageFake Storage { get; } = new();
        public RepositoryFake Repository { get; }
        public LoggerFake Logger { get; } = new();
        public ProdutoImagemService Service { get; }

        public Fixture()
        {
            Repository = new RepositoryFake(Storage);
            Service = new ProdutoImagemService(Repository, Storage, Logger);
        }
    }

    private sealed class RepositoryFake(StorageFake storage) : IProdutoRepository
    {
        public Produto? Produto { get; set; } = new("Produto", 10m, "Descrição", 1);
        public string? UrlPersistida { get; private set; } = Fixture.UrlAnterior;
        public int Leituras { get; private set; }
        public int Gravacoes { get; private set; }
        public bool ArquivoJaEnviadoAoSalvar { get; private set; }
        public Exception? ErroGravacao { get; set; }

        public Task<IReadOnlyList<ProdutoResumo>> ListarAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Produto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
        {
            Leituras++;
            return Task.FromResult(Produto);
        }

        public Task SalvarImagemAsync(long id, string? urlImagem, CancellationToken cancellationToken = default)
        {
            Gravacoes++;
            ArquivoJaEnviadoAoSalvar = storage.ConteudoEnviado is not null;
            cancellationToken.ThrowIfCancellationRequested();
            if (ErroGravacao is not null) throw ErroGravacao;
            UrlPersistida = urlImagem;
            return Task.CompletedTask;
        }
    }

    private sealed class StorageFake : IFileStorage
    {
        public string Url { get; set; } = "http://localhost:4566/kotshop-arquivos/produtos/42/nova.png";
        public string? KeyEnviada { get; private set; }
        public string? ContentType { get; private set; }
        public byte[]? ConteudoEnviado { get; private set; }
        public List<string> KeysRemovidas { get; } = [];
        public Exception? ErroUpload { get; set; }
        public Exception? ErroExclusao { get; set; }
        public Action? DepoisDoUpload { get; set; }
        public CancellationToken TokenExclusao { get; private set; }

        public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            if (ErroUpload is not null) throw ErroUpload;
            KeyEnviada = key;
            ContentType = contentType;
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            ConteudoEnviado = copy.ToArray();
            DepoisDoUpload?.Invoke();
            return Url;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            KeysRemovidas.Add(key);
            TokenExclusao = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (ErroExclusao is not null) throw ErroExclusao;
            return Task.CompletedTask;
        }
    }

    private sealed class LoggerFake : ILogger<ProdutoImagemService>
    {
        public Exception? Error { get; private set; }
        public string Message { get; private set; } = "";
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Error = exception;
            Message = formatter(state, exception);
        }
    }

    private sealed class StreamSemSeek(byte[] bytes) : Stream
    {
        private readonly MemoryStream inner = new(bytes);
        public int BytesLidos { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            BytesLidos += read;
            return read;
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
