using Bunit;
using KotShop.WebAdmin.Components;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace KotShop.WebAdmin.Tests;

public sealed class ProdutoImagemCardTests
{
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0];

    [Fact]
    public void Existing_image_url_is_previewed_and_changes_without_navigation()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var card = RenderCard(context, new("https://example.com/atual.jpg"));
        Assert.Equal("https://example.com/atual.jpg", card.Find("img").GetAttribute("src"));
        card.FindAll("input[type=radio]")[1].Change(true);
        card.Find("#imagem-url").Input("https://example.com/nova.png");
        Assert.Equal("https://example.com/nova.png", card.Find("img").GetAttribute("src"));
        card.Find("#imagem-url").Input("javascript:alert(1)");
        Assert.Empty(card.FindAll("img"));
        Assert.Contains("HTTP ou HTTPS", card.Find("[role=alert]").TextContent);
    }

    [Fact]
    public void Uploaded_file_is_previewed_and_can_be_removed_without_navigation()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var card = RenderCard(context, new("https://example.com/atual.jpg"));
        card.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Png, "nova.png"));
        card.WaitForAssertion(() => Assert.StartsWith("data:image/png;base64,", card.Find("img").GetAttribute("src")));
        Assert.Equal(Png, card.Instance.Valor.Arquivo);
        card.Find("button").Click();
        Assert.Null(card.Instance.Valor.Url);
        Assert.Null(card.Instance.Valor.Arquivo);
        Assert.Empty(card.FindAll("img"));
        Assert.Contains("Nenhuma imagem selecionada", card.Markup);
    }

    [Fact]
    public void Invalid_file_preserves_the_current_image()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var card = RenderCard(context, new("https://example.com/atual.jpg"));
        card.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "falsa.png"));
        card.WaitForAssertion(() => Assert.NotEmpty(card.FindAll("[role=alert]")));
        Assert.Equal("https://example.com/atual.jpg", card.Find("img").GetAttribute("src"));
        Assert.Null(card.Instance.Valor.Arquivo);
    }

    [Fact]
    public void Switching_to_url_discards_a_pending_file()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var card = RenderCard(context, new("https://example.com/atual.jpg"));
        card.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Png, "nova.png"));
        card.WaitForAssertion(() => Assert.NotNull(card.Instance.Valor.Arquivo));
        card.FindAll("input[type=radio]")[1].Change(true);
        Assert.Null(card.Instance.Valor.Arquivo);
        Assert.Equal("https://example.com/atual.jpg", card.Find("img").GetAttribute("src"));
    }

    private static IRenderedComponent<ProdutoImagemCard> RenderCard(BunitContext context, ProdutoImagemSelecao initial)
    {
        IRenderedComponent<ProdutoImagemCard>? card = null;
        card = context.Render<ProdutoImagemCard>(p => p.Add(c => c.Valor, initial)
            .Add(c => c.ValorChanged, value => card!.Render(p => p.Add(c => c.Valor, value))));
        return card;
    }
}
