using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TicketsAndretich.Web.Data;
using TicketsAndretich.Web.Models;
using TicketsAndretich.Web.Services;

namespace TicketsAndretich.Web.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorage _storage;
    private readonly IEmailSender _emailSender;

    public TicketsController(AppDbContext db, UserManager<ApplicationUser> userManager, IFileStorage storage, IEmailSender emailSender)
    {
        _db = db;
        _userManager = userManager;
        _storage = storage;
        _emailSender = emailSender;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);
        var currentUser = await _db.Users.FindAsync(currentUserId);
        var isAdmin = User.IsInRole("Admin");

        var query = _db.Tickets
            .Include(t => t.DepartamentoDestino)
            .Include(t => t.Creator)
            .Include(t => t.AsignadoA)
            .OrderByDescending(t => t.FechaCreacion)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(t =>
                t.CreadorUserId == currentUserId ||
                t.AsignadoUserId == currentUserId ||
                (t.EnviarATodoElDepartamento && t.DepartamentoDestinoId == currentUser!.DepartmentId));
        }

        var allTickets = await query.ToListAsync();

        // Separar tickets
        var misTickets = allTickets.Where(t => 
            t.AsignadoUserId == currentUserId || 
            t.CreadorUserId == currentUserId).ToList();
        
        var ticketsDepartamento = allTickets.Where(t => 
            t.AsignadoUserId != currentUserId && 
            t.CreadorUserId != currentUserId &&
            t.EnviarATodoElDepartamento && 
            t.DepartamentoDestinoId == currentUser?.DepartmentId).ToList();

        ViewBag.MisTickets = misTickets.Where(t => t.Estado != EstadoTicket.Cerrado).ToList();
        ViewBag.MisTicketsCerrados = misTickets.Where(t => t.Estado == EstadoTicket.Cerrado).ToList();
        ViewBag.TicketsDepartamento = ticketsDepartamento.Where(t => t.Estado != EstadoTicket.Cerrado).ToList();
        ViewBag.TicketsDepartamentoCerrados = ticketsDepartamento.Where(t => t.Estado == EstadoTicket.Cerrado).ToList();
        ViewBag.IsAdmin = isAdmin;

        return View(allTickets);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Users = new SelectList(await _db.Users.OrderBy(u => u.Email!).ToListAsync(), "Id", "Email");
        return View(new TicketCreateVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateVm vm, List<IFormFile> archivos)
    {
        if (archivos.Count > 3)
            ModelState.AddModelError(nameof(archivos), "Puede adjuntar hasta 3 archivos.");

        if (!ModelState.IsValid)
        {
            ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", vm.DepartamentoDestinoId);
            ViewBag.Users = new SelectList(await _db.Users.OrderBy(u => u.Email!).ToListAsync(), "Id", "Email", vm.AsignadoUserId);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var creador = await _db.Users.FindAsync(userId);

        var ticket = new Ticket
        {
            Asunto = vm.Asunto,
            Descripcion = vm.Descripcion,
            Importancia = vm.Importancia,
            Categoria = vm.Categoria,
            Estado = EstadoTicket.Abierto,
            CreadorUserId = userId,
            DepartamentoDestinoId = vm.DepartamentoDestinoId,
            EnviarATodoElDepartamento = vm.EnviarATodoElDepartamento,
            AsignadoUserId = vm.AsignadoUserId
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        // Registrar en historial - Creación
        await RegistrarHistorial(ticket.Id, TipoEventoTicket.Creado, 
            $"Ticket creado por {creador?.Email}", null, null, EstadoTicket.Abierto, userId);

        // Si tiene asignación inicial
        if (!string.IsNullOrEmpty(vm.AsignadoUserId))
        {
            var asignado = await _db.Users.FindAsync(vm.AsignadoUserId);
            await RegistrarHistorial(ticket.Id, TipoEventoTicket.Asignado, 
                $"Asignado a {asignado?.Email}", null, null, null, userId);
        }

        // Save attachments (<=3)
        int saved = 0;
        foreach (var file in archivos)
        {
            if (file.Length == 0) continue;
            if (++saved > 3) break;

            var storagePath = await _storage.SaveAsync($"ticket_{ticket.Id}", file.FileName, file.OpenReadStream());
            _db.TicketAttachments.Add(new TicketAttachment
            {
                TicketId = ticket.Id,
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                SizeBytes = file.Length,
                StoragePath = storagePath,
                UploadedByUserId = userId
            });
        }
        await _db.SaveChangesAsync();

        // Enviar notificación con HTML
        var recipients = await GetRecipients(vm.EnviarATodoElDepartamento, vm.DepartamentoDestinoId, vm.AsignadoUserId, userId);
        string link = Url.Action("Details", "Tickets", new { id = ticket.Id }, Request.Scheme)!;
        string subject = $"[Tickets Andretich] Nuevo ticket #{ticket.Id} - {ticket.Asunto} ({ticket.Importancia})";
        string body = EmailTemplates.NuevoTicket(
            ticket.Id, 
            ticket.Asunto, 
            ticket.Importancia.ToString(), 
            ticket.Categoria.ToString(), 
            ticket.Descripcion, 
            creador?.Email ?? "Usuario",
            link);

        foreach (var r in recipients)
            await _emailSender.SendEmailAsync(r, subject, body);

        // Crear notificaciones en el sistema
        await NotificarInteresados(ticket.Id, TipoNotificacion.NuevoTicket, 
            $"Nuevo ticket #{ticket.Id}: {ticket.Asunto}", userId);

        TempData["Success"] = "Ticket creado y notificaciones enviadas.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var t = await _db.Tickets
            .Include(t => t.DepartamentoDestino)
            .Include(t => t.Adjuntos)
            .Include(t => t.Creator)
            .Include(t => t.AsignadoA)
            .Include(t => t.UltimaAccionPor)
            .Include(t => t.Historial.OrderByDescending(h => h.Fecha))
                .ThenInclude(h => h.Usuario)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (t == null) return NotFound();

        ViewBag.Departments = new SelectList(await _db.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Users = new SelectList(await _db.Users.OrderBy(u => u.Email!).ToListAsync(), "Id", "Email");
        return View(t);
    }

    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var attachment = await _db.TicketAttachments.FindAsync(id);
        if (attachment == null) return NotFound();

        var stream = await _storage.OpenReadAsync(attachment.StoragePath);
        if (stream == null) return NotFound();

        // Para imágenes y PDFs, permitir visualización inline
        var contentDisposition = attachment.ContentType?.StartsWith("image/") == true || 
                                  attachment.ContentType == "application/pdf" 
            ? "inline" : "attachment";

        Response.Headers["Content-Disposition"] = $"{contentDisposition}; filename=\"{attachment.FileName}\"";
        return File(stream, attachment.ContentType ?? "application/octet-stream");
    }

    // --------- Acciones de Gestión ---------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, EstadoTicket nuevoEstado, string? comentario, int? tiempoEstimadoHoras)
    {
        var ticket = await _db.Tickets.Include(t => t.Creator).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var usuarioAccion = await _db.Users.FindAsync(userId);
        var estadoAnterior = ticket.Estado;
        ticket.Estado = nuevoEstado;
        ticket.UltimaAccionPorUserId = userId;

        // Si pasa a En Progreso, registrar fecha de inicio de tratamiento
        if (nuevoEstado == EstadoTicket.EnProgreso && !ticket.FechaInicioTratamiento.HasValue)
        {
            ticket.FechaInicioTratamiento = DateTimeOffset.UtcNow;
            ticket.TiempoEstimadoHoras = tiempoEstimadoHoras;
        }

        if (nuevoEstado == EstadoTicket.Cerrado)
        {
            ticket.FechaCierre = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Registrar en historial
        var tipoEvento = nuevoEstado == EstadoTicket.Cerrado ? TipoEventoTicket.Cerrado : TipoEventoTicket.CambioEstado;
        await RegistrarHistorial(id, tipoEvento, 
            $"Estado cambiado de {estadoAnterior} a {nuevoEstado} por {usuarioAccion?.Email}", 
            comentario, estadoAnterior, nuevoEstado, userId);

        // Notificar al creador del ticket
        if (ticket.Creator?.Email != null)
        {
            string link = Url.Action("Details", "Tickets", new { id = ticket.Id }, Request.Scheme)!;
            string subject = $"[Tickets Andretich] Ticket #{ticket.Id} - Estado actualizado a {nuevoEstado}";
            string body = EmailTemplates.CambioEstado(
                ticket.Id,
                ticket.Asunto,
                estadoAnterior.ToString(),
                nuevoEstado.ToString(),
                comentario,
                usuarioAccion?.Email ?? "Usuario",
                link);

            await _emailSender.SendEmailAsync(ticket.Creator.Email, subject, body);
        }

        // Crear notificaciones en el sistema
        await NotificarInteresados(id, TipoNotificacion.CambioEstado, 
            $"Ticket #{id} cambió a {nuevoEstado}", userId);

        TempData["Success"] = $"Estado cambiado a {nuevoEstado}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reasignar(int id, string? nuevoAsignadoId, int? nuevoDepartamentoId, string justificacion)
    {
        // Validar justificación obligatoria
        if (string.IsNullOrWhiteSpace(justificacion))
        {
            TempData["Error"] = "Debe proporcionar una justificación para reasignar el ticket.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticket = await _db.Tickets.Include(t => t.Creator).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var usuarioAccion = await _db.Users.FindAsync(userId);
        string? emailAsignado = null;
        string? nombreAsignado = null;

        ticket.JustificacionReasignacion = justificacion;
        ticket.UltimaAccionPorUserId = userId;

        if (!string.IsNullOrEmpty(nuevoAsignadoId))
        {
            ticket.AsignadoUserId = nuevoAsignadoId;
            ticket.EnviarATodoElDepartamento = false;
            var asignado = await _db.Users.FindAsync(nuevoAsignadoId);
            emailAsignado = asignado?.Email;
            nombreAsignado = asignado?.Email;
        }

        if (nuevoDepartamentoId.HasValue)
        {
            ticket.DepartamentoDestinoId = nuevoDepartamentoId.Value;
        }

        ticket.Estado = EstadoTicket.Pendiente;
        await _db.SaveChangesAsync();

        // Registrar en historial
        await RegistrarHistorial(id, TipoEventoTicket.Reasignado, 
            $"Reasignado a {nombreAsignado ?? "nuevo departamento"} por {usuarioAccion?.Email}", 
            justificacion, null, EstadoTicket.Pendiente, userId);

        string link = Url.Action("Details", "Tickets", new { id = ticket.Id }, Request.Scheme)!;

        // Notificar al creador
        if (ticket.Creator?.Email != null)
        {
            string subject = $"[Tickets Andretich] Ticket #{ticket.Id} - Reasignado";
            string body = EmailTemplates.Reasignacion(
                ticket.Id,
                ticket.Asunto,
                nombreAsignado ?? "Otro usuario/departamento",
                justificacion,
                usuarioAccion?.Email ?? "Usuario",
                link);

            await _emailSender.SendEmailAsync(ticket.Creator.Email, subject, body);
        }

        // Notificar al nuevo asignado
        if (!string.IsNullOrEmpty(emailAsignado) && emailAsignado != ticket.Creator?.Email)
        {
            string subject = $"[Tickets Andretich] Se te ha asignado el Ticket #{ticket.Id}";
            string body = EmailTemplates.Reasignacion(
                ticket.Id,
                ticket.Asunto,
                nombreAsignado!,
                justificacion,
                usuarioAccion?.Email ?? "Usuario",
                link);

            await _emailSender.SendEmailAsync(emailAsignado, subject, body);
        }

        TempData["Success"] = "Ticket reasignado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, string justificacion)
    {
        // Validar justificación obligatoria
        if (string.IsNullOrWhiteSpace(justificacion))
        {
            TempData["Error"] = "Debe proporcionar una justificación para cancelar el ticket.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticket = await _db.Tickets.Include(t => t.Creator).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var usuarioAccion = await _db.Users.FindAsync(userId);
        var estadoAnterior = ticket.Estado;
        
        ticket.Estado = EstadoTicket.Cerrado;
        ticket.FechaCierre = DateTimeOffset.UtcNow;
        ticket.JustificacionCancelacion = justificacion;
        ticket.UltimaAccionPorUserId = userId;
        
        await _db.SaveChangesAsync();

        // Registrar en historial
        await RegistrarHistorial(id, TipoEventoTicket.Cancelado, 
            $"Ticket cancelado por {usuarioAccion?.Email}", 
            justificacion, estadoAnterior, EstadoTicket.Cerrado, userId);

        // Notificar al creador
        if (ticket.Creator?.Email != null)
        {
            string link = Url.Action("Details", "Tickets", new { id = ticket.Id }, Request.Scheme)!;
            string subject = $"[Tickets Andretich] Ticket #{ticket.Id} - Cancelado";
            string body = EmailTemplates.CambioEstado(
                ticket.Id,
                ticket.Asunto,
                estadoAnterior.ToString(),
                "Cancelado",
                justificacion,
                usuarioAccion?.Email ?? "Usuario",
                link);

            await _emailSender.SendEmailAsync(ticket.Creator.Email, subject, body);
        }

        TempData["Success"] = "Ticket cancelado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Historial)
            .Include(t => t.Adjuntos)
            .FirstOrDefaultAsync(t => t.Id == id);
            
        if (ticket == null) return NotFound();

        // Eliminar historial y adjuntos primero
        _db.Set<TicketHistorial>().RemoveRange(ticket.Historial);
        _db.TicketAttachments.RemoveRange(ticket.Adjuntos);
        
        // Eliminar ticket
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Ticket #{id} eliminado permanentemente.";
        return RedirectToAction(nameof(Index));
    }

    // --------- Helpers ---------

    private async Task RegistrarHistorial(int ticketId, TipoEventoTicket tipo, string descripcion, 
        string? detalle, EstadoTicket? estadoAnterior, EstadoTicket? estadoNuevo, string usuarioId)
    {
        var historial = new TicketHistorial
        {
            TicketId = ticketId,
            TipoEvento = tipo,
            Descripcion = descripcion,
            Detalle = detalle,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            UsuarioId = usuarioId,
            Fecha = DateTimeOffset.UtcNow
        };
        _db.Set<TicketHistorial>().Add(historial);
        await _db.SaveChangesAsync();
    }

    private async Task<List<string>> GetRecipients(bool todoElDepartamento, int departamentoId, string? asignadoUserId, string creadorId)
    {
        var recipients = new List<string>();

        if (todoElDepartamento)
        {
            var emails = await _db.Users
                .Where(u => u.DepartmentId == departamentoId && u.Email != null)
                .Select(u => u.Email!)
                .ToListAsync();
            recipients.AddRange(emails);
        }
        else if (!string.IsNullOrWhiteSpace(asignadoUserId))
        {
            var email = await _db.Users
                .Where(u => u.Id == asignadoUserId && u.Email != null)
                .Select(u => u.Email!)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(email)) recipients.Add(email);
        }

        recipients = recipients.Distinct().ToList();
        if (recipients.Count == 0)
        {
            var creatorEmail = await _db.Users.Where(u => u.Id == creadorId).Select(u => u.Email!).FirstAsync();
            recipients.Add(creatorEmail);
        }

        return recipients;
    }

    // --------- Notificaciones ---------

    [HttpGet]
    public async Task<IActionResult> GetNotificaciones()
    {
        var userId = _userManager.GetUserId(User);
        var notificaciones = await _db.Notificaciones
            .Include(n => n.Ticket)
            .Where(n => n.UsuarioId == userId && !n.Leida)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(10)
            .Select(n => new {
                n.Id,
                n.TicketId,
                n.Mensaje,
                Tipo = n.Tipo.ToString(),
                Fecha = n.FechaCreacion.ToLocalTime().ToString("dd/MM HH:mm"),
                TicketAsunto = n.Ticket != null ? n.Ticket.Asunto : ""
            })
            .ToListAsync();

        return Json(new { count = notificaciones.Count, items = notificaciones });
    }

    [HttpPost]
    public async Task<IActionResult> MarcarNotificacionLeida(int id)
    {
        var userId = _userManager.GetUserId(User);
        var notif = await _db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId);
        if (notif != null)
        {
            notif.Leida = true;
            await _db.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> MarcarTodasLeidas()
    {
        var userId = _userManager.GetUserId(User);
        var notifs = await _db.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leida).ToListAsync();
        foreach (var n in notifs) n.Leida = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    private async Task CrearNotificacion(int ticketId, string usuarioDestinoId, TipoNotificacion tipo, string mensaje)
    {
        var notif = new Notificacion
        {
            TicketId = ticketId,
            UsuarioId = usuarioDestinoId,
            Tipo = tipo,
            Mensaje = mensaje,
            FechaCreacion = DateTimeOffset.UtcNow
        };
        _db.Notificaciones.Add(notif);
        await _db.SaveChangesAsync();
    }

    private async Task NotificarInteresados(int ticketId, TipoNotificacion tipo, string mensaje, string? excluirUserId = null)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket == null) return;

        var usuariosNotificar = new List<string>();

        // Notificar al asignado
        if (!string.IsNullOrEmpty(ticket.AsignadoUserId) && ticket.AsignadoUserId != excluirUserId)
            usuariosNotificar.Add(ticket.AsignadoUserId);

        // Notificar al creador
        if (!string.IsNullOrEmpty(ticket.CreadorUserId) && ticket.CreadorUserId != excluirUserId)
            usuariosNotificar.Add(ticket.CreadorUserId);

        // Si es para todo el departamento, notificar a todos los del depto
        if (ticket.EnviarATodoElDepartamento && ticket.DepartamentoDestinoId > 0)
        {
            var usersDepto = await _db.Users
                .Where(u => u.DepartmentId == ticket.DepartamentoDestinoId && u.Id != excluirUserId)
                .Select(u => u.Id)
                .ToListAsync();
            usuariosNotificar.AddRange(usersDepto);
        }

        foreach (var userId in usuariosNotificar.Distinct())
        {
            await CrearNotificacion(ticketId, userId, tipo, mensaje);
        }
    }
}

public class TicketCreateVm
{
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Importancia Importancia { get; set; } = Importancia.Media;
    public Categoria Categoria { get; set; } = Categoria.Solicitud;
    public int DepartamentoDestinoId { get; set; }
    public bool EnviarATodoElDepartamento { get; set; } = true;
    public string? AsignadoUserId { get; set; }
}
