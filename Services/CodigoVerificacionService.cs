using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace PeluCorte.Services;

public class CodigoVerificacionService
{
    private record Entry(string Codigo, DateTime Expira);

    private readonly ConcurrentDictionary<string, Entry> _store = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

    public string Generar(string clave)
    {
        var codigo = RandomNumberGenerator.GetInt32(100_000, 999_999).ToString();
        _store[clave] = new Entry(codigo, DateTime.UtcNow.Add(_ttl));
        return codigo;
    }

    public bool Validar(string clave, string codigo)
    {
        if (!_store.TryGetValue(clave, out var entry)) return false;
        if (DateTime.UtcNow > entry.Expira)
        {
            _store.TryRemove(clave, out _);
            return false;
        }
        if (!CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(entry.Codigo),
            System.Text.Encoding.UTF8.GetBytes(codigo ?? string.Empty))) return false;

        _store.TryRemove(clave, out _);
        return true;
    }
}
