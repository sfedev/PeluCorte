using Microsoft.EntityFrameworkCore;
using PeluCorte.Data;
using PeluCorte.Models;

namespace PeluCorte.Services;

public class ServicioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ServicioService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Servicio>> ObtenerPorPeluqueriaAsync(Guid peluqueriaId, bool soloActivos = false)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Servicios.Where(s => s.PeluqueriaId == peluqueriaId);
        if (soloActivos) q = q.Where(s => s.Activo);
        return await q.OrderBy(s => s.Nombre).ToListAsync();
    }

    public async Task<Servicio?> ObtenerPorIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Servicios.FindAsync(id);
    }

    public async Task CrearAsync(Guid peluqueriaId, string nombre, int duracion, decimal? precio)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Servicios.Add(new Servicio
        {
            PeluqueriaId = peluqueriaId,
            Nombre = nombre.Trim(),
            DuracionMinutos = Math.Max(15, duracion),
            Precio = precio
        });
        await db.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Guid id, string nombre, int duracion, decimal? precio, bool activo)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var s = await db.Servicios.FindAsync(id);
        if (s is null) return;
        s.Nombre = nombre.Trim();
        s.DuracionMinutos = Math.Max(15, duracion);
        s.Precio = precio;
        s.Activo = activo;
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(Guid id, Guid? peluqueriaIdEsperada = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var s = await db.Servicios.FindAsync(id);
        if (s is null) return;
        if (peluqueriaIdEsperada is not null && s.PeluqueriaId != peluqueriaIdEsperada) return;
        db.Servicios.Remove(s);
        await db.SaveChangesAsync();
    }
}
