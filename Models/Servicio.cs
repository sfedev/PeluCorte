namespace PeluCorte.Models;

public class Servicio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PeluqueriaId { get; set; }
    public Peluqueria? Peluqueria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; } = 30;
    public decimal? Precio { get; set; }
    public bool Activo { get; set; } = true;
}
