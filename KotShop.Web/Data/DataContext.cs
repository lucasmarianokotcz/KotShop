using KotShop.Web.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace KotShop.Web.Data
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.Property(p => p.Nome)
                    .HasMaxLength(255);

                entity.Property(p => p.Preco)
                    .HasPrecision(18, 2);

                entity.HasOne(p => p.Categoria)
                    .WithMany(c => c.Produtos)
                    .HasForeignKey(p => p.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.Property(c => c.Nome)
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Categoria>().HasData(
                new { Id = 1L, Nome = "Eletrônicos" },
                new { Id = 2L, Nome = "Informática" },
                new { Id = 3L, Nome = "Eletrodomésticos" }
            );

            modelBuilder.Entity<Produto>().HasData(
                new
                {
                    Id = 1L,
                    Nome = "Smartphone",
                    Preco = 1999.90m,
                    Descricao = "Smartphone com 128 GB de armazenamento.",
                    CategoriaId = 1L
                },
                new
                {
                    Id = 2L,
                    Nome = "Notebook",
                    Preco = 3499.90m,
                    Descricao = "Notebook com 16 GB de RAM e SSD de 512 GB.",
                    CategoriaId = 2L
                },
                new
                {
                    Id = 3L,
                    Nome = "Geladeira",
                    Preco = 2799.90m,
                    Descricao = "Geladeira frost free de 400 litros.",
                    CategoriaId = 3L
                }
            );
        }

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
    }
}
