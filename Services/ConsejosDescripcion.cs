using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GRUPAL.Services;

/// <summary>Presenta consejos contextuales. La predicción binaria sigue perteneciendo al modelo ML.NET.</summary>
public static class ConsejosDescripcion
{
    public static string Crear(bool suficiente, string descripcion, string? titulo, string? ubicacion)
    {
        var texto = Normalizar($"{titulo} {descripcion}");
        var presentes = new List<string>();
        var pendientes = new List<string>();
        void Registrar(bool presente, string nombre, string consejo)
        {
            if (presente) presentes.Add(nombre); else pendientes.Add(consejo);
        }
        Registrar(Regex.IsMatch(texto, @"\b(negr[oa]s?|blanc[oa]s?|azul(?:es)?|roj[oa]s?|verde?s?|gris(?:es)?|marron(?:es)?|amarill[oa]s?|rosad[oa]s?|rosa|beige|violeta|morad[oa]s?|naranja|platead[oa]s?|dorad[oa]s?)\b"),
            "color", "el color, si lo recuerdas");
        Registrar(Regex.IsMatch(texto, @"\b(totto|adidas|nike|sony|samsung|apple|iphone|jbl|lenovo|casio|contigo|stanley|puma|nissan|toyota|motorola|xiaomi|bose|oakley|columbia|renzo costa|ray ban)\b|\bmarca\s+(?!desconocida\b|no\b|sin\b|ni\b|del\b|de\b)[a-z0-9]+"),
            "marca o modelo", "la marca o el modelo, si los conoces");
        Registrar(Regex.IsMatch(texto, @"\b(bolsillo|bolsillos|cierre|cierres|broche|pegatina|sticker|llavero|rayadura|grieta|mancha|capucha|correa|estuche|tapa|asa|cordones|talla|dibujo|etiqueta|espiral|rasgun[oó]n)\b"),
            "rasgos particulares", "algún rasgo distintivo, como bolsillos, cierre, sticker, tamaño o una marca de uso");
        Registrar(!string.IsNullOrWhiteSpace(ubicacion) || Regex.IsMatch(texto, @"\b(biblioteca|cafeteria|aula|laboratorio|gimnasio|patio|parque|entrada|cancha|cancha?s|estacionamiento|pabellon|escaleras|jardines)\b"),
            "ubicación", "la ubicación aproximada donde se perdió o encontró");

        var resumen = presentes.Count > 0 ? $"Ya mencionas {string.Join(", ", presentes)}. " : "";
        if (pendientes.Count == 0)
            return resumen + (suficiente ? "La descripción aporta detalles útiles. Comprueba que sean correctos." :
                "El modelo aún la considera mejorable. Revisa que los detalles sean claros; esta valoración es orientativa.");
        return resumen + (suficiente ? "Para hacer el aviso más preciso, puedes añadir " : "Podrías añadir ")
            + string.Join("; ", pendientes) + ". Comparte solo información que conozcas y evita datos personales.";
    }

    private static string Normalizar(string texto) => new string(texto.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
}
