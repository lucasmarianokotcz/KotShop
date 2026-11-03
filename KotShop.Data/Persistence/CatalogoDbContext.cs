using KotShop.Domain.Produtos;
using KotShop.Domain.Categorias;

using Microsoft.EntityFrameworkCore;

namespace KotShop.Data.Persistence
{
    public class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogoDbContext).Assembly);

            modelBuilder.Entity<Categoria>().HasData(
                new { Id = 1L, Nome = "Eletrônicos" },
                new { Id = 2L, Nome = "Informática" },
                new { Id = 3L, Nome = "Eletrodomésticos" }
            );

            modelBuilder.Entity<Produto>().HasData(
                new
                {
                    Id = 1L,
                    Nome = "Celular Samsung Galaxy S26 5G",
                    Preco = 3999.90m,
                    Descricao = "Modelo: SM-S942BZVKZTO",
                    UrlImagem = "https://samsungbrshop.vtexassets.com/arquivos/ids/278675-800-auto?v=639076212705630000",
                    CategoriaId = 1L
                },
                new
                {
                    Id = 2L,
                    Nome = "Placa de Vídeo Zotac Gaming NVIDIA GeForce RTX 5070 Twin Edge OC",
                    Preco = 5499.90m,
                    Descricao = "12GB, GDDR7, DLSS, Ray Tracing, Modelo: ZT-B50700H-10A",
                    UrlImagem = "https://img.terabyteshop.com.br/produto/g/placa-de-video-zotac-gaming-nvidia-geforce-rtx-5070-twin-edge-oc-12gb-gddr7-dlss-ray-tracing-zt-b50700h-10a_280025.jpg",
                    CategoriaId = 2L
                },
                new
                {
                    Id = 3L,
                    Nome = "Geladeira Electrolux Frost Free Inverter 484L",
                    Preco = 5399.90m,
                    Descricao = "3 Portas, Inox. Modelo: IM7S",
                    UrlImagem = "https://electrolux.vtexassets.com/arquivos/ids/288686-1000-1000?v=639046868002630000&width=1000&height=1000&aspect=true&format=auto",
                    CategoriaId = 3L
                }
            );
        }

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
    }
}
