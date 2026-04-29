using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PeluCorte.Services;

/// <summary>
/// Verifica automáticamente si una ubicación corresponde a una peluquería real
/// consultando OpenStreetMap. 100% gratis, sin API key.
/// </summary>
public class VerificadorPeluqueriaService
{
    private readonly HttpClient _http;
    private readonly ILogger<VerificadorPeluqueriaService> _logger;

    // Etiquetas OSM que consideramos "peluquería o servicio capilar".
    private static readonly (string Key, string Value)[] TagsPeluqueria =
    {
        ("shop", "hairdresser"),
        ("shop", "barber"),
        ("shop", "beauty"),
        ("amenity", "hairdresser"),
        ("amenity", "beauty_salon"),
        ("craft", "hairdresser"),
        ("office", "hairdresser"),
    };

    private const int RadioMetros = 150;

    public VerificadorPeluqueriaService(HttpClient http, ILogger<VerificadorPeluqueriaService> logger)
    {
        _http = http;
        _logger = logger;
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", "PeluCorte/1.0 (verificacion-peluqueria)");
    }

    public record ResultadoVerificacion(bool EsPeluqueria, string? Metodo, string? Detalle);

    public async Task<ResultadoVerificacion> VerificarAsync(double lat, double lng, CancellationToken ct = default)
    {
        // 1) Overpass — consulta directa por etiquetas en un radio amplio
        var porOverpass = await ConsultarOverpassAsync(lat, lng, ct);
        if (porOverpass.EsPeluqueria) return porOverpass;

        // 2) Nominatim reverse — fallback por si Overpass no la indexa
        var porNominatim = await ConsultarNominatimReverseAsync(lat, lng, ct);
        if (porNominatim.EsPeluqueria) return porNominatim;

        return new ResultadoVerificacion(false, null,
            "No la encontramos en OpenStreetMap. Es habitual: revisaremos manualmente y te avisaremos.");
    }

    private async Task<ResultadoVerificacion> ConsultarOverpassAsync(double lat, double lng, CancellationToken ct)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("[out:json][timeout:10];");
        sb.AppendLine("(");
        foreach (var (key, value) in TagsPeluqueria)
        {
            sb.AppendLine($"  node[\"{key}\"=\"{value}\"](around:{RadioMetros},{lat.ToString(inv)},{lng.ToString(inv)});");
            sb.AppendLine($"  way[\"{key}\"=\"{value}\"](around:{RadioMetros},{lat.ToString(inv)},{lng.ToString(inv)});");
        }
        sb.AppendLine(");");
        sb.AppendLine("out tags 10;");
        var query = sb.ToString();

        try
        {
            var content = new StringContent("data=" + Uri.EscapeDataString(query),
                Encoding.UTF8, "application/x-www-form-urlencoded");
            using var resp = await _http.PostAsync("https://overpass-api.de/api/interpreter", content, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Overpass devolvió {Code}", resp.StatusCode);
                return new ResultadoVerificacion(false, null, "Servicio Overpass no disponible");
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var elements = doc.RootElement.GetProperty("elements");
            if (elements.GetArrayLength() == 0)
                return new ResultadoVerificacion(false, null, "Sin coincidencia en Overpass");

            string? nombreOsm = null;
            foreach (var el in elements.EnumerateArray())
            {
                if (el.TryGetProperty("tags", out var tags) &&
                    tags.TryGetProperty("name", out var name))
                {
                    nombreOsm = name.GetString();
                    break;
                }
            }
            _logger.LogInformation("Verificación OSM: peluquería confirmada (nombre OSM: {Nombre})", nombreOsm ?? "(sin nombre)");
            return new ResultadoVerificacion(true, "OSM-Overpass", nombreOsm);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando Overpass");
            return new ResultadoVerificacion(false, null, "Error de red en Overpass");
        }
    }

    private async Task<ResultadoVerificacion> ConsultarNominatimReverseAsync(double lat, double lng, CancellationToken ct)
    {
        var inv = CultureInfo.InvariantCulture;
        var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat.ToString(inv)}&lon={lng.ToString(inv)}&zoom=18&extratags=1";

        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return new ResultadoVerificacion(false, null, "Nominatim no disponible");

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? clase = root.TryGetProperty("class", out var c) ? c.GetString() : null;
            string? tipo = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            string? nombre = root.TryGetProperty("name", out var n) ? n.GetString() : null;

            // Match si la clase/tipo encajan con peluquería
            var coincide = TagsPeluqueria.Any(tag =>
                string.Equals(clase, tag.Key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(tipo, tag.Value, StringComparison.OrdinalIgnoreCase));

            if (coincide)
            {
                _logger.LogInformation("Verificación OSM: peluquería confirmada vía Nominatim ({Clase}={Tipo})", clase, tipo);
                return new ResultadoVerificacion(true, "OSM-Nominatim", nombre);
            }

            return new ResultadoVerificacion(false, null, $"Nominatim devolvió {clase}/{tipo}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando Nominatim reverse");
            return new ResultadoVerificacion(false, null, "Error de red en Nominatim");
        }
    }
}
