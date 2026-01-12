using Microsoft.AspNetCore.Identity.UI.Services;

namespace TicketsAndretich.Web.Services;

public class DevEmailSender : IEmailSender
{
    private readonly string _folder;
    public DevEmailSender(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var file = Path.Combine(_folder, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Sanitize(email)}.txt");
        var text = $"TO: {email}\nSUBJECT: {subject}\n\n{htmlMessage}";
        File.WriteAllText(file, text);
        return Task.CompletedTask;
    }

    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value;
    }
}
