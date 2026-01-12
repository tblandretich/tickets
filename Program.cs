using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TicketsAndretich.Web.Data;
using TicketsAndretich.Web.Models;
using TicketsAndretich.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=TicketsAndretich;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

// EF Core + Identity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Email sender configurable (SMTP, Gmail OAuth2, Microsoft OAuth2)
builder.Services.AddSingleton<IEmailSender>(sp =>
    new ConfigurableEmailSender(
        sp,
        sp.GetRequiredService<ILogger<ConfigurableEmailSender>>(),
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "emails")));

// Local file storage for attachments
builder.Services.AddSingleton<IFileStorage>(sp =>
    new LocalFileStorage(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads")));

var app = builder.Build();

// DB migrate + seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var ctx = services.GetRequiredService<AppDbContext>();
    await ctx.Database.MigrateAsync();
    await SeedData.InitializeAsync(services);
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
