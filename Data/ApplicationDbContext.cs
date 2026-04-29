using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PeluCorte.Models;

namespace PeluCorte.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Peluqueria> Peluquerias => Set<Peluqueria>();
    public DbSet<Peluquero> Peluqueros => Set<Peluquero>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<BloqueoHorario> Bloqueos => Set<BloqueoHorario>();
    public DbSet<HorarioDia> Horarios => Set<HorarioDia>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ApplicationUser>().ToTable("Usuarios");
        b.Entity<IdentityRole>().ToTable("Roles");
        b.Entity<IdentityUserRole<string>>().ToTable("UsuarioRoles");
        b.Entity<IdentityUserClaim<string>>().ToTable("UsuarioClaims");
        b.Entity<IdentityRoleClaim<string>>().ToTable("RolClaims");
        b.Entity<IdentityUserLogin<string>>().ToTable("UsuarioLogins");
        b.Entity<IdentityUserToken<string>>().ToTable("UsuarioTokens");

        b.Entity<Peluqueria>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(120);
            e.Property(p => p.Slug).IsRequired().HasMaxLength(140);
            e.Property(p => p.Direccion).IsRequired().HasMaxLength(255);
            e.Property(p => p.Telefono).HasMaxLength(30);
            e.Property(p => p.EmailContacto).HasMaxLength(180);
            e.Property(p => p.MotivoRechazo).HasMaxLength(500);
            e.Property(p => p.MetodoVerificacion).HasMaxLength(20);
            e.Property(p => p.DiasAbiertosBitmask).HasDefaultValue(127);
        });

        b.Entity<Peluquero>(e =>
        {
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(80);
            e.Property(p => p.Color).IsRequired().HasMaxLength(20);
            e.HasOne(p => p.Peluqueria)
                .WithMany(p => p.Peluqueros)
                .HasForeignKey(p => p.PeluqueriaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Peluquero>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => p.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
        });

        b.Entity<Cita>(e =>
        {
            e.Property(c => c.Nombre).IsRequired().HasMaxLength(80);
            e.Property(c => c.Telefono).IsRequired().HasMaxLength(30);
            e.Property(c => c.Email).HasMaxLength(180);
            e.Property(c => c.CancelToken).IsRequired().HasMaxLength(64);
            e.HasOne(c => c.Peluquero)
                .WithMany()
                .HasForeignKey(c => c.PeluqueroId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Peluqueria)
                .WithMany(p => p.Citas)
                .HasForeignKey(c => c.PeluqueriaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Servicio)
                .WithMany()
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(c => new { c.PeluqueriaId, c.Fecha });
            e.HasIndex(c => new { c.PeluqueroId, c.Fecha, c.Hora });
            e.HasIndex(c => c.CancelToken).IsUnique();
        });

        b.Entity<Servicio>(e =>
        {
            e.Property(s => s.Nombre).IsRequired().HasMaxLength(80);
            e.Property(s => s.Precio).HasPrecision(10, 2);
            e.HasOne(s => s.Peluqueria)
                .WithMany()
                .HasForeignKey(s => s.PeluqueriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<HorarioDia>(e =>
        {
            e.HasOne(h => h.Peluqueria)
                .WithMany(p => p.Horarios)
                .HasForeignKey(h => h.PeluqueriaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(h => new { h.PeluqueriaId, h.Dia }).IsUnique();
        });

        b.Entity<BloqueoHorario>(e =>
        {
            e.Property(x => x.Motivo).HasMaxLength(120);
            e.Property(x => x.Inicio).HasColumnType("timestamp without time zone");
            e.Property(x => x.Fin).HasColumnType("timestamp without time zone");
            e.HasOne(x => x.Peluquero)
                .WithMany()
                .HasForeignKey(x => x.PeluqueroId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PeluqueroId, x.Inicio });
        });
    }
}
