using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KotShop.Data.Migrations.Catalogo
{
    /// <inheritdoc />
    public partial class AtualizaSeedProdutosV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 1L,
                column: "UrlImagem",
                value: "https://samsungbrshop.vtexassets.com/arquivos/ids/278675-800-auto?v=639076212705630000");

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 2L,
                column: "UrlImagem",
                value: "https://img.terabyteshop.com.br/produto/g/placa-de-video-zotac-gaming-nvidia-geforce-rtx-5070-twin-edge-oc-12gb-gddr7-dlss-ray-tracing-zt-b50700h-10a_280025.jpg");

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 3L,
                column: "UrlImagem",
                value: "https://electrolux.vtexassets.com/arquivos/ids/288686-1000-1000?v=639046868002630000&width=1000&height=1000&aspect=true&format=auto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 1L,
                column: "UrlImagem",
                value: null);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 2L,
                column: "UrlImagem",
                value: null);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 3L,
                column: "UrlImagem",
                value: null);
        }
    }
}
