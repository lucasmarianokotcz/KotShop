using System.ComponentModel.DataAnnotations;

namespace KotShop.WebAdmin.Components.Pages;

public sealed class ProdutoFormModel
{
    [Required(ErrorMessage = "Informe o nome."), StringLength(255, ErrorMessage = "Use até 255 caracteres no nome.")]
    public string Nome { get; set; } = "";
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Informe um preço válido, maior ou igual a zero.")]
    public decimal Preco { get; set; }
    [Required(ErrorMessage = "Informe a descrição.")]
    public string Descricao { get; set; } = "";
    [Range(1, long.MaxValue, ErrorMessage = "Selecione uma categoria.")]
    public long CategoriaId { get; set; }
    [StringLength(1000, ErrorMessage = "A URL deve ter até 1000 caracteres.")]
    public string? UrlImagem { get; set; }
}
