using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PeluCorte.Services;

public static class SlugHelper
{
    public static string Crear(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return Guid.NewGuid().ToString("N")[..8];

        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var sinAcentos = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var slug = Regex.Replace(sinAcentos, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        slug = Regex.Replace(slug, @"-+", "-");
        if (string.IsNullOrEmpty(slug)) slug = Guid.NewGuid().ToString("N")[..8];
        return slug;
    }
}
