using PeluCorte.Models;
using PeluCorte.Services;
using Xunit;

namespace PeluCorte.Tests;

public class DisponibilidadCalculatorTests
{
    private static readonly DateOnly Fecha = new(2026, 5, 1);
    private static readonly TimeOnly Apertura = new(9, 0);
    private static readonly TimeOnly Cierre = new(21, 0);

    [Fact]
    public void Sin_citas_ni_bloqueos_devuelve_todos_los_slots()
    {
        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            citas: [],
            bloqueos: [],
            fecha: Fecha, apertura: Apertura, cierre: Cierre, duracionMin: 30);

        Assert.Contains(new TimeOnly(9, 0), disponibles);
        Assert.Contains(new TimeOnly(20, 30), disponibles);
        Assert.DoesNotContain(new TimeOnly(21, 0), disponibles);
        Assert.Equal(24, disponibles.Count);
    }

    [Fact]
    public void Slot_ocupado_no_aparece()
    {
        var citas = new[] { new DisponibilidadCalculator.CitaResumen(new TimeOnly(10, 0), 30) };

        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            citas, [], Fecha, Apertura, Cierre, 30);

        Assert.DoesNotContain(new TimeOnly(10, 0), disponibles);
        Assert.Contains(new TimeOnly(9, 30), disponibles);
        Assert.Contains(new TimeOnly(10, 30), disponibles);
    }

    [Fact]
    public void Cita_de_60min_bloquea_dos_slots_de_30()
    {
        var citas = new[] { new DisponibilidadCalculator.CitaResumen(new TimeOnly(10, 0), 60) };

        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            citas, [], Fecha, Apertura, Cierre, 30);

        Assert.DoesNotContain(new TimeOnly(10, 0), disponibles);
        Assert.DoesNotContain(new TimeOnly(10, 30), disponibles);
        Assert.Contains(new TimeOnly(11, 0), disponibles);
    }

    [Fact]
    public void Solicitar_servicio_60min_excluye_huecos_que_solapen_con_cita_existente()
    {
        var citas = new[] { new DisponibilidadCalculator.CitaResumen(new TimeOnly(10, 30), 30) };

        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            citas, [], Fecha, Apertura, Cierre, duracionMin: 60);

        Assert.DoesNotContain(new TimeOnly(10, 0), disponibles);
        Assert.DoesNotContain(new TimeOnly(10, 30), disponibles);
        Assert.Contains(new TimeOnly(11, 0), disponibles);
        Assert.Contains(new TimeOnly(9, 30), disponibles);
    }

    [Fact]
    public void Servicio_no_cabe_antes_del_cierre()
    {
        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            citas: [], bloqueos: [], fecha: Fecha, apertura: Apertura, cierre: Cierre, duracionMin: 60);

        Assert.Contains(new TimeOnly(20, 0), disponibles);
        Assert.DoesNotContain(new TimeOnly(20, 30), disponibles);
    }

    [Fact]
    public void Bloqueo_completo_de_dia_excluye_todos_los_slots()
    {
        var bloqueo = new BloqueoHorario
        {
            Inicio = Fecha.ToDateTime(TimeOnly.MinValue),
            Fin = Fecha.AddDays(1).ToDateTime(TimeOnly.MinValue)
        };

        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            [], [bloqueo], Fecha, Apertura, Cierre, 30);

        Assert.Empty(disponibles);
    }

    [Fact]
    public void Bloqueo_de_hora_de_comida_excluye_solo_ese_rango()
    {
        var bloqueo = new BloqueoHorario
        {
            Inicio = Fecha.ToDateTime(new TimeOnly(13, 0)),
            Fin = Fecha.ToDateTime(new TimeOnly(14, 0))
        };

        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            [], [bloqueo], Fecha, Apertura, Cierre, 30);

        Assert.DoesNotContain(new TimeOnly(13, 0), disponibles);
        Assert.DoesNotContain(new TimeOnly(13, 30), disponibles);
        Assert.Contains(new TimeOnly(12, 30), disponibles);
        Assert.Contains(new TimeOnly(14, 0), disponibles);
    }

    [Theory]
    [InlineData(10, 0, 30, 10, 30, 30, false)]
    [InlineData(10, 0, 60, 10, 30, 30, true)]
    [InlineData(10, 0, 30, 10, 0, 30, true)]
    [InlineData(10, 0, 30, 9, 45, 30, true)]
    [InlineData(10, 0, 30, 9, 30, 30, false)]
    public void HaySolape_detecta_solapes_correctamente(
        int h1, int m1, int dur1, int h2, int m2, int dur2, bool esperado)
    {
        var actual = DisponibilidadCalculator.HaySolape(
            new TimeOnly(h1, m1), dur1,
            new TimeOnly(h2, m2), dur2);
        Assert.Equal(esperado, actual);
    }

    [Fact]
    public void Citas_concatenadas_dejan_huecos_alrededor()
    {
        var citas = new[]
        {
            new DisponibilidadCalculator.CitaResumen(new TimeOnly(10, 0), 30),
            new DisponibilidadCalculator.CitaResumen(new TimeOnly(10, 30), 30),
            new DisponibilidadCalculator.CitaResumen(new TimeOnly(11, 0), 30)
        };

        var disponibles = DisponibilidadCalculator.CalcularDisponibles(
            citas, [], Fecha, Apertura, Cierre, 30);

        Assert.Contains(new TimeOnly(9, 30), disponibles);
        Assert.DoesNotContain(new TimeOnly(10, 0), disponibles);
        Assert.DoesNotContain(new TimeOnly(10, 30), disponibles);
        Assert.DoesNotContain(new TimeOnly(11, 0), disponibles);
        Assert.Contains(new TimeOnly(11, 30), disponibles);
    }
}
