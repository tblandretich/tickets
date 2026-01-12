namespace TicketsAndretich.Web.Models;

public class EmailSettings
{
    public int Id { get; set; }
    
    /// <summary>
    /// Proveedor: "SMTP", "Gmail", "Microsoft"
    /// </summary>
    public string Provider { get; set; } = "SMTP";
    
    /// <summary>
    /// Email del remitente
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del remitente
    /// </summary>
    public string SenderName { get; set; } = "Tickets Andretich";
    
    // SMTP tradicional
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    
    // OAuth2 (Gmail/Microsoft)
    public string? OAuth2ClientId { get; set; }
    public string? OAuth2ClientSecret { get; set; }
    public string? OAuth2RefreshToken { get; set; }
    public string? OAuth2AccessToken { get; set; }
    public DateTimeOffset? OAuth2TokenExpiry { get; set; }
    
    /// <summary>
    /// Indica si la configuración está completa y probada
    /// </summary>
    public bool IsConfigured { get; set; } = false;
    
    /// <summary>
    /// Última fecha de prueba exitosa
    /// </summary>
    public DateTimeOffset? LastSuccessfulTest { get; set; }
    
    /// <summary>
    /// Fecha de creación/actualización
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum EmailProvider
{
    SMTP,
    Gmail,
    Microsoft
}
