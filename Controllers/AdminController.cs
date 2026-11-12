using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Filters;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using GRUPAL.Services;

namespace GRUPAL.Controllers;

[TypeFilter(typeof(AdminAuthFilter))]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly Cloudinary? _cloudinary;
    private readonly AlgoliaObjetosService _algolia;
    private readonly CategoriaObjetoService _categoriaObjeto;

    public AdminController(ApplicationDbContext context, IConfiguration config, IWebHostEnvironment env,
        AlgoliaObjetosService algolia, CategoriaObjetoService categoriaObjeto)
    {
        _context = context;
        _config = config;
        _env = env;
        _algolia = algolia;
        _categoriaObjeto = categoriaObjeto;

        var cloud = config.GetSection("Cloudinary");
        if (!string.IsNullOrWhiteSpace(cloud["CloudName"]) && !string.IsNullOrWhiteSpace(cloud["ApiKey"])
            && !string.IsNullOrWhiteSpace(cloud["ApiSecret"]))
        {
            var account = new Account(cloud["CloudName"], cloud["ApiKey"], cloud["ApiSecret"]);
            _cloudinary = new Cloudinary(account);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  LOGIN / LOGOUT
    // ═══════════════════════════════════════════════════════════

    [SkipAdminAuth]
    public IActionResult Login()
    {
        // If already authenticated, go straight to dashboard
        if (HttpContext.Session.GetString("IsAdmin") == "true")
            return RedirectToAction(nameof(Index));
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SkipAdminAuth]
    public IActionResult Login(string password)
    {
        var adminPassword = _config["AdminSettings:Password"];
        if (!string.IsNullOrWhiteSpace(adminPassword) && !string.IsNullOrEmpty(password)
            && CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(password)),
                SHA256.HashData(Encoding.UTF8.GetBytes(adminPassword))))
        {
            HttpContext.Session.SetString("IsAdmin", "true");
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Error = string.IsNullOrWhiteSpace(adminPassword)
            ? "Configura AdminSettings__Password en el servidor para habilitar el acceso."
            : "Contraseña incorrecta. Inténtalo de nuevo.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // ═══════════════════════════════════════════════════════════
    //  DASHBOARD
    // ═══════════════════════════════════════════════════════════

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalObjetos = await _context.ObjetosPerdidos.CountAsync();
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.ObjetosPerdidos = await _context.ObjetosPerdidos.CountAsync(o => o.Estado == EstadoObjeto.Perdido);
        ViewBag.ObjetosEncontrados = await _context.ObjetosPerdidos.CountAsync(o => o.Estado == EstadoObjeto.Encontrado);

        var recientes = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .Take(5)
            .ToListAsync();

        return View(recientes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SincronizarAlgolia(CancellationToken ct)
    {
        if (!_algolia.PuedeSincronizar)
        {
            TempData["Error"] = "Completa las variables de Algolia para sincronizar avisos.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            var cantidad = await _algolia.SincronizarAsync(ct);
            TempData["Exito"] = $"{cantidad} avisos enviados al índice de Algolia.";
        }
        catch (Exception)
        {
            TempData["Error"] = "No se pudo sincronizar. Revisa la conexión y permisos de Algolia.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReentrenarCategoria(CancellationToken ct)
    {
        var entrenado = await _categoriaObjeto.ReentrenarAsync(ct);
        TempData[entrenado ? "Exito" : "Error"] = entrenado
            ? "El clasificador de categorías se reentrenó con los avisos actuales."
            : "Se necesitan al menos 10 avisos repartidos en dos o más categorías para entrenar el modelo.";
        return RedirectToAction(nameof(Index));
    }

    // ═══════════════════════════════════════════════════════════
    //  OBJETOS
    // ═══════════════════════════════════════════════════════════

    public async Task<IActionResult> Objetos()
    {
        var objetos = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .ToListAsync();
        return View(objetos);
    }

    // ═══════════════════════════════════════════════════════════
    //  USUARIOS
    // ═══════════════════════════════════════════════════════════

    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
        return View(usuarios);
    }

    // ═══════════════════════════════════════════════════════════
    //  EDITAR OBJETO
    // ═══════════════════════════════════════════════════════════

    public async Task<IActionResult> EditarObjeto(int id)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();
        return View(objeto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarObjeto(ObjetoPerdido objeto)
    {
        ModelState.Remove("Usuario");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("FotoUrl");

        var objetoDb = await _context.ObjetosPerdidos.FindAsync(objeto.Id);
        if (objetoDb == null) return NotFound();

        objetoDb.Título = objeto.Título;
        objetoDb.Categoría = objeto.Categoría;
        objetoDb.Descripcion = objeto.Descripcion;
        if (objeto.Fecha == default || objeto.Fecha.Date > DateTime.UtcNow.AddHours(-5).Date || !Enum.IsDefined(objeto.Estado))
        { ModelState.AddModelError("", "La fecha debe ser válida y no futura."); return View(objeto); }
        objetoDb.Fecha = objeto.Fecha;
        if (objeto.TipoPublicacion is not (TipoPublicacion.Dueno or TipoPublicacion.Hallazgo)) return BadRequest();
        objetoDb.TipoPublicacion = objeto.TipoPublicacion;
        objetoDb.Estado = objeto.Estado;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Objetos));
    }

    // ═══════════════════════════════════════════════════════════
    //  ELIMINAR OBJETO
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarObjeto(int id)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();

        // Limpiar foto (Cloudinary o local)
        await EliminarFotoAsync(objeto.FotoUrl);

        _context.ObjetosPerdidos.Remove(objeto);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Objetos));
    }

    // ═══════════════════════════════════════════════════════════
    //  CAMBIAR ESTADO
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, EstadoObjeto nuevoEstado)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();

        objeto.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Objetos));
    }

    // ═══════════════════════════════════════════════════════════
    //  ELIMINAR USUARIO (con limpieza de fotos)
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return NotFound();

        // Limpiar fotos de todos los objetos del usuario antes del cascade delete
        if (usuario.ObjetosPerdidos != null)
        {
            foreach (var obj in usuario.ObjetosPerdidos)
            {
                await EliminarFotoAsync(obj.FotoUrl);
            }
        }

        var solicitudesUsuario = await _context.SolicitudesRecuperacion
            .Where(s => s.SolicitanteId == usuario.Id).ToListAsync();
        _context.SolicitudesRecuperacion.RemoveRange(solicitudesUsuario);
        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Usuarios));
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPER: Eliminar foto (Cloudinary o disco local)
    // ═══════════════════════════════════════════════════════════

    private async Task EliminarFotoAsync(string? fotoUrl)
    {
        if (string.IsNullOrEmpty(fotoUrl)) return;

        if (fotoUrl.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase)
            && _cloudinary is not null)
        {
            // Extraer public_id de la URL de Cloudinary
            var match = Regex.Match(fotoUrl, @"/upload/(?:v\d+/)?(.+)\.\w+$");
            if (match.Success)
            {
                var publicId = match.Groups[1].Value;
                await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            }
        }
        else if (fotoUrl.StartsWith("/uploads/"))
        {
            // Archivo local (legacy) — intentar eliminar
            var nombreArchivo = Path.GetFileName(fotoUrl);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", nombreArchivo);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
