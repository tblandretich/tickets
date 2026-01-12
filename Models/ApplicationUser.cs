using Microsoft.AspNetCore.Identity;

namespace TicketsAndretich.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ThemePreferencesJson { get; set; }
}
