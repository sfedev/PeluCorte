using System.Collections.Concurrent;

namespace PeluCorte.Services;

/// <summary>
/// Rate limiter en memoria, por clave (típicamente IP). Singleton.
/// Útil para limitar acciones disparadas desde componentes Blazor interactivos
/// donde el RateLimiter de ASP.NET Core (que se aplica a endpoints) no llega.
/// </summary>
public class RateLimitService
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _hits = new();
    private DateTime _ultimaLimpieza = DateTime.UtcNow;

    /// <summary>
    /// Devuelve true si la acción se permite (y la cuenta como una llamada).
    /// Devuelve false si ya se han hecho <paramref name="maxEnVentana"/> llamadas
    /// dentro de la ventana de tiempo dada.
    /// </summary>
    public bool Permitir(string clave, int maxEnVentana, TimeSpan ventana)
    {
        var ahora = DateTime.UtcNow;
        var lista = _hits.GetOrAdd(clave, _ => new List<DateTime>());
        lock (lista)
        {
            lista.RemoveAll(t => ahora - t > ventana);
            if (lista.Count >= maxEnVentana) return false;
            lista.Add(ahora);
        }

        // Limpieza ocasional para no acumular claves muertas (cada 5 min).
        if (ahora - _ultimaLimpieza > TimeSpan.FromMinutes(5))
        {
            _ultimaLimpieza = ahora;
            foreach (var (k, v) in _hits)
            {
                lock (v)
                {
                    if (v.Count == 0 || v.All(t => ahora - t > TimeSpan.FromMinutes(15)))
                        _hits.TryRemove(k, out _);
                }
            }
        }
        return true;
    }
}
