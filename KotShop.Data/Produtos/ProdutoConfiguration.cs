using KotShop.Domain.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KotShop.Data.Produtos;

public sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> entity)
    {
        entity.ToTable("Produtos");
        entity.Property(p => p.Nome).HasMaxLength(Produto.TamanhoMaximoNome);
        entity.Property(p => p.Preco).HasPrecision(18, 2);
        entity.Property(p => p.UrlImagem).HasMaxLength(Produto.TamanhoMaximoUrlImagem).IsRequired(false);
        entity.HasOne(p => p.Categoria).WithMany(c => c.Produtos)
            .HasForeignKey(p => p.CategoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}
