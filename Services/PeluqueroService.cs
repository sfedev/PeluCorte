using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeluCorte.Data;
using PeluCorte.Models;

namespace PeluCorte.Services;

public class PeluqueroService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly string[] ColoresPorDefecto =
    {
        "#6c4cd9", "#ff6fae", "#2bb673", "#ff9f43", "#26b9d6", "#e25563", "#7d5fff", "#fdcb6e"
    };

    public PeluqueroService(IDbContextFactory<ApplicationDbContext> factory, UserManager<ApplicationUser> userManager)
    {
        _factory = factory;
        _userManager = userManager;
    }

    public async Task<List<Peluquero>> ObtenerPorPeluqueriaAsync(Guid peluqueriaId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluqueros
            .Include(p => p.User)
            .Where(p => p.PeluqueriaId == peluqueriaId)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Peluquero?> ObtenerPorIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluqueros.FindAsync(id);
    }

    public async Task<Peluquero?> ObtenerPorUserIdAsync(string userId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Peluqueros
            .Include(p => p.Peluqueria)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<(bool Ok, string? Error)> CrearAsync(Guid peluqueriaId, string nombre, string? email, string? password)
    {
        nombre = nombre.Trim();
        if (string.IsNullOrEmpty(nombre)) return (false, "Indica un nombre.");

        var conLogin = !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password);
        if (conLogin)
        {
            if (string.IsNullOrWhiteSpace(email)) return (false, "Falta el email.");
            if (string.IsNullOrWhiteSpace(password)) return (false, "Falta la contraseña.");
            var existente = await _userManager.FindByEmailAsync(email.Trim());
            if (existente is not null) return (false, "Ya existe una cuenta con ese email.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var existentes = await db.Peluqueros.CountAsync(p => p.PeluqueriaId == peluqueriaId);
        var color = ColoresPorDefecto[existentes % ColoresPorDefecto.Length];

        string? userId = null;
        if (conLogin)
        {
            var user = new ApplicationUser
            {
                UserName = email!.Trim(),
                Email = email.Trim(),
                NombreCompleto = nombre,
                PeluqueriaId = peluqueriaId,
                EmailConfirmed = true
            };
            var res = await _userManager.CreateAsync(user, password!);
            if (!res.Succeeded)
                return (false, string.Join(" ", res.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Empleado);
            userId = user.Id;
        }

        db.Peluqueros.Add(new Peluquero
        {
            Nombre = nombre,
            Color = color,
            PeluqueriaId = peluqueriaId,
            UserId = userId
        });
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task ActualizarAsync(Guid id, string nombre, string color)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Peluqueros.FindAsync(id);
        if (p is null) return;
        p.Nombre = nombre.Trim();
        p.Color = color;
        await db.SaveChangesAsync();
    }

    public async Task<(bool Ok, string? Error)> CrearComoDuenoAsync(Guid peluqueriaId, string userId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return (false, "Indica un nombre.");

        await using var db = await _factory.CreateDbContextAsync();
        if (await db.Peluqueros.AnyAsync(p => p.UserId == userId))
            return (false, "Ya estás añadido como peluquero.");

        var existentes = await db.Peluqueros.CountAsync(p => p.PeluqueriaId == peluqueriaId);
        var color = ColoresPorDefecto[existentes % ColoresPorDefecto.Length];

        db.Peluqueros.Add(new Peluquero
        {
            Nombre = nombre.Trim(),
            Color = color,
            PeluqueriaId = peluqueriaId,
            UserId = userId
        });
        await db.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null && !await _userManager.IsInRoleAsync(user, Roles.Empleado))
            await _userManager.AddToRoleAsync(user, Roles.Empleado);

        return (true, null);
    }

    public async Task EliminarAsync(Guid id, Guid? peluqueriaIdEsperada = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Peluqueros.FindAsync(id);
        if (p is null) return;
        // Comprobación de propiedad: si pasan peluqueriaIdEsperada, el peluquero debe pertenecer a esa peluquería.
        if (peluqueriaIdEsperada is not null && p.PeluqueriaId != peluqueriaIdEsperada) return;

        var userId = p.UserId;
        db.Peluqueros.Remove(p);
        await db.SaveChangesAsync();

        if (string.IsNullOrEmpty(userId)) return;

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return;

        // Si el usuario es también Dueño, no borramos la cuenta — solo le quitamos el rol de Empleado.
        if (await _userManager.IsInRoleAsync(user, Roles.Dueno))
        {
            if (await _userManager.IsInRoleAsync(user, Roles.Empleado))
                await _userManager.RemoveFromRoleAsync(user, Roles.Empleado);
            return;
        }

        await _userManager.DeleteAsync(user);
    }
}
