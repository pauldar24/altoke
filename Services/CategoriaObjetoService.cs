using GRUPAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace GRUPAL.Services;

/// <summary>Clasificador multiclase para sugerir categorías según el título y la descripción.</summary>
public sealed class CategoriaObjetoService(
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment environment,
    ILogger<CategoriaObjetoService> logger) : IDisposable
{
    private readonly MLContext _ml = new(seed: 42);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private PredictionEngine<EntradaCategoria, SalidaCategoria>? _predictor;

    private string DirectorioModelo => environment.IsProduction()
        ? "/app/data/ml"
        : Path.Combine(environment.ContentRootPath, "ML", "Models");
    private string RutaModelo => Path.Combine(DirectorioModelo, "categoria-objetos-v3.zip");

    public async Task<string?> PredecirAsync(string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        await _lock.WaitAsync(ct);
        try
        {
            if (_predictor is null && !await CargarOEntrenarAsync(ct)) return null;
            return _predictor!.Predict(new EntradaCategoria { Texto = texto.Trim() }).Categoria;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "No fue posible sugerir una categoría.");
            return null;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> ReentrenarAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try { return await EntrenarAsync(ct); }
        finally { _lock.Release(); }
    }

    private async Task<bool> CargarOEntrenarAsync(CancellationToken ct)
    {
        if (File.Exists(RutaModelo))
        {
            try
            {
                var model = _ml.Model.Load(RutaModelo, out _);
                _predictor = _ml.Model.CreatePredictionEngine<EntradaCategoria, SalidaCategoria>(model);
                return true;
            }
            catch (Exception ex) { logger.LogWarning(ex, "El modelo de categorías no se pudo cargar; se reentrenará."); }
        }
        return await EntrenarAsync(ct);
    }

    private async Task<bool> EntrenarAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var avisos = await db.ObjetosPerdidos.AsNoTracking()
            .Select(o => new { o.Título, o.Descripcion, o.Categoría })
            .ToListAsync(ct);
        var filas = avisos
            .Where(o => !string.IsNullOrWhiteSpace(o.Título) && !string.IsNullOrWhiteSpace(o.Categoría))
            .Select(o => new EjemploCategoria
            {
                Texto = o.Título.Trim(),
                Categoria = o.Categoría.Trim()
            })
            .ToList();

        var basePath = Path.Combine(environment.ContentRootPath, "ML", "categorias_base.csv");
        if (File.Exists(basePath))
        {
            var categorias = new HashSet<string> { "Electrónicos", "Documentos", "Ropa", "Accesorios", "Llaves", "Mascotas", "Otros" };
            filas = filas.Where(f => categorias.Contains(f.Categoria)).ToList();
            filas.AddRange(File.ReadLines(basePath).Skip(1).Select(l => l.Split(';', 2))
                .Where(p => p.Length == 2 && categorias.Contains(p[0]))
                .Select(p => new EjemploCategoria { Categoria = p[0], Texto = p[1] }));
        }
        if (filas.Count < 10 || filas.Select(x => x.Categoria).Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
        {
            logger.LogInformation("Se necesitan al menos 10 avisos y dos categorías para entrenar el clasificador.");
            return false;
        }

        var datos = _ml.Data.LoadFromEnumerable(filas);
        var datosConEtiqueta = _ml.Transforms.Conversion.MapValueToKey("Label", nameof(EjemploCategoria.Categoria))
            .Fit(datos)
            .Transform(datos);
        var pipeline = _ml.Transforms.Text.FeaturizeText("Features", nameof(EjemploCategoria.Texto))
            .Append(_ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label", featureColumnName: "Features"))
            .Append(_ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
        var model = pipeline.Fit(datosConEtiqueta);

        _predictor?.Dispose();
        _predictor = _ml.Model.CreatePredictionEngine<EntradaCategoria, SalidaCategoria>(model);
        try
        {
            Directory.CreateDirectory(DirectorioModelo);
            var esquemaPrediccion = _ml.Data.LoadFromEnumerable(Array.Empty<EntradaCategoria>()).Schema;
            _ml.Model.Save(model, esquemaPrediccion, RutaModelo);
        }
        catch (Exception ex) { logger.LogWarning(ex, "El clasificador funciona en memoria, pero no se pudo guardar el archivo del modelo."); }

        logger.LogInformation("Modelo de categorías entrenado con {Filas} avisos y {Categorias} categorías.",
            filas.Count, filas.Select(x => x.Categoria).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        return true;
    }

    public void Dispose()
    {
        _predictor?.Dispose();
        _lock.Dispose();
    }

    public sealed class EjemploCategoria
    {
        public string Texto { get; set; } = "";
        public string Categoria { get; set; } = "";
    }

    public sealed class EntradaCategoria { public string Texto { get; set; } = ""; }

    public sealed class SalidaCategoria
    {
        [ColumnName("PredictedLabel")] public string Categoria { get; set; } = "";
    }
}

