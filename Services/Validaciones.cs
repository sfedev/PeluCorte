using System.Text.RegularExpressions;

namespace PeluCorte.Services;

public static class Validaciones
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

    // Teléfonos españoles (sin prefijo internacional):
    //   Móviles: 6XXXXXXXX, 7XXXXXXXX
    //   Fijos:   8XXXXXXXX, 9XXXXXXXX
    // Total: 9 dígitos empezando por 6, 7, 8 ó 9.
    private static readonly Regex TelefonoEsRegex = new(
        @"^[6-9]\d{8}$",
        RegexOptions.Compiled);

    public static bool EsEmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        if (email.Length > 180) return false;
        return EmailRegex.IsMatch(email.Trim());
    }

    /// <summary>
    /// Normaliza un número de teléfono español: elimina espacios, guiones, paréntesis y
    /// el prefijo internacional (+34, 0034, 34) si lo trae. Devuelve solo los 9 dígitos
    /// si el número es válido, null si no.
    /// </summary>
    public static string? NormalizarTelefonoEs(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;

        // Quitar todo lo que no sea dígito o '+'
        var limpio = Regex.Replace(telefono, @"[^\d+]", "");

        // Quitar prefijo internacional español
        if (limpio.StartsWith("+34")) limpio = limpio[3..];
        else if (limpio.StartsWith("0034")) limpio = limpio[4..];
        else if (limpio.StartsWith("34") && limpio.Length == 11) limpio = limpio[2..];

        return TelefonoEsRegex.IsMatch(limpio) ? limpio : null;
    }

    public static bool EsTelefonoEspanaValido(string? telefono) =>
        NormalizarTelefonoEs(telefono) is not null;

    /// <summary>Devuelve "612 345 678" a partir de "612345678".</summary>
    public static string FormatearTelefono(string telefonoNormalizado)
    {
        if (telefonoNormalizado.Length != 9) return telefonoNormalizado;
        return $"{telefonoNormalizado[..3]} {telefonoNormalizado[3..6]} {telefonoNormalizado[6..]}";
    }

    public static string? TipoTelefonoEs(string telefonoNormalizado)
    {
        if (telefonoNormalizado.Length != 9) return null;
        return telefonoNormalizado[0] switch
        {
            '6' or '7' => "móvil",
            '8' or '9' => "fijo",
            _ => null
        };
    }
}
