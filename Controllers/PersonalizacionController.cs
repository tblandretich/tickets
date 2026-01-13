using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TicketsAndretich.Web.Models;

namespace TicketsAndretich.Web.Controllers;

[Authorize]
public class PersonalizacionController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PersonalizacionController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var prefs = new ThemePreferences();
        
        if (!string.IsNullOrEmpty(user?.ThemePreferencesJson))
        {
            try
            {
                prefs = JsonSerializer.Deserialize<ThemePreferences>(user.ThemePreferencesJson) ?? new ThemePreferences();
            }
            catch { }
        }

        return View(prefs);
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(ThemePreferences prefs)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        user.ThemePreferencesJson = JsonSerializer.Serialize(prefs);
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Preferencias guardadas correctamente";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Restablecer()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        user.ThemePreferencesJson = null;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Preferencias restablecidas a valores por defecto";
        return RedirectToAction(nameof(Index));
    }
}

public class ThemePreferences
{
    public string ColorPrimario { get; set; } = "#C62828";
    public string ColorSecundario { get; set; } = "#1565C0";
    public string ColorFondo { get; set; } = "#f5f5f5";
    public string ColorNavbar { get; set; } = "#ffffff";
    public string Tema { get; set; } = "claro"; // claro, oscuro
    public int BorderRadius { get; set; } = 8;
}
