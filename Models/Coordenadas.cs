namespace GRUPAL.Models;

public static class Coordenadas
{
    public static bool Validas(double? latitud, double? longitud) =>
        (!latitud.HasValue && !longitud.HasValue) ||
        (latitud.HasValue && longitud.HasValue && double.IsFinite(latitud.Value) && double.IsFinite(longitud.Value)
         && latitud.Value >= -90 && latitud.Value <= 90 && longitud.Value >= -180 && longitud.Value <= 180);
}
