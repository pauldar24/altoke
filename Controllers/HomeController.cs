using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Models;
using GRUPAL.Data;
using GRUPAL.Services;

namespace GRUPAL.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ModeracionService _moderacion;
    private readonly ModeloSpamService _spam;
    private readonly TurnstileService _turnstile;

    public HomeController(ApplicationDbContext context, ModeracionService moderacion, TurnstileService turnstile, ModeloSpamService spam)
    {
        _context = context;
        _moderacion = moderacion;
        _spam = spam;
        _turnstile = turnstile;
    }

    public async Task<IActionResult> Index()
    {
        var objetosRecientes = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .Take(6)
            .ToListAsync();

        // Stats
        ViewBag.TotalObjetos = await _context.ObjetosPerdidos.CountAsync();
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.ObjetosEncontrados = await _context.ObjetosPerdidos.CountAsync(o => o.Estado == EstadoObjeto.Encontrado);

        return View(objetosRecientes);
    }

    public IActionResult ComoFunciona()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contacto()
    {
        ViewBag.ConSesion = HttpContext.Session.GetInt32("UsuarioId").HasValue;
        return View(new MensajeContacto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacto(MensajeContacto contacto)
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        ViewBag.ConSesion = usuarioId.HasValue;
        if (usuarioId.HasValue)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId.Value);
            if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
            contacto.Nombre = usuario.Nombre;
            contacto.Correo = usuario.Correo;
            ModelState.Remove(nameof(contacto.Nombre));
            ModelState.Remove(nameof(contacto.Correo));
        }
        if (!usuarioId.HasValue && !await _turnstile.VerificarAsync(Request.Form["cf-turnstile-response"], "contacto"))
            ModelState.AddModelError("", "Completa la verificación antiabuso. Si no aparece, contacta al administrador.");
        if (!ModelState.IsValid) return View(contacto);
        contacto.Id = 0;
        contacto.Nombre = contacto.Nombre.Trim();
        contacto.Correo = contacto.Correo.Trim();
        contacto.Mensaje = contacto.Mensaje.Trim();
        var posibleSpam = _spam.EsSpam(contacto.Asunto + ". " + contacto.Mensaje);
        contacto.Clasificacion = posibleSpam == true ? "Posible spam (ML)" :
            await _moderacion.ClasificarContactoAsync(contacto.Asunto + ". " + contacto.Mensaje);
        contacto.EsSpam = posibleSpam == true || contacto.Clasificacion == "Spam/abuso";
        contacto.Sentimiento = "No aplica";
        contacto.Archivado = false;
        contacto.Atendido = false;
        contacto.Respuesta = null;
        contacto.RespondidoEn = null;
        contacto.CreadoEn = DateTime.UtcNow;
        _context.MensajesContacto.Add(contacto);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "¡Gracias por escribirnos! Tu mensaje fue recibido.";
        return RedirectToAction(nameof(Contacto));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
