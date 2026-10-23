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
        }

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
    }
}
