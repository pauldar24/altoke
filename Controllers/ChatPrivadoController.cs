using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

public class ChatPrivadoController(ApplicationDbContext db, IHubContext<ObjetosHub> hub,
    ILogger<ChatPrivadoController> logger) : Controller
{
    private async Task<SolicitudRecuperacion?> BuscarAsync(int id)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return null;
        return await db.SolicitudesRecuperacion.Include(s => s.ObjetoPerdido).FirstOrDefaultAsync(s => s.Id == id
            && s.Estado == EstadoSolicitud.Aceptada
            && (s.SolicitanteId == usuario || s.ObjetoPerdido.UsuarioId == usuario));
    }

    public async Task<IActionResult> Index(int id)
    {
        if (!HttpContext.Session.GetInt32("UsuarioId").HasValue) return RedirectToAction("Ingresar", "Cuenta");
        var solicitud = await BuscarAsync(id);
        if (solicitud is null) return NotFound();
        return View(solicitud);
    }

    [HttpGet]
    public async Task<IActionResult> Mensajes(int id, int despues = 0, int? antes = null)
    {
        Response.Headers.CacheControl = "no-store";
        if (await BuscarAsync(id) is null) return NotFound();
        var consulta = db.MensajesPrivados.AsNoTracking().Where(m => m.SolicitudRecuperacionId == id);
        if (antes.HasValue) consulta = consulta.Where(m => m.Id < antes);
        else if (despues > 0) consulta = consulta.Where(m => m.Id > despues);
        var pagina = despues > 0 && !antes.HasValue
            ? consulta.OrderBy(m => m.Id).Take(100)
            : consulta.OrderByDescending(m => m.Id).Take(100);
        var mensajes = await pagina.Select(m => new { m.Id, m.AutorId, autor = m.Autor.Nombre, m.Texto, m.CreadoEn }).ToListAsync();
        return Json(mensajes.OrderBy(m => m.Id));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(int id, string texto, Guid claveEnvio)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var solicitud = await BuscarAsync(id);
        if (solicitud is null) return NotFound();
        if (solicitud.ObjetoPerdido.Estado != EstadoObjeto.Perdido) return BadRequest("El caso está cerrado. El historial sigue disponible.");
        texto = texto?.Trim() ?? "";
        if (texto.Length is < 1 or > 1000 || claveEnvio == Guid.Empty) return BadRequest("Escribe entre 1 y 1000 caracteres.");
        var autor = HttpContext.Session.GetInt32("UsuarioId")!.Value;
        if (await db.MensajesPrivados.AnyAsync(m => m.SolicitudRecuperacionId == id && m.AutorId == autor && m.ClaveEnvio == claveEnvio))
            return Ok();
        var mensaje = new MensajePrivado { SolicitudRecuperacionId = id, AutorId = autor, Texto = texto, ClaveEnvio = claveEnvio };
        db.MensajesPrivados.Add(mensaje);
        // No incluir el texto privado en la campana ni en eventos de difusión.
        var destinatario = autor == solicitud.SolicitanteId ? solicitud.ObjetoPerdido.UsuarioId : solicitud.SolicitanteId;
        var aviso = await db.Notificaciones.FirstOrDefaultAsync(n => n.UsuarioId == destinatario && n.InformanteId == autor
            && n.SolicitudRecuperacionId == id && n.Texto == "Tienes mensajes nuevos en el chat privado.");
        if (aviso is null)
            db.Notificaciones.Add(new Notificacion { UsuarioId = destinatario, InformanteId = autor,
                ObjetoPerdidoId = solicitud.ObjetoPerdidoId, SolicitudRecuperacionId = id, EsChat = true, Texto = "Tienes mensajes nuevos en el chat privado." });
        else { aviso.Leida = false; aviso.CreadoEn = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        try
        {
            await hub.Clients.Groups($"usuario-{autor}", $"usuario-{destinatario}").SendAsync("ChatActualizado", id);
            await hub.Clients.Group($"usuario-{destinatario}").SendAsync("NotificacionRecibida");
        }
        catch (Exception ex) { logger.LogWarning("Mensaje guardado; actualización en vivo pendiente ({Tipo}).", ex.GetType().Name); }
        return Ok();
    }
}
