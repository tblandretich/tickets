using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using TicketsAndretich.Web.Data;
using TicketsAndretich.Web.Models;

namespace TicketsAndretich.Web.Services;

/// <summary>
/// Servicio de email configurable que soporta SMTP, Gmail OAuth2 y Microsoft OAuth2
/// </summary>
public class ConfigurableEmailSender : IEmailSender
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurableEmailSender> _logger;
    private readonly string _devEmailFolder;

    public ConfigurableEmailSender(
        IServiceProvider serviceProvider, 
        ILogger<ConfigurableEmailSender> logger,
        string devEmailFolder)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _devEmailFolder = devEmailFolder;
        Directory.CreateDirectory(_devEmailFolder);
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var settings = await db.EmailSettings.FirstOrDefaultAsync();
        
        if (settings == null || !settings.IsConfigured)
        {
            // Fallback a modo desarrollo - guardar en archivo
            await SaveToFile(email, subject, htmlMessage);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlMessage };

            using var client = new SmtpClient();

            switch (settings.Provider)
            {
                case "Gmail":
                    await SendViaGmailOAuth2(client, message, settings);
                    break;
                case "Microsoft":
                    await SendViaMicrosoftOAuth2(client, message, settings);
                    break;
                default: // SMTP
                    await SendViaSmtp(client, message, settings);
                    break;
            }

            _logger.LogInformation("Email enviado a {Email}: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {Email}", email);
            // Guardar en archivo como respaldo
            await SaveToFile(email, subject, htmlMessage);
        }
    }

    private async Task SendViaSmtp(SmtpClient client, MimeMessage message, EmailSettings settings)
    {
        var secureSocketOptions = settings.SmtpUseSsl 
            ? SecureSocketOptions.StartTls 
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureSocketOptions);
        
        if (!string.IsNullOrEmpty(settings.SmtpUsername))
        {
            await client.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword);
        }
        
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private async Task SendViaGmailOAuth2(SmtpClient client, MimeMessage message, EmailSettings settings)
    {
        var accessToken = await GetGmailAccessToken(settings);
        
        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        
        var oauth2 = new SaslMechanismOAuth2(settings.SenderEmail, accessToken);
        await client.AuthenticateAsync(oauth2);
        
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private async Task SendViaMicrosoftOAuth2(SmtpClient client, MimeMessage message, EmailSettings settings)
    {
        var accessToken = await GetMicrosoftAccessToken(settings);
        
        await client.ConnectAsync("smtp.office365.com", 587, SecureSocketOptions.StartTls);
        
        var oauth2 = new SaslMechanismOAuth2(settings.SenderEmail, accessToken);
        await client.AuthenticateAsync(oauth2);
        
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private async Task<string> GetGmailAccessToken(EmailSettings settings)
    {
        // Si el token no ha expirado, usarlo
        if (!string.IsNullOrEmpty(settings.OAuth2AccessToken) && 
            settings.OAuth2TokenExpiry.HasValue && 
            settings.OAuth2TokenExpiry.Value > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return settings.OAuth2AccessToken;
        }

        // Renovar token usando refresh token
        using var http = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = settings.OAuth2ClientId!,
            ["client_secret"] = settings.OAuth2ClientSecret!,
            ["refresh_token"] = settings.OAuth2RefreshToken!,
            ["grant_type"] = "refresh_token"
        });

        var response = await http.PostAsync("https://oauth2.googleapis.com/token", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<OAuth2TokenResponse>(json);

        // Guardar nuevo access token
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbSettings = await db.EmailSettings.FirstAsync();
        dbSettings.OAuth2AccessToken = tokenResponse!.access_token;
        dbSettings.OAuth2TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.expires_in);
        await db.SaveChangesAsync();

        return tokenResponse.access_token!;
    }

    private async Task<string> GetMicrosoftAccessToken(EmailSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.OAuth2AccessToken) && 
            settings.OAuth2TokenExpiry.HasValue && 
            settings.OAuth2TokenExpiry.Value > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return settings.OAuth2AccessToken;
        }

        using var http = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = settings.OAuth2ClientId!,
            ["client_secret"] = settings.OAuth2ClientSecret!,
            ["refresh_token"] = settings.OAuth2RefreshToken!,
            ["grant_type"] = "refresh_token",
            ["scope"] = "https://outlook.office365.com/SMTP.Send offline_access"
        });

        var response = await http.PostAsync(
            "https://login.microsoftonline.com/common/oauth2/v2.0/token", 
            content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<OAuth2TokenResponse>(json);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbSettings = await db.EmailSettings.FirstAsync();
        dbSettings.OAuth2AccessToken = tokenResponse!.access_token;
        dbSettings.OAuth2TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.expires_in);
        if (!string.IsNullOrEmpty(tokenResponse.refresh_token))
        {
            dbSettings.OAuth2RefreshToken = tokenResponse.refresh_token;
        }
        await db.SaveChangesAsync();

        return tokenResponse.access_token!;
    }

    private async Task SaveToFile(string email, string subject, string htmlMessage)
    {
        var file = Path.Combine(_devEmailFolder, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Sanitize(email)}.txt");
        var text = $"TO: {email}\nSUBJECT: {subject}\n\n{htmlMessage}";
        await File.WriteAllTextAsync(file, text);
        _logger.LogInformation("Email guardado en archivo (modo dev): {File}", file);
    }

    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value;
    }
}

internal class OAuth2TokenResponse
{
    public string? access_token { get; set; }
    public string? refresh_token { get; set; }
    public int expires_in { get; set; }
    public string? token_type { get; set; }
}
