namespace PeluCorte.Models;

public class BloqueoHorario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PeluqueroId { get; set; }
    public Peluquero? Peluquero { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string? Motivo { get; set; }
}
