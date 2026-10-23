namespace KotShop.Web.Data.Models
{
    public class Categoria
    {
        public long Id { get; set; }
        public required string Nome { get; set; }
        public ICollection<Produto> Produtos { get; set; } = [];
    }
}
