namespace TicketsAndretich.Web.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public Importancia Importancia { get; set; }
    public Categoria Categoria { get; set; }
    public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;

    public string CreadorUserId { get; set; } = default!;
    public ApplicationUser? Creator { get; set; }

    public int DepartamentoDestinoId { get; set; }
    public Department? DepartamentoDestino { get; set; }

    public bool EnviarATodoElDepartamento { get; set; } = true;

    public string? AsignadoUserId { get; set; }
    public ApplicationUser? AsignadoA { get; set; }

    // Fechas del ciclo de vida
    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaInicioTratamiento { get; set; }
    public DateTimeOffset? FechaCierre { get; set; }
    
    /// <summary>
    /// Tiempo estimado de resolución en horas (opcional)
    /// </summary>
    public int? TiempoEstimadoHoras { get; set; }
    
    /// <summary>
    /// Justificación cuando el ticket es cancelado (obligatorio para cancelar)
    /// </summary>
    public string? JustificacionCancelacion { get; set; }
    
    /// <summary>
    /// Justificación cuando el ticket es reasignado (obligatorio para reasignar)
    /// </summary>
    public string? JustificacionReasignacion { get; set; }
    
    /// <summary>
    /// Usuario que realizó la última acción de estado
    /// </summary>
    public string? UltimaAccionPorUserId { get; set; }
    public ApplicationUser? UltimaAccionPor { get; set; }

    public List<TicketAttachment> Adjuntos { get; set; } = new();
    
    /// <summary>
    /// Historial de eventos del ticket (línea de tiempo)
    /// </summary>
    public List<TicketHistorial> Historial { get; set; } = new();
}
