using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GRUPAL.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace GRUPAL.Services;

/// <summary>Clasificador binario de pares perdido/hallazgo. Sus datos iniciales son ejemplos redactados, no recuperaciones verificadas.</summary>
public sealed class ModeloCoincidenciasService(IWebHostEnvironment env, ILogger<ModeloCoincidenciasService> logger) : IDisposable
{
    private readonly MLContext _ml = new(seed: 73);
    private readonly object _candado = new();
    private PredictionEngine<Entrada, Salida>? _predictor;
    private bool _preparado;

    public void Preparar()
    {
        lock (_candado)
        {
            if (_preparado) return;
            _preparado = true;
            try
            {
                var directorio = env.IsProduction() ? "/app/data/ml" : Path.Combine(env.ContentRootPath, "ML", "Models");
                var ruta = Path.Combine(directorio, "coincidencias-v2.zip");
                ITransformer modelo;
                if (File.Exists(ruta)) modelo = _ml.Model.Load(ruta, out _);
                else
                {
                    var entrenamiento = Leer("coincidencias-train.jsonl");
                    var evaluacion = Leer("coincidencias-eval.jsonl");
                    var datos = _ml.Data.LoadFromEnumerable(entrenamiento);
                    var pipeline = _ml.Transforms.Concatenate("Features", nameof(Entrada.Titulo), nameof(Entrada.Descripcion),
                        nameof(Entrada.Categoria), nameof(Entrada.Ubicacion), nameof(Entrada.ConflictoColor))
                        .Append(_ml.BinaryClassification.Trainers.SdcaLogisticRegression());
                    modelo = pipeline.Fit(datos);
                    using var evaluador = _ml.Model.CreatePredictionEngine<Entrada, Salida>(modelo);
                    var resultados = evaluacion.Select(e => (Real: e.Label, Pred: evaluador.Predict(e).Probabilidad >= 0.65f)).ToList();
                    var tp = resultados.Count(r => r.Real && r.Pred);
                    var fp = resultados.Count(r => !r.Real && r.Pred);
                    var fn = resultados.Count(r => r.Real && !r.Pred);
                    var precision = tp / (double)Math.Max(1, tp + fp);
                    var recall = tp / (double)Math.Max(1, tp + fn);
                    var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);
                    logger.LogInformation("ML coincidencias piloto: entrenamiento {Train}, evaluación separada {Eval}, umbral 0.65, precisión {Precision:F3}, exhaustividad {Recall:F3}, F1 {F1:F3}.",
                        entrenamiento.Count, evaluacion.Count, precision, recall, f1);
                    try { Directory.CreateDirectory(directorio); _ml.Model.Save(modelo, datos.Schema, ruta); }
                    catch (IOException) { logger.LogWarning("El modelo de coincidencias funcionará en memoria."); }
                    catch (UnauthorizedAccessException) { logger.LogWarning("Sin permisos para guardar el modelo de coincidencias; se usa memoria."); }
                }
                _predictor = _ml.Model.CreatePredictionEngine<Entrada, Salida>(modelo);
            }
            catch (Exception ex) { logger.LogError(ex, "Modelo de coincidencias no disponible."); }
        }
    }

    public float? Puntuar(ObjetoPerdido a, ObjetoPerdido b)
    {
        Preparar();
        lock (_candado)
        {
            if (_predictor is null) return null;
            var entrada = CrearEntrada(a.Título, a.Descripcion, a.Categoría, a.Ubicacion, b.Título, b.Descripcion, b.Categoría, b.Ubicacion);
            // Exigir una referencia textual común para evitar sugerir solo por compartir categoría o lugar.
            if (entrada.Titulo == 0 && entrada.Descripcion == 0) return null;
            var resultado = _predictor.Predict(entrada);
            return resultado.EsCoincidencia && resultado.Probabilidad >= 0.65f ? resultado.Probabilidad : null;
        }
    }

    private List<Entrada> Leer(string archivo) => File.ReadLines(Path.Combine(env.ContentRootPath, "ML", "modelos", archivo))
        .Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => JsonSerializer.Deserialize<Par>(l)!)
        .Select(p => { var e = CrearEntrada(p.TituloA, p.DescripcionA, p.CategoriaA, p.UbicacionA,
            p.TituloB, p.DescripcionB, p.CategoriaB, p.UbicacionB); e.Label = p.Label; return e; }).ToList();

    private static Entrada CrearEntrada(string ta, string da, string ca, string ua, string tb, string db, string cb, string ub) => new()
    {
        Titulo = Similitud(ta, tb), Descripcion = Similitud(ta + " " + da, tb + " " + db),
        Categoria = ca.Equals(cb, StringComparison.OrdinalIgnoreCase) ? 1 : 0, Ubicacion = Similitud(ua, ub), ConflictoColor = ColorIncompatible(ta, tb) ? 1 : 0
    };

    private static bool ColorIncompatible(string a, string b)
    {
        static HashSet<string> Colores(string texto) =>
            Regex.Matches(texto.ToLowerInvariant(), @"\b(negr|blanc|azul|roj|gris|marr|verde|amarill|rosad)\w*")
                .Select(m => m.Groups[1].Value).ToHashSet();
        var x = Colores(a); var y = Colores(b);
        return x.Count > 0 && y.Count > 0 && !x.Overlaps(y);
    }

    private static float Similitud(string a, string b)
    {
        static HashSet<string> Tokens(string texto)
        {
            var normal = new string(texto.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
            return Regex.Matches(normal, "[a-z0-9]+").Select(m => m.Value)
                .Where(t => t.Length >= 3 && t is not ("perdi" or "perdido" or "encontre" or "encontrado" or "objeto" or "una" or "con" or "del" or "los" or "las"))
                .ToHashSet(StringComparer.Ordinal);
        }
        var x = Tokens(a); var y = Tokens(b);
        var union = x.Union(y).Count();
        return union == 0 ? 0 : x.Intersect(y).Count() / (float)union;
    }

    public void Dispose() => _predictor?.Dispose();
    public sealed class Entrada { public bool Label { get; set; } public float Titulo { get; set; } public float Descripcion { get; set; } public float Categoria { get; set; } public float Ubicacion { get; set; } public float ConflictoColor { get; set; } }
    public sealed class Salida { [ColumnName("PredictedLabel")] public bool EsCoincidencia { get; set; } [ColumnName("Probability")] public float Probabilidad { get; set; } }
    public sealed class Par
    {
        public bool Label { get; set; }
        public string TituloA { get; set; } = ""; public string DescripcionA { get; set; } = "";
        public string CategoriaA { get; set; } = ""; public string UbicacionA { get; set; } = "";
        public string TituloB { get; set; } = ""; public string DescripcionB { get; set; } = "";
        public string CategoriaB { get; set; } = ""; public string UbicacionB { get; set; } = "";
    }
}
