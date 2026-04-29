using Microsoft.AspNetCore.Identity;

namespace PeluCorte.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public Guid? PeluqueriaId { get; set; }
    public Peluqueria? Peluqueria { get; set; }
}
