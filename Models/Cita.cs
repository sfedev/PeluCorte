namespace PeluCorte.Models;

public class Cita
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public int DuracionMinutos { get; set; } = 30;
    public Guid? ServicioId { get; set; }
    public Servicio? Servicio { get; set; }
    public Guid PeluqueroId { get; set; }
    public Peluquero? Peluquero { get; set; }
    public Guid PeluqueriaId { get; set; }
    public Peluqueria? Peluqueria { get; set; }
    public string CancelToken { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime? RecordatorioEnviadoEl { get; set; }
    public DateTime CreadaEl { get; set; } = DateTime.UtcNow;
}
