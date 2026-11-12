namespace GRUPAL.Services;

/// <summary>Detecta posible spam/abuso, independientemente de que un mensaje exprese satisfacción o una queja.</summary>
public sealed class ModeloSpamService(IWebHostEnvironment env, ILogger<ModeloSpamService> logger) : IDisposable
{
    private readonly ModeloTextoBinario _modelo = new("spam", env, logger, 0.85f);
    public void Preparar() => _modelo.Preparar();
    public bool? EsSpam(string texto)
    {
        var resultado = _modelo.Predecir(texto);
        return resultado is null ? null : resultado.Positivo && resultado.Probabilidad >= 0.85f;
    }
    public void Dispose() => _modelo.Dispose();
}
