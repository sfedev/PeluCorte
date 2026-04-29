namespace PeluCorte.Models;

public class Peluqueria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? GoogleMapsUrl { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string EmailContacto { get; set; } = string.Empty;
    public TimeOnly HoraApertura { get; set; } = new TimeOnly(9, 0);
    public TimeOnly HoraCierre { get; set; } = new TimeOnly(21, 0);
    /// <summary>Bitmask: bit 0 = lunes, bit 1 = martes, ..., bit 6 = domingo. 127 = todos abiertos.</summary>
    public int DiasAbiertosBitmask { get; set; } = 127;
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    public DateTime CreadaEl { get; set; } = DateTime.UtcNow;
    public string? MotivoRechazo { get; set; }
    /// <summary>Cómo se verificó: "OSM", "Manual" o null si pendiente.</summary>
    public string? MetodoVerificacion { get; set; }

    public List<Peluquero> Peluqueros { get; set; } = new();
    public List<Cita> Citas { get; set; } = new();
    public List<HorarioDia> Horarios { get; set; } = new();

    /// <summary>Horario para un día concreto. Si no existe registro,
    /// devuelve uno por defecto cerrado.</summary>
    public HorarioDia HorarioDe(DateOnly fecha)
    {
        var h = Horarios.FirstOrDefault(x => x.Dia == fecha.DayOfWeek);
        if (h is not null) return h;
        return new HorarioDia
        {
            Dia = fecha.DayOfWeek,
            Abierto = false,
            Apertura = new TimeOnly(9, 0),
            Cierre = new TimeOnly(21, 0)
        };
    }

    public bool EstaAbiertoEn(DateOnly fecha) => HorarioDe(fecha).Abierto;
}
