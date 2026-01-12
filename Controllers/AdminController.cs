using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketsAndretich.Web.Data;
using TicketsAndretich.Web.Models;

namespace TicketsAndretich.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // --------- Departamentos ---------
    public async Task<IActionResult> Departamentos()
    {
        var list = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearDepartamento(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.Departments.Add(new Department { Name = name.Trim() });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Departamentos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDepartamento(int id)
    {
        var d = await _db.Departments.FindAsync(id);
        if (d != null)
        {
            _db.Departments.Remove(d);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Departamentos));
    }

    // --------- Usuarios ---------
    public async Task<IActionResult> Usuarios()
    {
        var users = await _db.Users.Include(u => u.Department).OrderBy(u => u.Email).ToListAsync();
        ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return View(users);
    }

    // GET: Crear usuario
    public async Task<IActionResult> CrearUsuario()
    {
        ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return View(new CrearUsuarioViewModel());
    }

    // POST: Crear usuario
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                DepartmentId = model.DepartmentId,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }
                TempData["Success"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Usuarios));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return View(model);
    }

    // GET: Editar usuario
    public async Task<IActionResult> EditarUsuario(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var model = new EditarUsuarioViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DepartmentId = user.DepartmentId,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault()
        };

        ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return View(model);
    }

    // POST: Editar usuario
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(EditarUsuarioViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _db.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email;
            user.NormalizedEmail = model.Email.ToUpper();
            user.NormalizedUserName = model.Email.ToUpper();
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DepartmentId = model.DepartmentId;
            user.IsActive = model.IsActive;

            // Actualizar contraseña si se proporcionó
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
                    ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
                    return View(model);
                }
            }

            await _db.SaveChangesAsync();

            // Actualizar rol
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Usuarios));
        }

        ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return View(model);
    }

    // POST: Eliminar usuario
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null)
        {
            // No permitir eliminarse a sí mismo
            if (user.Email == User.Identity?.Name)
            {
                TempData["Error"] = "No puedes eliminarte a ti mismo.";
                return RedirectToAction(nameof(Usuarios));
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "Usuario eliminado correctamente.";
        }
        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserDepartment(string userId, int? departmentId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.DepartmentId = departmentId;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRole(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToAction(nameof(Usuarios));
        if (!await _roleManager.RoleExistsAsync(roleName)) return RedirectToAction(nameof(Usuarios));

        if (await _userManager.IsInRoleAsync(user, roleName))
            await _userManager.RemoveFromRoleAsync(user, roleName);
        else
            await _userManager.AddToRoleAsync(user, roleName);

        return RedirectToAction(nameof(Usuarios));
    }

    // --------- Configuración de Email ---------
    public async Task<IActionResult> ConfiguracionEmail()
    {
        var settings = await _db.EmailSettings.FirstOrDefaultAsync() ?? new EmailSettings();
        return View(new EmailSettingsViewModel
        {
            Provider = settings.Provider,
            SenderEmail = settings.SenderEmail,
            SenderName = settings.SenderName,
            SmtpHost = settings.SmtpHost,
            SmtpPort = settings.SmtpPort,
            SmtpUsername = settings.SmtpUsername,
            SmtpPassword = settings.SmtpPassword,
            SmtpUseSsl = settings.SmtpUseSsl,
            OAuth2ClientId = settings.OAuth2ClientId,
            OAuth2ClientSecret = settings.OAuth2ClientSecret,
            IsConfigured = settings.IsConfigured,
            HasRefreshToken = !string.IsNullOrEmpty(settings.OAuth2RefreshToken),
            LastSuccessfulTest = settings.LastSuccessfulTest
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarConfiguracionEmail(EmailSettingsViewModel model)
    {
        var settings = await _db.EmailSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new EmailSettings();
            _db.EmailSettings.Add(settings);
        }

        settings.Provider = model.Provider;
        settings.SenderEmail = model.SenderEmail;
        settings.SenderName = model.SenderName;
        settings.SmtpHost = model.SmtpHost;
        settings.SmtpPort = model.SmtpPort;
        settings.SmtpUsername = model.SmtpUsername;
        settings.SmtpUseSsl = model.SmtpUseSsl;
        settings.OAuth2ClientId = model.OAuth2ClientId;

        // Solo actualizar password si se proporcionó uno nuevo
        if (!string.IsNullOrEmpty(model.SmtpPassword))
        {
            settings.SmtpPassword = model.SmtpPassword;
        }
        if (!string.IsNullOrEmpty(model.OAuth2ClientSecret))
        {
            settings.OAuth2ClientSecret = model.OAuth2ClientSecret;
        }

        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Configuración guardada correctamente.";
        return RedirectToAction(nameof(ConfiguracionEmail));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProbarEmail()
    {
        var settings = await _db.EmailSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            TempData["Error"] = "No hay configuración de email.";
            return RedirectToAction(nameof(ConfiguracionEmail));
        }

        try
        {
            var emailSender = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            await emailSender.SendEmailAsync(
                User.Identity?.Name ?? settings.SenderEmail,
                "Prueba de configuración de email - Tickets Andretich",
                "<h1>Email de prueba</h1><p>Si recibes este mensaje, la configuración de email funciona correctamente.</p>"
            );

            settings.IsConfigured = true;
            settings.LastSuccessfulTest = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Email de prueba enviado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al enviar email: {ex.Message}";
        }

        return RedirectToAction(nameof(ConfiguracionEmail));
    }

    public async Task<IActionResult> OAuth2Authorize(string provider)
    {
        var settings = await _db.EmailSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.OAuth2ClientId))
        {
            TempData["Error"] = "Primero guarda la configuración con el Client ID antes de autorizar.";
            return RedirectToAction(nameof(ConfiguracionEmail));
        }

        var redirectUri = Url.Action("OAuth2Callback", "Admin", null, Request.Scheme);
        string authUrl;

        if (provider == "Gmail")
        {
            authUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(settings.OAuth2ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
                "&response_type=code" +
                "&scope=https://mail.google.com/" +
                "&access_type=offline" +
                "&prompt=consent";
        }
        else // Microsoft
        {
            authUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize" +
                $"?client_id={Uri.EscapeDataString(settings.OAuth2ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
                "&response_type=code" +
                "&scope=https://outlook.office365.com/SMTP.Send offline_access";
        }

        TempData["OAuth2Provider"] = provider;
        return Redirect(authUrl);
    }

    public async Task<IActionResult> OAuth2Callback(string code, string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            TempData["Error"] = $"Error de autorización: {error}";
            return RedirectToAction(nameof(ConfiguracionEmail));
        }

        var provider = TempData["OAuth2Provider"]?.ToString();
        var settings = await _db.EmailSettings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            TempData["Error"] = "Guarda la configuración antes de autorizar.";
            return RedirectToAction(nameof(ConfiguracionEmail));
        }

        var redirectUri = Url.Action("OAuth2Callback", "Admin", null, Request.Scheme);

        try
        {
            using var http = new HttpClient();
            FormUrlEncodedContent content;
            string tokenUrl;

            if (provider == "Gmail")
            {
                tokenUrl = "https://oauth2.googleapis.com/token";
                content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = settings.OAuth2ClientId!,
                    ["client_secret"] = settings.OAuth2ClientSecret!,
                    ["redirect_uri"] = redirectUri!,
                    ["grant_type"] = "authorization_code"
                });
            }
            else
            {
                tokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
                content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = settings.OAuth2ClientId!,
                    ["client_secret"] = settings.OAuth2ClientSecret!,
                    ["redirect_uri"] = redirectUri!,
                    ["grant_type"] = "authorization_code",
                    ["scope"] = "https://outlook.office365.com/SMTP.Send offline_access"
                });
            }

            var response = await http.PostAsync(tokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = $"Error al obtener token: {json}";
                return RedirectToAction(nameof(ConfiguracionEmail));
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<OAuth2TokenResult>(json);
            
            settings.OAuth2AccessToken = tokenData!.access_token;
            settings.OAuth2RefreshToken = tokenData.refresh_token;
            settings.OAuth2TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenData.expires_in);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Autorización OAuth2 completada correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(ConfiguracionEmail));
    }
}

// ViewModels para usuarios
public class CrearUsuarioViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? Role { get; set; }
}

public class EditarUsuarioViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? NewPassword { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public string? Role { get; set; }
}

// ViewModels para configuración de email
public class EmailSettingsViewModel
{
    public string Provider { get; set; } = "SMTP";
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Tickets Andretich";
    
    // SMTP
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    
    // OAuth2
    public string? OAuth2ClientId { get; set; }
    public string? OAuth2ClientSecret { get; set; }
    
    // Estado
    public bool IsConfigured { get; set; }
    public bool HasRefreshToken { get; set; }
    public DateTimeOffset? LastSuccessfulTest { get; set; }
}

public class OAuth2TokenResult
{
    public string? access_token { get; set; }
    public string? refresh_token { get; set; }
    public int expires_in { get; set; }
}
