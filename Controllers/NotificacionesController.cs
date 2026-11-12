using GRUPAL.Data;
using GRUPAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

public class NotificacionesController(ApplicationDbContext db) : Controller
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

}
