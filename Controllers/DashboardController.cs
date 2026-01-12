using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketsAndretich.Web.Data;
using TicketsAndretich.Web.Models;

namespace TicketsAndretich.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // Contadores por estado
        var nuevos = await _db.Tickets.CountAsync(t => t.Estado == EstadoTicket.Abierto);
        var enProceso = await _db.Tickets.CountAsync(t => t.Estado == EstadoTicket.EnProgreso);
        var enEspera = await _db.Tickets.CountAsync(t => t.Estado == EstadoTicket.Pendiente);
        var cerrados = await _db.Tickets.CountAsync(t => t.Estado == EstadoTicket.Cerrado);
        var criticos = await _db.Tickets.CountAsync(t => t.Importancia == Importancia.Alta && t.Estado != EstadoTicket.Cerrado);

        // Tiempo promedio de cierre en horas
        var tiempos = await _db.Tickets
            .Where(t => t.Estado == EstadoTicket.Cerrado && t.FechaCierre != null)
            .Select(t => EF.Functions.DateDiffMinute(t.FechaCreacion, t.FechaCierre!.Value))
            .ToListAsync();

        double promedioHoras = tiempos.Count == 0 ? 0 : tiempos.Average() / 60.0;

        // Datos para gráfico de barras (por prioridad)
        var baja = await _db.Tickets.CountAsync(t => t.Importancia == Importancia.Baja);
        var media = await _db.Tickets.CountAsync(t => t.Importancia == Importancia.Media);
        var alta = await _db.Tickets.CountAsync(t => t.Importancia == Importancia.Alta);

        // Datos para gráfico de tendencia (últimos 12 días)
        var hace12Dias = DateTimeOffset.UtcNow.AddDays(-12);
        var ticketsRecientes = await _db.Tickets
            .Where(t => t.FechaCreacion >= hace12Dias)
            .ToListAsync();

        var tendenciaCreados = new int[12];
        var tendenciaCerrados = new int[12];
        var labelsdia = new string[12];

        for (int i = 0; i < 12; i++)
        {
            var dia = DateTimeOffset.UtcNow.AddDays(-11 + i).Date;
            labelsdia[i] = dia.ToString("dd");
            tendenciaCreados[i] = ticketsRecientes.Count(t => t.FechaCreacion.Date == dia);
            tendenciaCerrados[i] = ticketsRecientes.Count(t => t.FechaCierre?.Date == dia);
        }

        ViewBag.Nuevos = nuevos;
        ViewBag.EnProceso = enProceso;
        ViewBag.EnEspera = enEspera;
        ViewBag.Cerrados = cerrados;
        ViewBag.Criticos = criticos;
        ViewBag.PromedioHoras = promedioHoras;

        // Datos para gráficos JSON
        ViewBag.EstadoLabels = new[] { "Abierto", "En Proceso", "Pendiente", "Cerrado" };
        ViewBag.EstadoData = new[] { nuevos, enProceso, enEspera, cerrados };

        ViewBag.PrioridadLabels = new[] { "Baja", "Media", "Alta" };
        ViewBag.PrioridadData = new[] { baja, media, alta };

        ViewBag.TendenciaLabels = labelsdia;
        ViewBag.TendenciaCreados = tendenciaCreados;
        ViewBag.TendenciaCerrados = tendenciaCerrados;

        return View();
    }
}
