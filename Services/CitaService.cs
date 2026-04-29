using Microsoft.EntityFrameworkCore;
using PeluCorte.Data;
using PeluCorte.Models;

namespace PeluCorte.Services;

public class CitaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly BloqueoService _bloqueos;
    private readonly EmailService _email;

    public CitaService(IDbContextFactory<ApplicationDbContext> factory, BloqueoService bloqueos, EmailService email)
    {
        _factory = factory;
        _bloqueos = bloqueos;
        _email = email;
    }

    public async Task<List<Cita>> ObtenerPorFechaAsync(Guid peluqueriaId, DateOnly fecha, Guid? peluqueroId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Citas.Where(c => c.PeluqueriaId == peluqueriaId && c.Fecha == fecha);
        if (peluqueroId is not null)
            q = q.Where(c => c.PeluqueroId == peluqueroId);
        return await q.OrderBy(c => c.Hora).ToListAsync();
    }

    public async Task<List<Cita>> ObtenerPorPeluqueriaAsync(Guid peluqueriaId, bool incluirPasadas = true)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Citas.Where(c => c.PeluqueriaId == peluqueriaId);
        if (!incluirPasadas)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            q = q.Where(c => c.Fecha >= hoy);
        }
        return await q.OrderBy(c => c.Fecha).ThenBy(c => c.Hora).ToListAsync();
    }

    public async Task<List<Cita>> ObtenerHistoricoAsync(Guid peluqueriaId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return await db.Citas
            .Where(c => c.PeluqueriaId == peluqueriaId && c.Fecha < hoy)
            .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Hora)
            .ToListAsync();
    }

    public async Task<List<Cita>> ObtenerPorTelefonoAsync(string telefono, bool soloFuturas = true)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var q = db.Citas
            .Include(c => c.Peluqueria)
            .Include(c => c.Peluquero)
            .Where(c => c.Telefono == telefono);
        if (soloFuturas) q = q.Where(c => c.Fecha >= hoy);
        return await q.OrderBy(c => c.Fecha).ThenBy(c => c.Hora).ToListAsync();
    }

    public async Task<HashSet<TimeOnly>> ObtenerHorasOcupadasAsync(Guid peluqueroId, DateOnly fecha)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var horas = await db.Citas
            .Where(c => c.PeluqueroId == peluqueroId && c.Fecha == fecha)
            .Select(c => c.Hora)
            .ToListAsync();
        return horas.ToHashSet();
    }

    public async Task<HashSet<TimeOnly>> ObtenerHorasDisponiblesAsync(
        Guid peluqueroId, DateOnly fecha, int duracionMin, HorarioDia horarioDelDia)
    {
        if (!horarioDelDia.Abierto) return new HashSet<TimeOnly>();

        await using var db = await _factory.CreateDbContextAsync();
        var citas = await db.Citas
            .Where(c => c.PeluqueroId == peluqueroId && c.Fecha == fecha)
            .Select(c => new DisponibilidadCalculator.CitaResumen(c.Hora, c.DuracionMinutos))
            .ToListAsync();
        var bloqueos = await _bloqueos.ObtenerActivosEnDiaAsync(peluqueroId, fecha);

        return DisponibilidadCalculator.CalcularDisponibles(
            citas, bloqueos, fecha, horarioDelDia.Apertura, horarioDelDia.Cierre, duracionMin);
    }

    public async Task<(bool Ok, string? Error)> CrearAsync(Cita cita, HorarioDia horarioDelDia)
    {
        var validacion = ValidarCita(cita, horarioDelDia);
        if (validacion is not null) return (false, validacion);

        cita.Nombre = SanearTexto(cita.Nombre, 80);
        cita.Telefono = SanearTexto(cita.Telefono, 30);
        if (!string.IsNullOrWhiteSpace(cita.Email))
            cita.Email = SanearTexto(cita.Email, 180);

        await using var db = await _factory.CreateDbContextAsync();

        if (await SolapaConOtrasAsync(db, cita.PeluqueroId, cita.Fecha, cita.Hora, cita.DuracionMinutos, ignorarId: null))
            return (false, "Esa hora ya está ocupada para ese peluquero.");

        var bloqueos = await _bloqueos.ObtenerActivosEnDiaAsync(cita.PeluqueroId, cita.Fecha);
        if (bloqueos.Any(b => BloqueoService.SolapaConCita(b, cita.Fecha, cita.Hora, cita.DuracionMinutos)))
            return (false, "El peluquero no está disponible en ese horario.");

        try
        {
            db.Citas.Add(cita);
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Esa hora se acaba de ocupar. Por favor, elige otra.");
        }
    }

    public async Task<(bool Ok, string? Error)> ActualizarAsync(Cita actualizada, HorarioDia horarioDelDia)
    {
        var validacion = ValidarCita(actualizada, horarioDelDia);
        if (validacion is not null) return (false, validacion);

        await using var db = await _factory.CreateDbContextAsync();
        var existente = await db.Citas.FindAsync(actualizada.Id);
        if (existente is null) return (false, "Cita no encontrada.");

        if (await SolapaConOtrasAsync(db, actualizada.PeluqueroId, actualizada.Fecha, actualizada.Hora, actualizada.DuracionMinutos, ignorarId: actualizada.Id))
            return (false, "Esa hora ya está ocupada.");

        var fechaAntigua = existente.Fecha;
        var horaAntigua = existente.Hora;

        existente.Nombre = SanearTexto(actualizada.Nombre, 80);
        existente.Telefono = SanearTexto(actualizada.Telefono, 30);
        existente.Email = string.IsNullOrWhiteSpace(actualizada.Email) ? null : SanearTexto(actualizada.Email, 180);
        existente.Fecha = actualizada.Fecha;
        existente.Hora = actualizada.Hora;
        existente.DuracionMinutos = actualizada.DuracionMinutos;
        existente.PeluqueroId = actualizada.PeluqueroId;
        existente.ServicioId = actualizada.ServicioId;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Esa hora se acaba de ocupar. Por favor, elige otra.");
        }

        await NotificarCambioSiProcedeAsync(db, existente, fechaAntigua, horaAntigua);
        return (true, null);
    }

    private async Task NotificarCambioSiProcedeAsync(ApplicationDbContext db, Cita cita, DateOnly fechaAntigua, TimeOnly horaAntigua)
    {
        if (string.IsNullOrWhiteSpace(cita.Email)) return;
        if (cita.Fecha == fechaAntigua && cita.Hora == horaAntigua) return;

        var pelu = await db.Peluquerias.FindAsync(cita.PeluqueriaId);
        var peluquero = await db.Peluqueros.FindAsync(cita.PeluqueroId);
        if (pelu is null || peluquero is null) return;
        await _email.NotificarCambioCitaAsync(cita, pelu, peluquero, fechaAntigua, horaAntigua);
    }

    public async Task<(bool Ok, string? Error)> MoverAsync(Guid id, DateOnly fecha, TimeOnly hora, Guid peluqueroId, HorarioDia horarioDelDia)
    {
        if (!horarioDelDia.Abierto) return (false, "La peluquería no abre ese día.");

        await using var db = await _factory.CreateDbContextAsync();
        var cita = await db.Citas.FindAsync(id);
        if (cita is null) return (false, "Cita no encontrada.");

        if (hora < horarioDelDia.Apertura || hora.AddMinutes(cita.DuracionMinutos) > horarioDelDia.Cierre.AddMinutes(1))
            return (false, "Hora fuera del horario de apertura.");

        if (await SolapaConOtrasAsync(db, peluqueroId, fecha, hora, cita.DuracionMinutos, ignorarId: id))
            return (false, "Esa hora ya está ocupada.");

        var bloqueos = await _bloqueos.ObtenerActivosEnDiaAsync(peluqueroId, fecha);
        if (bloqueos.Any(b => BloqueoService.SolapaConCita(b, fecha, hora, cita.DuracionMinutos)))
            return (false, "El peluquero no está disponible en ese horario.");

        var fechaAntigua = cita.Fecha;
        var horaAntigua = cita.Hora;

        cita.Fecha = fecha;
        cita.Hora = hora;
        cita.PeluqueroId = peluqueroId;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Esa hora se acaba de ocupar. Por favor, elige otra.");
        }

        await NotificarCambioSiProcedeAsync(db, cita, fechaAntigua, horaAntigua);
        return (true, null);
    }

    public async Task EliminarAsync(Guid id, Guid? peluqueriaIdEsperada = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var cita = await db.Citas.FindAsync(id);
        if (cita is null) return;
        if (peluqueriaIdEsperada is not null && cita.PeluqueriaId != peluqueriaIdEsperada) return;
        db.Citas.Remove(cita);
        await db.SaveChangesAsync();
    }

    public async Task<bool> EliminarSiPertenecePeluqueroAsync(Guid citaId, Guid peluqueroId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var cita = await db.Citas.FindAsync(citaId);
        if (cita is null || cita.PeluqueroId != peluqueroId) return false;
        db.Citas.Remove(cita);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Cita?> ObtenerPorTokenAsync(string token)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Citas
            .Include(c => c.Peluquero)!.ThenInclude(p => p!.User)
            .Include(c => c.Peluqueria)
            .FirstOrDefaultAsync(c => c.CancelToken == token);
    }

    public async Task<bool> CancelarPorTokenAsync(string token)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var cita = await db.Citas.FirstOrDefaultAsync(c => c.CancelToken == token);
        if (cita is null) return false;
        db.Citas.Remove(cita);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Cita>> ObtenerPorPeluqueroAsync(Guid peluqueroId, DateOnly fecha)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Citas
            .Where(c => c.PeluqueroId == peluqueroId && c.Fecha == fecha)
            .OrderBy(c => c.Hora)
            .ToListAsync();
    }

    public async Task<Cita?> ProximaCitaPeluqueroAsync(Guid peluqueroId, DateTime ahoraUtc)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var hoy = DateOnly.FromDateTime(ahoraUtc.ToLocalTime());
        var ahora = TimeOnly.FromDateTime(ahoraUtc.ToLocalTime());
        return await db.Citas
            .Where(c => c.PeluqueroId == peluqueroId &&
                       (c.Fecha > hoy || (c.Fecha == hoy && c.Hora >= ahora)))
            .OrderBy(c => c.Fecha).ThenBy(c => c.Hora)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Cita>> ObtenerParaRecordatorioAsync(DateOnly fechaObjetivo)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Citas
            .Include(c => c.Peluqueria)
            .Include(c => c.Peluquero)
            .Where(c => c.Fecha == fechaObjetivo
                     && c.Email != null
                     && c.RecordatorioEnviadoEl == null)
            .ToListAsync();
    }

    public async Task MarcarRecordatorioEnviadoAsync(Guid citaId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var c = await db.Citas.FindAsync(citaId);
        if (c is null) return;
        c.RecordatorioEnviadoEl = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task<bool> SolapaConOtrasAsync(ApplicationDbContext db, Guid peluqueroId, DateOnly fecha, TimeOnly hora, int duracion, Guid? ignorarId)
    {
        var citas = await db.Citas
            .Where(c => c.PeluqueroId == peluqueroId && c.Fecha == fecha && (ignorarId == null || c.Id != ignorarId))
            .Select(c => new { c.Hora, c.DuracionMinutos })
            .ToListAsync();
        var fin = hora.AddMinutes(duracion);
        return citas.Any(c => c.Hora < fin && c.Hora.AddMinutes(c.DuracionMinutos) > hora);
    }

    private static string? ValidarCita(Cita c, HorarioDia horario)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre) || c.Nombre.Trim().Length < 2) return "Nombre demasiado corto.";
        if (c.Nombre.Length > 80) return "Nombre demasiado largo.";

        var telNorm = Validaciones.NormalizarTelefonoEs(c.Telefono);
        if (telNorm is null) return "Teléfono no válido. Debe ser un número español (9 dígitos, empezando por 6, 7, 8 o 9).";
        c.Telefono = Validaciones.FormatearTelefono(telNorm);

        if (!string.IsNullOrWhiteSpace(c.Email) && !Validaciones.EsEmailValido(c.Email)) return "Email no válido.";

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        if (c.Fecha < hoy) return "No puedes reservar en el pasado.";
        if (c.Fecha > hoy.AddDays(365)) return "Fecha demasiado lejana.";

        if (c.Fecha == hoy && c.Hora < TimeOnly.FromDateTime(DateTime.Now)) return "Hora ya pasada.";

        if (!horario.Abierto) return "La peluquería no abre ese día.";

        if (c.Hora < horario.Apertura) return "Hora antes de la apertura.";
        var fin = c.Hora.AddMinutes(c.DuracionMinutos);
        if (fin > horario.Cierre.AddMinutes(1)) return "La cita termina después del cierre.";

        if (c.DuracionMinutos < 15 || c.DuracionMinutos > 360) return "Duración no válida.";

        return null;
    }

    private static string SanearTexto(string s, int maxLen)
    {
        var t = (s ?? string.Empty).Trim();
        if (t.Length > maxLen) t = t[..maxLen];
        return t;
    }
}
