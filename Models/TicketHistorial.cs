namespace TicketsAndretich.Web.Models;

/// <summary>
/// Registro de historial/trazabilidad de un ticket
/// </summary>
public class TicketHistorial
{
    public int Id { get; set; }
    
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    
    /// <summary>
    /// Tipo de evento: Creado, CambioEstado, Asignado, Reasignado, Comentario, Cancelado, Cerrado
    /// </summary>
    public TipoEventoTicket TipoEvento { get; set; }
    
    /// <summary>
    /// Descripción del evento
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;
    
    /// <summary>
    /// Detalle adicional (ej: justificación, comentario)
    /// </summary>
    public string? Detalle { get; set; }
    
    /// <summary>
    /// Estado anterior (si aplica)
    /// </summary>
    public EstadoTicket? EstadoAnterior { get; set; }
    
    /// <summary>
    /// Estado nuevo (si aplica)
    /// </summary>
    public EstadoTicket? EstadoNuevo { get; set; }
    
    /// <summary>
    /// Usuario que realizó la acción
    /// </summary>
    public string UsuarioId { get; set; } = default!;
    public ApplicationUser? Usuario { get; set; }
    
    /// <summary>
    /// Fecha y hora del evento
    /// </summary>
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}

public enum TipoEventoTicket
{
    Creado = 0,
    CambioEstado = 1,
    Asignado = 2,
    Reasignado = 3,
    Comentario = 4,
    Cancelado = 5,
    Cerrado = 6
}
