using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicketsAndretich.Web.Models;

namespace TicketsAndretich.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Department>().HasIndex(d => d.Name).IsUnique();

        builder.Entity<Ticket>()
            .HasOne(t => t.Creator)
            .WithMany()
            .HasForeignKey(t => t.CreadorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.AsignadoA)
            .WithMany()
            .HasForeignKey(t => t.AsignadoUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Ticket>()
            .Property(t => t.Asunto).HasMaxLength(200);

        builder.Entity<TicketAttachment>()
            .HasOne(a => a.Ticket)
            .WithMany(t => t.Adjuntos)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TicketHistorial>()
            .HasOne(h => h.Ticket)
            .WithMany(t => t.Historial)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TicketHistorial>()
            .HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notificacion>()
            .HasOne(n => n.Usuario)
            .WithMany()
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notificacion>()
            .HasOne(n => n.Ticket)
            .WithMany()
            .HasForeignKey(n => n.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
