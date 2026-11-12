using GRUPAL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace GRUPAL.Controllers;

public class CuentaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<Usuario> _hasher = new();
    private readonly IConfiguration _configuration;
    private readonly TurnstileService _turnstile;

    public CuentaController(ApplicationDbContext context, IConfiguration configuration, TurnstileService turnstile)
    {
        _context = context;
        _configuration = configuration;
        _turnstile = turnstile;
    }

    // GET: Cuenta/CrearCuenta
    public IActionResult CrearCuenta()
    {
        if (HttpContext.Session.GetInt32("UsuarioId").HasValue)
            return RedirectToAction("Index", "Home");
            
        return View();
    }

    // POST: Cuenta/CrearCuenta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCuenta(string nombre, string correo, string contraseña)
    {
        if (HttpContext.Session.GetInt32("UsuarioId").HasValue) return RedirectToAction("Index", "Home");
        ViewBag.Nombre = nombre;
        ViewBag.Correo = correo;
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contraseña))
        {
            ViewBag.Error = "Todos los campos son obligatorios.";
            return View();
        }

        nombre = nombre.Trim();
        correo = correo.Trim().ToLowerInvariant();
        if (nombre.Length > 100 || correo.Length > 150 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(correo) || contraseña.Length < 8 || contraseña.Length > 128)
        {
            ViewBag.Error = "Usa un nombre y correo válidos y una contraseña de entre 8 y 128 caracteres.";
            return View();
        }
        var existente = await _context.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo);
        if (existente is not null && existente.Contraseña.StartsWith("AQAAAA", StringComparison.Ordinal) && _hasher.VerifyHashedPassword(existente, existente.Contraseña, contraseña) != PasswordVerificationResult.Failed)
        {
            HttpContext.Session.SetInt32("UsuarioId", existente.Id);
            HttpContext.Session.SetString("UsuarioNombre", existente.Nombre);
            return RedirectToAction("Index", "Home");
        }
        if (!await _turnstile.VerificarAsync(Request.Form["cf-turnstile-response"], "registro"))
        { ViewBag.Error = "Completa la verificación antiabuso. Si no aparece, contacta al administrador."; return View(); }
        var existeCorreo = await _context.Usuarios.AnyAsync(u => u.Correo == correo);
        if (existeCorreo)
        {
            ViewBag.Error = "El correo ya está registrado.";
            return View();
        }

        var nuevoUsuario = new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            Contraseña = string.Empty,
            PublicacionesActivas = 0,
            HistoriasResueltas = 0,
            BuenasAcciones = 0
        };

        nuevoUsuario.Contraseña = _hasher.HashPassword(nuevoUsuario, contraseña);
        _context.Usuarios.Add(nuevoUsuario);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            _context.Entry(nuevoUsuario).State = EntityState.Detached;
            var creado = await _context.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo);
            if (creado is null) throw;
            if (!creado.Contraseña.StartsWith("AQAAAA", StringComparison.Ordinal) || _hasher.VerifyHashedPassword(creado, creado.Contraseña, contraseña) == PasswordVerificationResult.Failed)
            { ViewBag.Error = "Este correo ya tiene una cuenta. Usa Ingresar."; return View(); }
            nuevoUsuario = creado;
        }

        // Iniciar sesión automáticamente
        HttpContext.Session.SetInt32("UsuarioId", nuevoUsuario.Id);
        HttpContext.Session.SetString("UsuarioNombre", nuevoUsuario.Nombre);

        return RedirectToAction("Index", "Home");
    }

    // GET: Cuenta/Ingresar
    public IActionResult Ingresar(bool googleError = false)
    {
        if (HttpContext.Session.GetInt32("UsuarioId").HasValue)
            return RedirectToAction("Index", "Home");
        ViewBag.GoogleLoginEnabled = GoogleLoginEnabled;
        if (googleError) ViewBag.Error = "No se pudo iniciar sesión con Google. Inténtalo de nuevo.";
        else if (TempData["Error"] is string error) ViewBag.Error = error;
        return View();
    }

    private bool GoogleLoginEnabled =>
        !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);

    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (!GoogleLoginEnabled)
        {
            TempData["Error"] = "El inicio de sesión con Google aún no está configurado.";
            return RedirectToAction(nameof(Ingresar));
        }

        var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home");
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), "Cuenta", new { returnUrl = safeReturnUrl })
        };
        return Challenge(properties, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        if (!GoogleLoginEnabled)
            return RedirectToAction(nameof(Ingresar), new { googleError = true });

        var external = await HttpContext.AuthenticateAsync("GoogleExternal");
        if (!external.Succeeded || external.Principal is null)
            return RedirectToAction(nameof(Ingresar), new { googleError = true });

        var principal = external.Principal;
        var correo = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        var emailVerified = principal.FindFirstValue("verified_email");
        if (string.IsNullOrWhiteSpace(correo) || string.Equals(emailVerified, "false", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync("GoogleExternal");
            return RedirectToAction(nameof(Ingresar), new { googleError = true });
        }

        correo = correo.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
        if (usuario is null)
        {
            var nombre = principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name") ?? correo;
            usuario = new Usuario
            {
                Nombre = nombre.Trim()[..Math.Min(nombre.Trim().Length, 100)],
                Correo = correo,
                Contraseña = string.Empty,
                PublicacionesActivas = 0,
                HistoriasResueltas = 0,
                BuenasAcciones = 0
            };
            var claveAleatoria = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            usuario.Contraseña = _hasher.HashPassword(usuario, claveAleatoria);
            _context.Usuarios.Add(usuario);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.Entry(usuario).State = EntityState.Detached;
                usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
                if (usuario is null)
                {
                    await HttpContext.SignOutAsync("GoogleExternal");
                    return RedirectToAction(nameof(Ingresar), new { googleError = true });
                }
            }
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
        HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
        await HttpContext.SignOutAsync("GoogleExternal");
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
    }

    // POST: Cuenta/Ingresar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(string correo, string contraseña)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contraseña))
        {
            ViewBag.Error = "Debe ingresar correo y contraseña.";
            return View();
        }

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo.Trim().ToLowerInvariant());
        
        if (usuario != null)
        {
            var verificacion = usuario.Contraseña.StartsWith("AQAAAA", StringComparison.Ordinal)
                ? _hasher.VerifyHashedPassword(usuario, usuario.Contraseña, contraseña)
                : PasswordVerificationResult.Failed;
            var legado = verificacion == PasswordVerificationResult.Failed &&
                (VerificarLegado(usuario.Contraseña, HashPassword(contraseña)) ||
                 VerificarLegado(usuario.Contraseña, contraseña));
            if (verificacion != PasswordVerificationResult.Failed || legado)
            {
                // Actualizar hashes antiguos (SHA256 o texto de datos demostrativos) al iniciar sesión.
                if (legado || verificacion == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    usuario.Contraseña = _hasher.HashPassword(usuario, contraseña);
                    await _context.SaveChangesAsync();
                }
            }
            if (verificacion != PasswordVerificationResult.Failed || legado)
            {
                HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
                return RedirectToAction("Index", "Home");
            }
        }

        ViewBag.Error = "Correo o contraseña incorrectos.";
        return View();
    }

    // GET: Cuenta/MiPerfil
    public async Task<IActionResult> MiPerfil()
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue)
            return RedirectToAction(nameof(Ingresar));

        var usuario = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);

        if (usuario == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Ingresar));
        }

        ViewBag.Resueltas = usuario.ObjetosPerdidos.Count(o => o.Estado == EstadoObjeto.Encontrado);
        ViewBag.BuenasAcciones = await _context.SolicitudesRecuperacion
            .CountAsync(s => s.ObjetoPerdido.UsuarioId == usuario.Id && s.Estado == EstadoSolicitud.Aceptada);
        return View(usuario);
    }

    // POST: Cuenta/CerrarSesion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CerrarSesion()
    {
        HttpContext.Session.Remove("UsuarioId");
        HttpContext.Session.Remove("UsuarioNombre");
        return RedirectToAction("Index", "Home");
    }

    private static bool VerificarLegado(string almacenada, string candidata)
    {
        var a = Encoding.UTF8.GetBytes(almacenada);
        var b = Encoding.UTF8.GetBytes(candidata);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    // Sólo compatibilidad de migración: las contraseñas nuevas usan PasswordHasher (PBKDF2).
    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}
