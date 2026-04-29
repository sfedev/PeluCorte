namespace PeluCorte.Models;

public class HorarioDia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PeluqueriaId { get; set; }
    public Peluqueria? Peluqueria { get; set; }
    public DayOfWeek Dia { get; set; }
    public bool Abierto { get; set; } = true;
    public TimeOnly Apertura { get; set; } = new(9, 0);
    public TimeOnly Cierre { get; set; } = new(21, 0);
}
