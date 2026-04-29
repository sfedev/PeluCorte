namespace PeluCorte.Models;

public class Peluquero
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Color { get; set; } = "#6c4cd9";
    public Guid PeluqueriaId { get; set; }
    public Peluqueria? Peluqueria { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
