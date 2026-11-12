using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Services;
using GRUPAL.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

public class NotificacionesController(ApplicationDbContext db, IHubContext<ObjetosHub> hub,
    CorreoService correo, ILogger<NotificacionesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return Unauthorized();
        var lista = await db.Notificaciones.AsNoTracking().Where(n => n.UsuarioId == usuario)
            .OrderByDescending(n => n.Id).Take(50)
            .Select(n => new { n.Id, n.Texto, n.Leida, n.ObjetoPerdidoId }).ToListAsync();
        var noLeidas = await db.Notificaciones.CountAsync(n => n.UsuarioId == usuario && !n.Leida);
        return Json(new { noLeidas, lista });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Leer(int id)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return Unauthorized();
        var aviso = await db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuario);
        if (aviso is null) return NotFound();
        aviso.Leida = true;
        await db.SaveChangesAsync();
        return RedirectToAction("Detalle", "Explorar", new { id = aviso.ObjetoPerdidoId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoVi(int id, string lugar, DateTime vistoEn)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var objeto = await db.ObjetosPerdidos.Include(o => o.Usuario).FirstOrDefaultAsync(o => o.Id == id);
        if (objeto is null) return NotFound();
        if (objeto.UsuarioId == usuario || objeto.Estado != EstadoObjeto.Perdido) return BadRequest();
        if (string.IsNullOrWhiteSpace(lugar) || lugar.Length > 200 || vistoEn == default || vistoEn > DateTime.UtcNow.AddHours(-5))
            return BadRequest("Indica dónde lo viste y una fecha y hora válidas de Perú.");
        // Evitar duplicados por doble clic o reenvío del mismo formulario.
        var texto = $"Alguien vio tu {objeto.Título} en {lugar.Trim()} el {vistoEn:dd/MM/yyyy HH:mm} (Perú).";
        var aviso = await db.Notificaciones.FirstOrDefaultAsync(n => n.UsuarioId == objeto.UsuarioId && n.InformanteId == usuario && n.ObjetoPerdidoId == id && n.Texto == texto);
        if (aviso is null)
        {
            aviso = new Notificacion { UsuarioId = objeto.UsuarioId, InformanteId = usuario.Value, ObjetoPerdidoId = id, Texto = texto, VistoEn = vistoEn };
            db.Notificaciones.Add(aviso);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                db.Entry(aviso).State = EntityState.Detached;
                aviso = await db.Notificaciones.SingleAsync(n => n.UsuarioId == objeto.UsuarioId && n.InformanteId == usuario && n.ObjetoPerdidoId == id && n.Texto == texto);
            }
        }
        try { await hub.Clients.Group($"usuario-{objeto.UsuarioId}").SendAsync("NotificacionRecibida"); }
        catch (Exception ex) { logger.LogWarning("No se pudo avisar en vivo ({Tipo}).", ex.GetType().Name); }
        if (!aviso.CorreoEnviado && objeto.Usuario is not null)
        {
            aviso.CorreoEnviado = await correo.EnviarAsync(objeto.Usuario.Correo, "Vieron tu objeto en AlToke", texto, $"avistamiento-{aviso.Id}");
            await db.SaveChangesAsync();
        }
        TempData["Exito"] = "Avisamos al dueño. Gracias por ayudar.";
        return RedirectToAction("Detalle", "Explorar", new { id });
    }
}
