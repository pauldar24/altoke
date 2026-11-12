using Microsoft.AspNetCore.Mvc;
using GRUPAL.Data;
using GRUPAL.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Services;
using GRUPAL.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GRUPAL.Controllers;

public class ObjetosPerdidosController : Controller
{
    private readonly ModeracionService _moderacion;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly CategoriaObjetoService _categoriaObjeto;
    private readonly IHubContext<ObjetosHub> _objetosHub;

    public ObjetosPerdidosController(ApplicationDbContext context, IConfiguration config, IWebHostEnvironment env,
        CategoriaObjetoService categoriaObjeto, IHubContext<ObjetosHub> objetosHub, ModeracionService moderacion)
    {
        _context = context;
        _moderacion = moderacion;
        _config = config;
        _env = env;
        _categoriaObjeto = categoriaObjeto;
        _objetosHub = objetosHub;
    }

    [HttpGet]
    public async Task<IActionResult> SugerirCategoria(string? titulo, string? descripcion, CancellationToken ct)
    {
        var texto = $"{titulo?.Trim()} {descripcion?.Trim()}".Trim();
        if (texto.Length < 8 || texto.Length > 3200) return NoContent();
        var categoria = await _categoriaObjeto.PredecirAsync(texto, ct);
        return string.IsNullOrWhiteSpace(categoria) ? NoContent() : Json(new { categoria });
    }

    public IActionResult Create()
    {
        if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
            return RedirectToAction("Ingresar", "Cuenta");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObjetoPerdido objeto, IFormFile? foto)
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue) return RedirectToAction("Ingresar", "Cuenta");
        ModelState.Remove("Usuario");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("FotoUrl");
        if (string.IsNullOrWhiteSpace(objeto.Título) || objeto.Título.Length > 150 ||
            string.IsNullOrWhiteSpace(objeto.Descripcion) || objeto.Descripcion.Length > 3000 ||
            string.IsNullOrWhiteSpace(objeto.Categoría) || objeto.Categoría.Length > 100 ||
            objeto.Fecha == default || objeto.Fecha.Date > DateTime.UtcNow.AddHours(-5).Date || objeto.TipoPublicacion is not (TipoPublicacion.Dueno or TipoPublicacion.Hallazgo))
        {
            ModelState.AddModelError("", "Completa los datos del reporte con información válida.");
            return View(objeto);
        }
        if (!ModelState.IsValid) return View(objeto);
        var rechazo = await _moderacion.ValidarObjetoAsync(objeto.Título, objeto.Descripcion);
        if (rechazo is not null) { ModelState.AddModelError("", rechazo); return View(objeto); }
        if (!Coordenadas.Validas(objeto.Latitud, objeto.Longitud)) { ModelState.AddModelError("", "Selecciona una ubicación válida."); return View(objeto); }
        objeto.Estado = EstadoObjeto.Perdido;
        objeto.Id = 0;
        objeto.UsuarioId = usuarioId.Value;
        objeto.Ubicacion = string.IsNullOrWhiteSpace(objeto.Ubicacion) ? "No especificada" : objeto.Ubicacion.Trim();
        if (objeto.Ubicacion.Length > 200) return BadRequest("Ubicación demasiado larga.");
        objeto.FotoUrl = null;
        if (foto is { Length: > 0 })
        {
            if (foto.Length > 5 * 1024 * 1024 ||
                foto.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
            {
                ModelState.AddModelError("", "La foto debe ser JPG, PNG o WebP, de hasta 5 MB.");
                return View(objeto);
            }
            var cloud = _config.GetSection("Cloudinary");
            if (!string.IsNullOrWhiteSpace(cloud["CloudName"]) && !string.IsNullOrWhiteSpace(cloud["ApiKey"])
                && !string.IsNullOrWhiteSpace(cloud["ApiSecret"]))
            {
                var client = new Cloudinary(new Account(cloud["CloudName"], cloud["ApiKey"], cloud["ApiSecret"]));
                using var stream = foto.OpenReadStream();
                var result = await client.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription(Path.GetFileName(foto.FileName), stream),
                    Folder = "altoke/objetos",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                });
                if (result.StatusCode == System.Net.HttpStatusCode.OK) objeto.FotoUrl = result.SecureUrl.ToString();
                else ModelState.AddModelError("", "No se pudo subir la fotografía.");
            }
            else
            {
                var extension = foto.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
                var directorio = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(directorio);
                var nombre = Guid.NewGuid().ToString("N") + extension;
                await using var archivo = System.IO.File.Create(Path.Combine(directorio, nombre));
                await foto.CopyToAsync(archivo);
                objeto.FotoUrl = "/uploads/" + nombre;
            }
            if (!ModelState.IsValid) return View(objeto);
        }
        _context.ObjetosPerdidos.Add(objeto);
        await _context.SaveChangesAsync();
        await _objetosHub.Clients.All.SendAsync("ObjetoPublicado", new
        {
            id = objeto.Id,
            titulo = objeto.Título,
            categoria = objeto.Categoría,
            descripcion = objeto.Descripcion,
            ubicacion = objeto.Ubicacion,
            fecha = objeto.Fecha,
            estado = objeto.EstadoVisible,
            tipoPublicacion = objeto.TipoVisible,
            tipoCodigo = (int)objeto.TipoPublicacion,
            fotoUrl = objeto.FotoUrl,
            autor = HttpContext.Session.GetString("UsuarioNombre") ?? "Anónimo"
        });
        return RedirectToAction("Detalle", "Explorar", new { id = objeto.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto is null) return NotFound();
        if (objeto.UsuarioId != usuario.Value) return StatusCode(StatusCodes.Status403Forbidden);

        if (objeto.Estado == EstadoObjeto.Encontrado) return RedirectToAction("Detalle", "Explorar", new { id });
        objeto.Estado = EstadoObjeto.Encontrado;
        var solicitudes = await _context.SolicitudesRecuperacion.Where(s => s.ObjetoPerdidoId == id).ToListAsync();
        foreach (var solicitud in solicitudes)
        {
            _context.Notificaciones.Add(new Notificacion {
                UsuarioId = solicitud.SolicitanteId, InformanteId = usuario.Value, ObjetoPerdidoId = id,
                SolicitudRecuperacionId = solicitud.Id, Texto = $"El caso de {objeto.Título} fue cerrado como resuelto."
            });
        }
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();
        foreach (var destinatario in solicitudes.Select(s => s.SolicitanteId).Distinct())
        {
            try { await _objetosHub.Clients.Group($"usuario-{destinatario}").SendAsync("NotificacionRecibida"); }
            catch { /* La campana recupera la notificación persistida al reconectar. */ }
        }
        TempData["Exito"] = "Caso cerrado. El objeto fue entregado o recuperado.";
        return RedirectToAction("Detalle", "Explorar", new { id });
    }

    public async Task<IActionResult> Editar(int id)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var obj = await _context.ObjetosPerdidos.FindAsync(id);
        if (obj is null) return NotFound();
        if (obj.UsuarioId != usuario) return Forbid();
        return View(obj);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ObjetoPerdido datos)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var obj = await _context.ObjetosPerdidos.FindAsync(id);
        if (obj is null) return NotFound();
        if (obj.UsuarioId != usuario) return Forbid();
        if (string.IsNullOrWhiteSpace(datos.Título) || datos.Título.Length > 150 ||
            string.IsNullOrWhiteSpace(datos.Descripcion) || datos.Descripcion.Length > 3000 ||
            string.IsNullOrWhiteSpace(datos.Categoría) || datos.Categoría.Length > 100 ||
            string.IsNullOrWhiteSpace(datos.Ubicacion) || datos.Ubicacion.Length > 200 ||
            datos.Fecha == default || datos.Fecha.Date > DateTime.UtcNow.AddHours(-5).Date)
        {
            ModelState.AddModelError("", "Completa correctamente los campos.");
            return View(obj);
        }
        var rechazo = await _moderacion.ValidarObjetoAsync(datos.Título, datos.Descripcion);
        if (rechazo is not null) { ModelState.AddModelError("", rechazo); return View(datos); }
        if (!Coordenadas.Validas(datos.Latitud, datos.Longitud)) { ModelState.AddModelError("", "Selecciona una ubicación válida."); return View(datos); }
        obj.Latitud = datos.Latitud;
        obj.Longitud = datos.Longitud;
        if (datos.TipoPublicacion is not (TipoPublicacion.Dueno or TipoPublicacion.Hallazgo)) return BadRequest();
        obj.TipoPublicacion = datos.TipoPublicacion;
        obj.Título = datos.Título.Trim();
        obj.Categoría = datos.Categoría.Trim();
        obj.Descripcion = datos.Descripcion.Trim();
        obj.Ubicacion = datos.Ubicacion.Trim();
        obj.Fecha = datos.Fecha == default ? obj.Fecha : datos.Fecha;
        await _context.SaveChangesAsync();
        return RedirectToAction("Detalle", "Explorar", new { id });
    }
}
