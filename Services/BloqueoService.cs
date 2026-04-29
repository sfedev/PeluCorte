using Microsoft.EntityFrameworkCore;
using PeluCorte.Data;
using PeluCorte.Models;

namespace PeluCorte.Services;

public class BloqueoService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public BloqueoService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<BloqueoHorario>> ObtenerPorPeluqueroAsync(Guid peluqueroId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        // La columna es timestamp without time zone, así que comparamos contra
        // un DateTime con Kind=Unspecified (hora local del salón).
        var ahora = DateTime.SpecifyKind(DateTime.Now.AddDays(-1), DateTimeKind.Unspecified);
        return await db.Bloqueos
            .Where(b => b.PeluqueroId == peluqueroId && b.Fin >= ahora)
            .OrderBy(b => b.Inicio)
            .ToListAsync();
    }

    public async Task<List<BloqueoHorario>> ObtenerActivosEnDiaAsync(Guid peluqueroId, DateOnly fecha)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var inicioDia = DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var finDia = DateTime.SpecifyKind(fecha.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return await db.Bloqueos
            .Where(b => b.PeluqueroId == peluqueroId &&
                        b.Inicio < finDia && b.Fin > inicioDia)
            .ToListAsync();
    }

    public async Task<bool> CrearAsync(Guid peluqueroId, DateTime inicio, DateTime fin, string? motivo)
    {
        if (fin <= inicio) return false;
        await using var db = await _factory.CreateDbContextAsync();
        db.Bloqueos.Add(new BloqueoHorario
        {
            PeluqueroId = peluqueroId,
            Inicio = DateTime.SpecifyKind(inicio, DateTimeKind.Unspecified),
            Fin = DateTime.SpecifyKind(fin, DateTimeKind.Unspecified),
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim()
        });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task EliminarAsync(Guid id, Guid? peluqueriaIdEsperada = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var b = await db.Bloqueos.Include(x => x.Peluquero).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return;
        if (peluqueriaIdEsperada is not null && b.Peluquero?.PeluqueriaId != peluqueriaIdEsperada) return;
        db.Bloqueos.Remove(b);
        await db.SaveChangesAsync();
    }

    public async Task<bool> EliminarSiPertenecePeluqueroAsync(Guid bloqueoId, Guid peluqueroId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var b = await db.Bloqueos.FindAsync(bloqueoId);
        if (b is null || b.PeluqueroId != peluqueroId) return false;
        db.Bloqueos.Remove(b);
        await db.SaveChangesAsync();
        return true;
    }

    public static bool SolapaConCita(BloqueoHorario b, DateOnly fecha, TimeOnly hora, int duracionMin)
    {
        var inicio = fecha.ToDateTime(hora);
        var fin = inicio.AddMinutes(duracionMin);
        return b.Inicio < fin && b.Fin > inicio;
    }
}
