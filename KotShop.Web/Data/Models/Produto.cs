namespace KotShop.Web.Data.Models
{
    public class Produto
    {
        public long Id { get; set; }
        public required string Nome { get; set; }
        public decimal Preco { get; set; }
        public required string Descricao { get; set; }

        public long CategoriaId { get; set; }
        public Categoria Categoria { get; set; } = null!;
    }
}
