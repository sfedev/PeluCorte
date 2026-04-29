using Microsoft.EntityFrameworkCore;
using PeluCorte.Data;

namespace PeluCorte.Services;

public class EstadisticasService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public EstadisticasService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<Resumen> ObtenerResumenAsync(Guid peluqueriaId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + (hoy.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);

        var citasMes = await db.Citas
            .Where(c => c.PeluqueriaId == peluqueriaId && c.Fecha >= inicioMes)
            .ToListAsync();

        var citasSemana = citasMes.Where(c => c.Fecha >= inicioSemana).Count();
        var citasHoy = citasMes.Where(c => c.Fecha == hoy).Count();

        var porPeluquero = citasMes
            .GroupBy(c => c.PeluqueroId)
            .Select(g => new { g.Key, Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();

        string? topPeluqueroNombre = null;
        int topPeluqueroCount = 0;
        if (porPeluquero is not null)
        {
            var p = await db.Peluqueros.FindAsync(porPeluquero.Key);
            topPeluqueroNombre = p?.Nombre;
            topPeluqueroCount = porPeluquero.Total;
        }

        var horaPopular = citasMes
            .GroupBy(c => c.Hora)
            .OrderByDescending(g => g.Count())
            .Select(g => (TimeOnly?)g.Key)
            .FirstOrDefault();

        var citasFuturas = await db.Citas
            .CountAsync(c => c.PeluqueriaId == peluqueriaId && c.Fecha >= hoy);

        return new Resumen(
            CitasHoy: citasHoy,
            CitasSemana: citasSemana,
            CitasMes: citasMes.Count,
            CitasFuturas: citasFuturas,
            TopPeluqueroNombre: topPeluqueroNombre,
            TopPeluqueroCount: topPeluqueroCount,
            HoraMasPopular: horaPopular
        );
    }

    public async Task<List<DiaConteo>> ObtenerCitasPorDiaAsync(Guid peluqueriaId, int diasAtras = 30)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var desde = DateOnly.FromDateTime(DateTime.Today).AddDays(-diasAtras);
        var datos = await db.Citas
            .Where(c => c.PeluqueriaId == peluqueriaId && c.Fecha >= desde)
            .GroupBy(c => c.Fecha)
            .Select(g => new { Fecha = g.Key, Total = g.Count() })
            .OrderBy(x => x.Fecha)
            .ToListAsync();
        return datos.Select(d => new DiaConteo(d.Fecha, d.Total)).ToList();
    }

    public record Resumen(
        int CitasHoy,
        int CitasSemana,
        int CitasMes,
        int CitasFuturas,
        string? TopPeluqueroNombre,
        int TopPeluqueroCount,
        TimeOnly? HoraMasPopular
    );

    public record DiaConteo(DateOnly Fecha, int Total);
}
