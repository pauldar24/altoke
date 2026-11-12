using GRUPAL.Models;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MensajePrivado> MensajesPrivados => Set<MensajePrivado>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ObjetoPerdido> ObjetosPerdidos => Set<ObjetoPerdido>();
    public DbSet<ObjetoFavorito> Favoritos => Set<ObjetoFavorito>();
    public DbSet<MensajeContacto> MensajesContacto => Set<MensajeContacto>();
    public DbSet<SolicitudRecuperacion> SolicitudesRecuperacion => Set<SolicitudRecuperacion>();
    public DbSet<ArticuloAyuda> ArticulosAyuda => Set<ArticuloAyuda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MensajePrivado>(e =>
        {
            e.HasIndex(m => new { m.SolicitudRecuperacionId, m.AutorId, m.ClaveEnvio }).IsUnique();
            e.HasOne(m => m.SolicitudRecuperacion).WithMany().HasForeignKey(m => m.SolicitudRecuperacionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Autor).WithMany().HasForeignKey(m => m.AutorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Notificacion>(e =>
        {
            e.HasIndex(n => new { n.UsuarioId, n.InformanteId, n.ObjetoPerdidoId, n.Texto }).IsUnique();
            e.HasOne(n => n.Usuario).WithMany().HasForeignKey(n => n.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.ObjetoPerdido).WithMany().HasForeignKey(n => n.ObjetoPerdidoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Correo).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Contraseña).IsRequired();
            entity.HasIndex(u => u.Correo).IsUnique();
        });

        modelBuilder.Entity<ObjetoPerdido>(entity =>
        {
            entity.Property(o => o.Título).IsRequired().HasMaxLength(150);
            entity.Property(o => o.Categoría).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Descripcion).IsRequired();
            entity.Property(o => o.Ubicacion).IsRequired().HasMaxLength(200);
            entity.Property(o => o.Fecha).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(o => o.Usuario)
                  .WithMany(u => u.ObjetosPerdidos)
                  .HasForeignKey(o => o.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ObjetoFavorito>(entity =>
        {
            entity.HasIndex(f => new { f.UsuarioId, f.ObjetoPerdidoId }).IsUnique();
            entity.HasOne(f => f.Usuario).WithMany().HasForeignKey(f => f.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.ObjetoPerdido).WithMany().HasForeignKey(f => f.ObjetoPerdidoId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SolicitudRecuperacion>(entity =>
        {
            entity.HasIndex(s => new { s.SolicitanteId, s.ObjetoPerdidoId }).IsUnique();
            entity.HasOne(s => s.Solicitante).WithMany().HasForeignKey(s => s.SolicitanteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.ObjetoPerdido).WithMany().HasForeignKey(s => s.ObjetoPerdidoId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(s => s.Estado).HasConversion<int>();
        });
    }
}