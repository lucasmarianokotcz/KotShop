using KotShop.Domain.Categorias;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KotShop.Data.Categorias;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> entity)
    {
        entity.ToTable("Categorias");
        entity.Property(c => c.Nome).HasMaxLength(Categoria.TamanhoMaximoNome);
    }
}
