using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KotShop.Data.Migrations.Catalogo
{
    /// <inheritdoc />
    public partial class AtualizaSeedProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "Descricao", "Nome", "Preco" },
                values: new object[] { "Modelo: SM-S942BZVKZTO", "Celular Samsung Galaxy S26 5G", 3999.90m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "Descricao", "Nome", "Preco" },
                values: new object[] { "12GB, GDDR7, DLSS, Ray Tracing, Modelo: ZT-B50700H-10A", "Placa de Vídeo Zotac Gaming NVIDIA GeForce RTX 5070 Twin Edge OC", 5499.90m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "Descricao", "Nome", "Preco" },
                values: new object[] { "3 Portas, Inox. Modelo: IM7S", "Geladeira Electrolux Frost Free Inverter 484L", 5399.90m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "Descricao", "Nome", "Preco" },
                values: new object[] { "Smartphone com 128 GB de armazenamento.", "Smartphone", 1999.90m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "Descricao", "Nome", "Preco" },
                values: new object[] { "Notebook com 16 GB de RAM e SSD de 512 GB.", "Notebook", 3499.90m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "Descricao", "Nome", "Preco" },
                values: new object[] { "Geladeira frost free de 400 litros.", "Geladeira", 2799.90m });
        }
    }
}
