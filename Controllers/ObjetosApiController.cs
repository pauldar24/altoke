using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

/// <summary>API de publicaciones AlToke. Lectura pública; escritura solo con sesión y antifalsificación.</summary>
[ApiController]
[Route("api/objetos")]
public class ObjetosApiController(ApplicationDbContext db, GRUPAL.Services.ModeracionService moderacion) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? buscar, [FromQuery] string? categoria,
        [FromQuery] EstadoObjeto? estado, [FromQuery] int pagina = 1, [FromQuery] int tamano = 12)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 50);
        var consulta = db.ObjetosPerdidos.AsNoTracking().Include(o => o.Usuario).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim().ToLower();
            consulta = consulta.Where(x => x.Título.ToLower().Contains(t) || x.Descripcion.ToLower().Contains(t) || x.Ubicacion.ToLower().Contains(t));
        }
        if (!string.IsNullOrWhiteSpace(categoria)) consulta = consulta.Where(x => x.Categoría == categoria);
        if (estado.HasValue) consulta = consulta.Where(x => x.Estado == estado);
        var total = await consulta.CountAsync();
        var datos = await consulta.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamano).Take(tamano).ToListAsync();
        return Ok(new { total, pagina, tamano, objetos = datos.Select(Mapear) });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var obj = await db.ObjetosPerdidos.AsNoTracking().Include(o => o.Usuario).FirstOrDefaultAsync(o => o.Id == id);
        return obj is null ? NotFound() : Ok(Mapear(obj));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([FromBody] ObjetoDto dto)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuario.HasValue) return Unauthorized(new { error = "Inicia sesión para publicar." });
        if (dto.Fecha == default || dto.Fecha.Date > DateTime.UtcNow.AddHours(-5).Date || !Enum.IsDefined(dto.Estado))
            return BadRequest(new { error = "La fecha debe ser válida y no puede ser futura." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var rechazo = await moderacion.ValidarObjetoAsync(dto.Titulo, dto.Descripcion);
        if (rechazo is not null) return BadRequest(new { error = rechazo });
        var obj = new ObjetoPerdido
        {
            Título = dto.Titulo.Trim(), Categoría = dto.Categoria.Trim(), Descripcion = dto.Descripcion.Trim(),
            Ubicacion = dto.Ubicacion.Trim(), Fecha = dto.Fecha, Estado = dto.Estado, UsuarioId = usuario.Value
        };
        db.ObjetosPerdidos.Add(obj);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Detalle), new { id = obj.Id }, Mapear(obj));
    }

    [HttpPut("{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [FromBody] ObjetoDto dto)
    {
        var obj = await db.ObjetosPerdidos.FindAsync(id);
        if (obj is null) return NotFound();
        if (!Autorizado(obj.UsuarioId)) return Unauthorized(new { error = "No tienes permiso para editar este aviso." });
        if (dto.Fecha == default || dto.Fecha.Date > DateTime.UtcNow.AddHours(-5).Date || !Enum.IsDefined(dto.Estado))
            return BadRequest(new { error = "La fecha debe ser válida y no puede ser futura." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var rechazo = await moderacion.ValidarObjetoAsync(dto.Titulo, dto.Descripcion);
        if (rechazo is not null) return BadRequest(new { error = rechazo });
        obj.Título = dto.Titulo.Trim();
        obj.Categoría = dto.Categoria.Trim();
        obj.Descripcion = dto.Descripcion.Trim();
        obj.Ubicacion = dto.Ubicacion.Trim();
        obj.Fecha = dto.Fecha;
        obj.Estado = dto.Estado;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var obj = await db.ObjetosPerdidos.FindAsync(id);
        if (obj is null) return NotFound();
        if (!Autorizado(obj.UsuarioId)) return Unauthorized(new { error = "No tienes permiso para borrar este aviso." });
        db.ObjetosPerdidos.Remove(obj);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private bool Autorizado(int propietario) =>
        HttpContext.Session.GetInt32("UsuarioId") == propietario || HttpContext.Session.GetString("IsAdmin") == "true";

    private static ObjetoDto Mapear(ObjetoPerdido o) => new()
    {
        Id = o.Id, Titulo = o.Título, Categoria = o.Categoría, Descripcion = o.Descripcion,
        Ubicacion = o.Ubicacion, Fecha = o.Fecha, Estado = o.Estado, FotoUrl = o.FotoUrl, Autor = o.Usuario?.Nombre
    };
}
