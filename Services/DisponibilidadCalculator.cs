using PeluCorte.Models;

namespace PeluCorte.Services;

public static class DisponibilidadCalculator
{
    public record CitaResumen(TimeOnly Hora, int DuracionMinutos);

    public static HashSet<TimeOnly> CalcularDisponibles(
        IEnumerable<CitaResumen> citas,
        IEnumerable<BloqueoHorario> bloqueos,
        DateOnly fecha,
        TimeOnly apertura,
        TimeOnly cierre,
        int duracionMin,
        int pasoMin = 30)
    {
        var disponibles = new HashSet<TimeOnly>();
        var citasList = citas.ToList();
        var bloqueosList = bloqueos.ToList();

        var actual = apertura;
        while (actual.AddMinutes(duracionMin) <= cierre.AddMinutes(1))
        {
            var inicio = actual;
            var fin = actual.AddMinutes(duracionMin);

            var solapaCita = citasList.Any(c =>
                c.Hora < fin && c.Hora.AddMinutes(c.DuracionMinutos) > inicio);

            var solapaBloqueo = bloqueosList.Any(b => SolapaConBloqueo(b, fecha, inicio, duracionMin));

            if (!solapaCita && !solapaBloqueo)
                disponibles.Add(inicio);

            actual = actual.AddMinutes(pasoMin);
        }
        return disponibles;
    }

    public static bool HaySolape(TimeOnly hora1, int dur1, TimeOnly hora2, int dur2)
    {
        return hora1 < hora2.AddMinutes(dur2) && hora1.AddMinutes(dur1) > hora2;
    }

    public static bool SolapaConBloqueo(BloqueoHorario b, DateOnly fecha, TimeOnly hora, int duracionMin)
    {
        var inicio = fecha.ToDateTime(hora);
        var fin = inicio.AddMinutes(duracionMin);
        return b.Inicio < fin && b.Fin > inicio;
    }
}
