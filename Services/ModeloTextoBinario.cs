using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace GRUPAL.Services;

/// <summary>Infraestructura compartida. Cada instancia usa su propio dataset, entrenamiento y artefacto.</summary>
public sealed class ModeloTextoBinario(string nombre, IWebHostEnvironment env, ILogger logger) : IDisposable
{
    private readonly MLContext _ml = new(seed: 91);
    private readonly object _candado = new();
    private PredictionEngine<Fila, Salida>? _predictor;
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
                var ruta = Path.Combine(directorio, nombre + "-v1.zip");
                ITransformer modelo;
                if (File.Exists(ruta)) modelo = _ml.Model.Load(ruta, out _);
                else
                {
                    var entrenamiento = Leer(nombre + "-train.jsonl");
                    var evaluacion = Leer(nombre + "-eval.jsonl");
                    if (entrenamiento.Select(f => f.Label).Distinct().Count() != 2 || evaluacion.Select(f => f.Label).Distinct().Count() != 2)
                        throw new InvalidDataException("Se requieren ambas clases en entrenamiento y evaluación.");
                    var datos = _ml.Data.LoadFromEnumerable(entrenamiento);
                    var pipeline = _ml.Transforms.Text.FeaturizeText("Features", nameof(Fila.Texto))
                        .Append(_ml.BinaryClassification.Trainers.SdcaLogisticRegression());
                    modelo = pipeline.Fit(datos);
                    var metricas = _ml.BinaryClassification.Evaluate(modelo.Transform(_ml.Data.LoadFromEnumerable(evaluacion)));
                    logger.LogInformation("ML {Modelo} piloto: entrenamiento {Train}, evaluación separada {Eval}, precisión {Precision:F3}, exhaustividad {Recall:F3}, F1 {F1:F3}.",
                        nombre, entrenamiento.Count, evaluacion.Count, metricas.PositivePrecision, metricas.PositiveRecall, metricas.F1Score);
                    try { Directory.CreateDirectory(directorio); _ml.Model.Save(modelo, datos.Schema, ruta); }
                    catch (IOException) { logger.LogWarning("Modelo {Modelo} disponible en memoria.", nombre); }
                    catch (UnauthorizedAccessException) { logger.LogWarning("Modelo {Modelo} disponible en memoria; falta permiso de escritura.", nombre); }
                }
                _predictor = _ml.Model.CreatePredictionEngine<Fila, Salida>(modelo);
            }
            catch (Exception ex) { logger.LogError(ex, "Modelo {Modelo} no disponible.", nombre); }
        }
    }

    public Salida? Predecir(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        Preparar();
        lock (_candado) { return _predictor?.Predict(new Fila { Texto = texto }); }
    }
    private List<Fila> Leer(string archivo) => File.ReadLines(Path.Combine(env.ContentRootPath, "ML", "modelos", archivo))
        .Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => JsonSerializer.Deserialize<Fila>(l)!).ToList();
    public void Dispose() => _predictor?.Dispose();
    public sealed class Fila { public bool Label { get; set; } public string Texto { get; set; } = ""; }
    public sealed class Salida { [ColumnName("PredictedLabel")] public bool Positivo { get; set; } [ColumnName("Probability")] public float Probabilidad { get; set; } }
}
