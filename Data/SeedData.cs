using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketsAndretich.Web.Models;

namespace TicketsAndretich.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        string[] roles = new[] { "Admin", "Líder", "Coordinador", "Usuario" };
        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
                await roleManager.CreateAsync(new IdentityRole(r));
        }

        // Seed departamentos
        var deptos = new[] { "Administracion", "Comercial", "Finanzas", "Facturacion", "Logistica", "Ecommerce", "Compras", "Gerencia", "Depositos", "Tesoreria" };
        foreach (var d in deptos)
        {
            if (!await ctx.Departments.AnyAsync(x => x.Name == d))
                ctx.Departments.Add(new Department { Name = d });
        }
        await ctx.SaveChangesAsync();

        // Admin inicial
        var adminEmail = "admin@andretich.local";
        var admin = await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Andretich",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, "Admin123$");
            if (!result.Succeeded)
                throw new Exception(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // Ensure admin role
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
