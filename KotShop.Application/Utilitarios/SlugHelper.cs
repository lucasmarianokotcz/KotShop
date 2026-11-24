using System.Globalization;
using System.Text;

namespace KotShop.Application.Utilitarios;

public static class SlugHelper
{
    public static string Criar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        var resultado = new StringBuilder();
        var separadorPendente = false;

        foreach (var caractere in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var minusculo = char.ToLowerInvariant(caractere);
            if (minusculo is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (separadorPendente && resultado.Length > 0)
                {
                    resultado.Append('-');
                }

                resultado.Append(minusculo);
                separadorPendente = false;
            }
            else
            {
                separadorPendente = true;
            }
        }

        return resultado.ToString();
    }
}
