using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeluCorte.Data;
using PeluCorte.Models;

namespace PeluCorte.Services;

public class PeluqueriaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly UserManager<ApplicationUser> _userManager;

    public PeluqueriaService(IDbContextFactory<ApplicationDbContext> factory, UserManager<ApplicationUser> userManager)
    {
        _factory = factory;
        _userManager = userManager;
    }

    public async Task<List<Peluqueria>> BuscarPorNombreAsync(string termino)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var t = $"%{termino.Trim()}%";
        return await db.Peluquerias
            .Where(p => p.Estado == EstadoSolicitud.Aprobada && EF.Functions.ILike(p.Nombre, t))
            .OrderBy(p => p.Nombre)
            .Take(30)
            .ToListAsync();
    }

    public async Task CerrarYEliminarAsync(Guid peluqueriaId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var pelu = await db.Peluquerias.FindAsync(peluqueriaId);
        if (pelu is null) return;

        var usuariosDeLaPelu = await db.Set<ApplicationUser>()
            .Where(u => u.PeluqueriaId == peluqueriaId)
            .ToListAsync();

        db.Peluquerias.Remove(pelu);
        await db.SaveChangesAsync();

        foreach (var u in usuariosDeLaPelu)
        {
            var fresh = await _userManager.FindByIdAsync(u.Id);
            if (fresh is not null) await _userManager.DeleteAsync(fresh);
        }
    }

    public async Task<List<Peluqueria>> ObtenerAprobadasAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluquerias
            .Where(p => p.Estado == EstadoSolicitud.Aprobada)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<List<Peluqueria>> ObtenerPendientesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluquerias
            .Where(p => p.Estado == EstadoSolicitud.Pendiente)
            .OrderBy(p => p.CreadaEl)
            .ToListAsync();
    }

    public async Task<List<Peluqueria>> ObtenerTodasAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluquerias
            .OrderByDescending(p => p.CreadaEl)
            .ToListAsync();
    }

    public async Task<Peluqueria?> ObtenerPorSlugAprobadaAsync(string slug)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluquerias
            .Include(p => p.Horarios)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Estado == EstadoSolicitud.Aprobada);
    }

    public async Task<Peluqueria?> ObtenerPorIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluquerias
            .Include(p => p.Horarios)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ExisteSlugAsync(string slug)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluquerias.AnyAsync(p => p.Slug == slug);
    }

    public async Task<string> GenerarSlugUnicoAsync(string nombre)
    {
        var baseSlug = SlugHelper.Crear(nombre);
        var slug = baseSlug;
        var i = 2;
        while (await ExisteSlugAsync(slug))
        {
            slug = $"{baseSlug}-{i++}";
        }
        return slug;
    }

    public async Task CrearAsync(Peluqueria pelu)
    {
        // Si no se han indicado horarios explícitos, sembramos 7 (todos abiertos por defecto).
        if (pelu.Horarios.Count == 0)
            pelu.Horarios = HorariosPorDefecto();

        await using var db = await _factory.CreateDbContextAsync();
        db.Peluquerias.Add(pelu);
        await db.SaveChangesAsync();
    }

    public async Task ActualizarHorariosAsync(Guid peluqueriaId, IEnumerable<HorarioDia> horarios)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existentes = await db.Horarios.Where(h => h.PeluqueriaId == peluqueriaId).ToListAsync();

        foreach (var nuevo in horarios)
        {
            var act = existentes.FirstOrDefault(h => h.Dia == nuevo.Dia);
            if (act is null)
            {
                db.Horarios.Add(new HorarioDia
                {
                    PeluqueriaId = peluqueriaId,
                    Dia = nuevo.Dia,
                    Abierto = nuevo.Abierto,
                    Apertura = nuevo.Apertura,
                    Cierre = nuevo.Cierre
                });
            }
            else
            {
                act.Abierto = nuevo.Abierto;
                act.Apertura = nuevo.Apertura;
                act.Cierre = nuevo.Cierre;
            }
        }
        await db.SaveChangesAsync();
    }

    public static List<HorarioDia> HorariosPorDefecto()
    {
        var lista = new List<HorarioDia>();
        foreach (DayOfWeek d in Enum.GetValues<DayOfWeek>())
        {
            lista.Add(new HorarioDia
            {
                Dia = d,
                Abierto = true,
                Apertura = new TimeOnly(9, 0),
                Cierre = new TimeOnly(21, 0)
            });
        }
        return lista;
    }

    public async Task AprobarAsync(Guid id, string metodoVerificacion = "Manual")
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Peluquerias.FindAsync(id);
        if (p is null) return;
        p.Estado = EstadoSolicitud.Aprobada;
        p.MotivoRechazo = null;
        p.MetodoVerificacion = metodoVerificacion;
        await db.SaveChangesAsync();
    }

    public async Task RechazarAsync(Guid id, string motivo)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Peluquerias.FindAsync(id);
        if (p is null) return;
        p.Estado = EstadoSolicitud.Rechazada;
        p.MotivoRechazo = motivo;
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Peluquerias.FindAsync(id);
        if (p is null) return;
        db.Peluquerias.Remove(p);
        await db.SaveChangesAsync();
    }

    public async Task ActualizarHorarioAsync(Guid id, TimeOnly apertura, TimeOnly cierre)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Peluquerias.FindAsync(id);
        if (p is null) return;
        p.HoraApertura = apertura;
        p.HoraCierre = cierre;
        await db.SaveChangesAsync();
    }

    public async Task ActualizarPerfilAsync(Guid id, string nombre, string direccion, string? googleMapsUrl,
        double latitud, double longitud, string telefono, string emailContacto)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Peluquerias.FindAsync(id);
        if (p is null) return;
        p.Nombre = nombre.Trim();
        p.Direccion = direccion.Trim();
        p.GoogleMapsUrl = googleMapsUrl;
        p.Latitud = latitud;
        p.Longitud = longitud;
        p.Telefono = telefono.Trim();
        p.EmailContacto = emailContacto.Trim();
        await db.SaveChangesAsync();
    }
}
