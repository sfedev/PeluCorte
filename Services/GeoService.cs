using System.Text.Json;
using System.Text.RegularExpressions;

namespace PeluCorte.Services;

public class GeoService
{
    private readonly HttpClient _http;
    private readonly ILogger<GeoService> _logger;

    public GeoService(HttpClient http, ILogger<GeoService> logger)
    {
        _http = http;
        _logger = logger;
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", "PeluCorte/1.0 (geocoding)");
    }

    public static (double lat, double lng)? ParsearGoogleMapsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var m = Regex.Match(url, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
        if (m.Success &&
            double.TryParse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var la) &&
            double.TryParse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture, out var ln))
            return (la, ln);

        m = Regex.Match(url, @"[?&!]q=(-?\d+\.\d+),(-?\d+\.\d+)");
        if (m.Success &&
            double.TryParse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out la) &&
            double.TryParse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture, out ln))
            return (la, ln);

        m = Regex.Match(url, @"!3d(-?\d+\.\d+)!4d(-?\d+\.\d+)");
        if (m.Success &&
            double.TryParse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out la) &&
            double.TryParse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture, out ln))
            return (la, ln);

        return null;
    }

    /// <summary>
    /// Intenta extraer coordenadas de una URL de Google Maps. Si es una URL corta
    /// (maps.app.goo.gl, goo.gl/maps) sigue el redirect para obtener la URL larga
    /// y extraer coordenadas de ella.
    /// </summary>
    public async Task<(double lat, double lng)?> ResolverGoogleMapsUrlAsync(string? url, CancellationToken ct = default)
    {
        var directo = ParsearGoogleMapsUrl(url);
        if (directo is not null) return directo;

        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!EsUrlCortaGoogleMaps(url)) return null;

        try
        {
            // HttpClient sigue redirects automáticamente. Solo leemos headers, no body.
            using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            var finalUrl = resp.RequestMessage?.RequestUri?.ToString();
            return ParsearGoogleMapsUrl(finalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolviendo URL corta de Google Maps: {Url}", url);
            return null;
        }
    }

    private static bool EsUrlCortaGoogleMaps(string url) =>
        url.Contains("maps.app.goo.gl", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("goo.gl/maps", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("g.co/kgs", StringComparison.OrdinalIgnoreCase);

    public async Task<(double lat, double lng)?> GeocodificarAsync(string direccion, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(direccion)) return null;

        // Nominatim falla con direcciones largas o componentes que no reconoce
        // (ej. distritos con guiones tipo "San Blas-Canillejas"). Probamos versiones
        // progresivamente más cortas hasta que una funcione.
        foreach (var variante in GenerarVariantesDireccion(direccion))
        {
            var coords = await IntentarGeocodificarAsync(variante, ct);
            if (coords is not null) return coords;
            // Throttle: la política de Nominatim pide <= 1 req/seg.
            await Task.Delay(1000, ct);
        }
        return null;
    }

    private static IEnumerable<string> GenerarVariantesDireccion(string direccion)
    {
        var original = direccion.Trim();
        yield return original;

        var partes = original
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
        if (partes.Length < 3) yield break;

        // Variante 1: quitar la segunda parte (suele ser distrito/barrio).
        // Ej. "C. Zabalza, 1, San Blas-Canillejas, 28037 Madrid" → "C. Zabalza, 1, 28037 Madrid"
        var sinDistrito = string.Join(", ", new[] { partes[0] }.Concat(partes.Skip(2)));
        if (sinDistrito != original) yield return sinDistrito;

        // Variante 2: solo calle + última parte (ciudad/país).
        var calleCiudad = $"{partes[0]}, {partes[^1]}";
        if (calleCiudad != sinDistrito && calleCiudad != original) yield return calleCiudad;

        // Variante 3: solo la primera parte (calle).
        if (partes[0] != calleCiudad) yield return partes[0];
    }

    private async Task<(double lat, double lng)?> IntentarGeocodificarAsync(string q, CancellationToken ct)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(q)}";
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.GetArrayLength() == 0) return null;
            var primero = root[0];
            var lat = double.Parse(primero.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            var lon = double.Parse(primero.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            return (lat, lon);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error geocodificando '{Q}'", q);
            return null;
        }
    }

    public static double DistanciaKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0;
        var dLat = Grados(lat2 - lat1);
        var dLng = Grados(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(Grados(lat1)) * Math.Cos(Grados(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double Grados(double deg) => deg * Math.PI / 180.0;
}
