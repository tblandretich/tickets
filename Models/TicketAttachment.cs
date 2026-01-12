namespace TicketsAndretich.Web.Models;

public class TicketAttachment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = default!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset FechaSubida { get; set; } = DateTimeOffset.UtcNow;
    public string UploadedByUserId { get; set; } = string.Empty;
}
