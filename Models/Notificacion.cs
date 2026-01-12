using System.ComponentModel.DataAnnotations;

namespace TicketsAndretich.Web.Models;

public enum TipoNotificacion
{
    NuevoTicket,
    CambioEstado,
    Reasignacion,
    Comentario
}

public class Notificacion
{
    public int Id { get; set; }
    
    [Required]
    public string UsuarioId { get; set; } = null!;
    public ApplicationUser? Usuario { get; set; }
    
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Mensaje { get; set; } = null!;
    
    public TipoNotificacion Tipo { get; set; }
    
    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    
    public bool Leida { get; set; } = false;
}
