namespace GRUPAL.Services;

/// <summary>Evalúa si la descripción aporta características útiles para reconocer un objeto. No comprueba la veracidad del aviso.</summary>
public sealed class ModeloCalidadAvisoService(IWebHostEnvironment env, ILogger<ModeloCalidadAvisoService> logger) : IDisposable
{
    private readonly ModeloTextoBinario _modelo = new("calidad", env, logger);
    public void Preparar() => _modelo.Preparar();
    public bool? TieneDetalle(string descripcion) => _modelo.Predecir(descripcion)?.Positivo;
    public void Dispose() => _modelo.Dispose();
}
