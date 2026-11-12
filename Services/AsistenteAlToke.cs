using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using GRUPAL.Data;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Services;

public class AsistenteAlToke(ApplicationDbContext db, IHttpClientFactory httpFactory,
    IConfiguration configuration, ILogger<AsistenteAlToke> logger)
{
    public async Task<(string Respuesta, IReadOnlyList<object> Objetos)> ResponderAsync(string pregunta, CancellationToken ct)
    {
        var consulta = pregunta.Trim();
        var comunes = new HashSet<string>(StringComparer.Ordinal)
        {
            "estoy", "buscando", "busco", "buscar", "quiero", "necesito", "objeto", "objetos",
            "perdi", "perdido", "perdidos", "encontre", "encontrado", "encontrados", "encontrar",
            "donde", "dejaron", "alguien", "tiene", "tienen", "ayuda", "favor", "puedes",
            "como", "para", "sobre", "esta", "unas", "unos", "una", "por", "las", "los",
            "del", "que", "con", "hay", "mis", "mi", "ver", "avisos", "aviso"
        };
        var tokens = Palabras(consulta).Where(t => !comunes.Contains(t)).Take(12).ToArray();
        var objetos = await db.ObjetosPerdidos.AsNoTracking().OrderByDescending(x => x.Fecha).Take(150).ToListAsync(ct);
        var relacionados = objetos.Select(o => new
        {
            Objeto = o,
            Titulo = Palabras(o.Título),
            Contenido = Palabras($"{o.Título} {o.Descripcion} {o.Ubicacion} {o.Categoría}")
        })
            .Where(x => tokens.Length > 0 && tokens.All(t => x.Contenido.Contains(t)))
            .OrderByDescending(x => tokens.Count(t => x.Titulo.Contains(t)))
            .ThenByDescending(x => x.Objeto.Fecha)
            .Take(4).Select(x => x.Objeto).ToList();
        var articulos = await db.ArticulosAyuda.AsNoTracking().ToListAsync(ct);
        var ayuda = articulos.Select(a => new
        {
            Articulo = a,
            Puntos = tokens.Count(t => a.Pregunta.Contains(t, StringComparison.OrdinalIgnoreCase)) * 2
                + tokens.Count(t => a.Respuesta.Contains(t, StringComparison.OrdinalIgnoreCase))
        }).Where(a => a.Puntos > 0).OrderByDescending(a => a.Puntos).Take(3)
            .Select(a => a.Articulo).ToList();
        var enlaces = relacionados.Select(o => (object)new { id = o.Id, titulo = o.Título, url = $"/Explorar/Detalle/{o.Id}" }).ToList();
        var contexto = string.Join("\n", relacionados.Select(o => $"#{o.Id}: {o.Título} / {o.Categoría} / {o.Ubicacion} / {o.Estado}"));
        var baseAyuda = ayuda.Any() ? string.Join("\n", ayuda.Select(a => $"{a.Pregunta}: {a.Respuesta}")) :
            "AlToke permite reportar y buscar objetos, guardar favoritos y enviar solicitudes de recuperación al autor de un aviso.";
        var apiKey = configuration["Groq:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                using var http = httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(15);
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var groqUrl = configuration["Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1/chat/completions";
                using var response = await http.PostAsJsonAsync(groqUrl, new
                {
                    model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile",
                    temperature = 0.2,
                    max_tokens = 280,
                    messages = new object[]
                    {
                        new { role = "system", content = "Eres AlToke, asistente de objetos perdidos de la comunidad USMP. Responde en español, de forma breve. Usa exclusivamente los avisos y guía proporcionados. No inventes hallazgos, contactos ni garantías; si no hay información, recomiéndales explorar y reportar. Nunca proporciones datos privados.\nAVISOS:\n" + contexto + "\nGUÍA:\n" + baseAyuda },
                        new { role = "user", content = consulta }
                    }
                }, ct);
                response.EnsureSuccessStatusCode();
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var texto = body.RootElement.GetProperty("choices")[0].GetProperty("message")
                    .GetProperty("content").GetString();
                if (!string.IsNullOrWhiteSpace(texto)) return (texto, enlaces);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Groq no disponible, se usa respuesta local.");
            }
        }
        // Respaldo local: el asistente funciona sin cuentas ni claves externas.
        if (relacionados.Count > 0)
            return ($"Encontré {relacionados.Count} aviso(s) que podrían ayudarte. Revisa sus detalles y contacta al autor desde la página del objeto. No puedo asegurar que sea el tuyo.", enlaces);
        if (ayuda.Count > 0) return (ayuda[0].Respuesta, enlaces);
        return ("Puedes explorar publicaciones, usar el buscador por categoría y ubicación o publicar tu objeto. Para solicitar una recuperación, abre un aviso e inicia sesión. ¿Qué objeto buscas?", enlaces);
    }
    private static HashSet<string> Palabras(string texto)
    {
        var normalizado = new string(texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();
        return Regex.Matches(normalizado, @"[a-z0-9]+").Select(m => m.Value)
            .Where(p => p.Length >= 3).ToHashSet(StringComparer.Ordinal);
    }

}
