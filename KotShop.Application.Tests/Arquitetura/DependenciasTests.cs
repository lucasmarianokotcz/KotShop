using KotShop.Application.Produtos.AtualizarImagem;
using KotShop.Domain.Produtos;
using Xunit;

namespace KotShop.Application.Tests.Arquitetura;

public class DependenciasTests
{
    [Fact]
    public void DomainPermaneceIndependenteDeFrameworksEOutrasCamadas()
    {
        var references = typeof(Produto).Assembly.GetReferencedAssemblies();
        Assert.All(references, reference => Assert.StartsWith("System.", reference.Name));
    }

    [Fact]
    public void CasosDeUsoNaoConhecemPersistenciaHttpOuProvedorDeArquivos()
    {
        var references = typeof(ProdutoImagemService).Assembly.GetReferencedAssemblies();
        var forbiddenPrefixes = new[] { "KotShop.Data", "KotShop.Infrastructure", "KotShop.Web",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "AWSSDK", "Npgsql" };
        Assert.DoesNotContain(references, reference => forbiddenPrefixes.Any(prefix =>
            reference.Name!.StartsWith(prefix, StringComparison.Ordinal)));
    }
}
