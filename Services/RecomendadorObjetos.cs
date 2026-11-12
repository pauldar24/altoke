using GRUPAL.Data;
using GRUPAL.Models;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Services;

/// <summary>Recomendaciones explicables basadas en categoría, ubicación, título y favoritos.
/// No se usan los pesos ni el dataset de productos de la tienda Demo.</summary>
public class RecomendadorObjetos(ApplicationDbContext db, ModeloCoincidenciasService coincidencias)
{
    public async Task<List<ObjetoPerdido>> RecomendarAsync(int? usuarioId, int? objetoId = null, int max = 6)
    {
        if (objetoId.HasValue)
        {
            var principal = await db.ObjetosPerdidos.AsNoTracking().FirstOrDefaultAsync(o => o.Id == objetoId);
            if (principal is null || principal.Estado != EstadoObjeto.Perdido ||
                principal.TipoPublicacion is not (TipoPublicacion.Dueno or TipoPublicacion.Hallazgo)) return new();
            var opuesto = principal.TipoPublicacion == TipoPublicacion.Dueno ? TipoPublicacion.Hallazgo : TipoPublicacion.Dueno;
            var candidatos = await db.ObjetosPerdidos.Include(o => o.Usuario).AsNoTracking()
                .Where(o => o.Estado == EstadoObjeto.Perdido && o.TipoPublicacion == opuesto && o.UsuarioId != principal.UsuarioId)
                .OrderByDescending(o => o.Fecha).Take(350).ToListAsync();
            return candidatos.Select(o => new { Objeto = o, Puntaje = coincidencias.Puntuar(principal, o) })
                .Where(x => x.Puntaje.HasValue).OrderByDescending(x => x.Puntaje).ThenByDescending(x => x.Objeto.Fecha)
                .Take(Math.Clamp(max, 1, 12)).Select(x => x.Objeto).ToList();
        }
        var objetos = await db.ObjetosPerdidos.Include(o => o.Usuario).AsNoTracking()
            .OrderByDescending(o => o.Fecha).Take(350).ToListAsync();
        var favoritas = usuarioId.HasValue
            ? await db.Favoritos.Where(f => f.UsuarioId == usuarioId.Value)
                .Select(f => f.ObjetoPerdidoId).ToListAsync()
            : new List<int>();
        var baseObjeto = objetos.FirstOrDefault(o => o.Id == objetoId);
        var preferencias = objetos.Where(o => favoritas.Contains(o.Id)).ToList();
        return objetos.Where(o => o.Id != objetoId && o.Estado == EstadoObjeto.Perdido && o.UsuarioId != usuarioId)
            .OrderByDescending(o => Puntuar(o, baseObjeto, preferencias))
            .ThenByDescending(o => o.Fecha)
            .Take(Math.Clamp(max, 1, 12)).ToList();
    }

    private static int Puntuar(ObjetoPerdido candidato, ObjetoPerdido? principal, List<ObjetoPerdido> favoritas)
    {
        var referencias = principal is null ? favoritas : favoritas.Append(principal);
        var puntos = 0;
        foreach (var objeto in referencias)
        {
            if (candidato.Categoría == objeto.Categoría) puntos += 8;
            if (candidato.Ubicacion.Equals(objeto.Ubicacion, StringComparison.OrdinalIgnoreCase)) puntos += 4;
            var tokens = objeto.Título.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 3).Distinct(StringComparer.OrdinalIgnoreCase);
            puntos += tokens.Count(t => candidato.Título.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        return puntos;
    }
}
