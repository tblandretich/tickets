namespace TicketsAndretich.Web.Services;

/// <summary>
/// Helper para generar plantillas de email HTML
/// </summary>
public static class EmailTemplates
{
    private const string Signature = @"
        <hr style='border:none;border-top:1px solid #ddd;margin:30px 0 20px;'>
        <p style='color:#666;font-size:12px;'>
            Este es un mensaje automático del <strong>Sistema de Tickets Andretich</strong>.<br>
            Por favor no responda a este correo.
        </p>";

    /// <summary>
    /// Genera email HTML para nuevo ticket
    /// </summary>
    public static string NuevoTicket(int ticketId, string asunto, string importancia, string categoria, string descripcion, string creadoPor, string link)
    {
        return $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
            <div style='background:#C62828;color:white;padding:20px;border-radius:8px 8px 0 0;'>
                <h2 style='margin:0;'>🎫 Nuevo Ticket #{ticketId}</h2>
            </div>
            <div style='background:#f9f9f9;padding:20px;border:1px solid #ddd;border-radius:0 0 8px 8px;'>
                <h3 style='color:#333;margin-top:0;'>{asunto}</h3>
                <table style='width:100%;border-collapse:collapse;'>
                    <tr>
                        <td style='padding:8px 0;color:#666;width:140px;'><strong>Creado por:</strong></td>
                        <td style='padding:8px 0;'>{creadoPor}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;color:#666;'><strong>Importancia:</strong></td>
                        <td style='padding:8px 0;'><span style='background:{GetImportanciaColor(importancia)};color:white;padding:3px 10px;border-radius:4px;font-size:12px;'>{importancia}</span></td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;color:#666;'><strong>Categoría:</strong></td>
                        <td style='padding:8px 0;'>{categoria}</td>
                    </tr>
                </table>
                <div style='background:white;padding:15px;border-radius:6px;margin:15px 0;border-left:4px solid #C62828;'>
                    <p style='margin:0;color:#333;'>{descripcion}</p>
                </div>
                <a href='{link}' style='display:inline-block;background:#C62828;color:white;text-decoration:none;padding:12px 24px;border-radius:6px;font-weight:bold;margin-top:10px;'>
                    Ver Ticket en el Portal
                </a>
                {Signature}
            </div>
        </div>";
    }

    /// <summary>
    /// Genera email HTML para cambio de estado
    /// </summary>
    public static string CambioEstado(int ticketId, string asunto, string estadoAnterior, string estadoNuevo, string? comentario, string realizadoPor, string link)
    {
        return $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
            <div style='background:#1976D2;color:white;padding:20px;border-radius:8px 8px 0 0;'>
                <h2 style='margin:0;'>🔄 Actualización de Ticket #{ticketId}</h2>
            </div>
            <div style='background:#f9f9f9;padding:20px;border:1px solid #ddd;border-radius:0 0 8px 8px;'>
                <h3 style='color:#333;margin-top:0;'>{asunto}</h3>
                
                <table style='width:100%;border-collapse:collapse;margin-bottom:15px;'>
                    <tr>
                        <td style='padding:8px 0;color:#666;width:140px;'><strong>Acción realizada por:</strong></td>
                        <td style='padding:8px 0;font-weight:bold;'>{realizadoPor}</td>
                    </tr>
                </table>
                
                <p style='color:#666;margin-bottom:10px;'>El estado del ticket ha cambiado:</p>
                <div style='margin:15px 0;'>
                    <span style='background:#9E9E9E;color:white;padding:6px 12px;border-radius:4px;'>{estadoAnterior}</span>
                    <span style='font-size:20px;margin:0 10px;'>→</span>
                    <span style='background:{GetEstadoColor(estadoNuevo)};color:white;padding:6px 12px;border-radius:4px;font-weight:bold;'>{estadoNuevo}</span>
                </div>
                {(string.IsNullOrEmpty(comentario) ? "" : $@"
                <div style='background:white;padding:15px;border-radius:6px;margin:15px 0;border-left:4px solid #1976D2;'>
                    <p style='margin:0;color:#666;font-size:12px;'>Comentario/Justificación:</p>
                    <p style='margin:5px 0 0;color:#333;'>{comentario}</p>
                </div>")}
                <a href='{link}' style='display:inline-block;background:#1976D2;color:white;text-decoration:none;padding:12px 24px;border-radius:6px;font-weight:bold;margin-top:10px;'>
                    Ver Ticket en el Portal
                </a>
                {Signature}
            </div>
        </div>";
    }

    /// <summary>
    /// Genera email HTML para reasignación
    /// </summary>
    public static string Reasignacion(int ticketId, string asunto, string asignadoA, string justificacion, string realizadoPor, string link)
    {
        return $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
            <div style='background:#FF9800;color:white;padding:20px;border-radius:8px 8px 0 0;'>
                <h2 style='margin:0;'>👤 Ticket Reasignado #{ticketId}</h2>
            </div>
            <div style='background:#f9f9f9;padding:20px;border:1px solid #ddd;border-radius:0 0 8px 8px;'>
                <h3 style='color:#333;margin-top:0;'>{asunto}</h3>
                
                <table style='width:100%;border-collapse:collapse;'>
                    <tr>
                        <td style='padding:8px 0;color:#666;width:140px;'><strong>Reasignado por:</strong></td>
                        <td style='padding:8px 0;font-weight:bold;'>{realizadoPor}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;color:#666;'><strong>Nuevo asignado:</strong></td>
                        <td style='padding:8px 0;'>{asignadoA}</td>
                    </tr>
                </table>
                
                <div style='background:white;padding:15px;border-radius:6px;margin:15px 0;border-left:4px solid #FF9800;'>
                    <p style='margin:0;color:#666;font-size:12px;'>Justificación:</p>
                    <p style='margin:5px 0 0;color:#333;'>{justificacion}</p>
                </div>
                
                <a href='{link}' style='display:inline-block;background:#FF9800;color:white;text-decoration:none;padding:12px 24px;border-radius:6px;font-weight:bold;margin-top:10px;'>
                    Ver Ticket en el Portal
                </a>
                {Signature}
            </div>
        </div>";
    }

    private static string GetImportanciaColor(string importancia) => importancia switch
    {
        "Alta" => "#B71C1C",
        "Media" => "#E65100",
        _ => "#1B5E20"
    };

    private static string GetEstadoColor(string estado) => estado switch
    {
        "Abierto" => "#1B5E20",
        "EnProgreso" or "En Progreso" => "#0D47A1",
        "Pendiente" => "#E65100",
        "Cerrado" or "Resuelto" => "#424242",
        "Cancelado" => "#B71C1C",
        _ => "#9E9E9E"
    };
}
