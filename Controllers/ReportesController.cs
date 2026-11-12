using GRUPAL.Data;
using GRUPAL.Filters;
using GRUPAL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

[TypeFilter(typeof(AdminAuthFilter))]
public class ReportesController(ApplicationDbContext db, CorreoService correo) : Controller
{
    public async Task<IActionResult> Contactos(string? filtro, DateTime? fecha)
    {
        var consulta = db.MensajesContacto.AsNoTracking().AsQueryable();
        consulta = filtro switch
        {
            "Spam" => consulta.Where(x => x.EsSpam),
            "Archivado" => consulta.Where(x => x.Archivado),
            _ => consulta.Where(x => !x.EsSpam && !x.Archivado)
        };
        if (fecha.HasValue) consulta = consulta.Where(x => x.CreadoEn.Date == fecha.Value.Date);
        ViewBag.Filtro = filtro;
        ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");
        return View(await consulta.OrderByDescending(x => x.CreadoEn).Take(250).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Gestionar(int id, string accion, string? respuesta)
    {
        var contacto = await db.MensajesContacto.FindAsync(id);
        if (contacto is null) return NotFound();
        switch (accion)
        {
            case "spam": contacto.EsSpam = true; break;
            case "archivar": contacto.Archivado = true; break;
            case "restaurar": contacto.Archivado = false; contacto.EsSpam = false; break;
            case "responder":
                respuesta = respuesta?.Trim();
                if (string.IsNullOrWhiteSpace(respuesta) || respuesta.Length > 3000)
                    return BadRequest("Escribe una respuesta de hasta 3000 caracteres.");
                if (contacto.RespondidoEn.HasValue) return RedirectToAction(nameof(Contactos));
                await db.MensajesContacto.Where(m => m.Id == id && m.Respuesta == null)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.Respuesta, respuesta));
                await db.Entry(contacto).ReloadAsync();
                if (contacto.Respuesta != respuesta)
                {
                    TempData["Mensaje"] = "Ya existe una respuesta preparada. Reintenta con ese mismo texto.";
                    return RedirectToAction(nameof(Contactos));
                }
                if (!await correo.EnviarAsync(contacto.Correo, "Respuesta de AlToke", respuesta, $"contacto-{id}-{respuesta}"))
                {
                    TempData["Mensaje"] = "No se pudo enviar. Revisa la configuración de Resend y vuelve a intentarlo.";
                    return RedirectToAction(nameof(Contactos));
                }
                contacto.Respuesta = respuesta;
                contacto.RespondidoEn = DateTime.UtcNow;
                contacto.Atendido = true;
                break;
            default: return BadRequest();
        }
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Contactos));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarAtendido(int id, bool atendido)
    {
        var contacto = await db.MensajesContacto.FindAsync(id);
        if (contacto is null) return NotFound();
        contacto.Atendido = atendido;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Contactos));
    }
}
