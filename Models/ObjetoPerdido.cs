namespace GRUPAL.Models;

public class ObjetoPerdido
{
    public int Id { get; set; }
    public string Título { get; set; } = string.Empty;
    public string Categoría { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public TipoPublicacion TipoPublicacion { get; set; }
    public string TipoVisible => TipoPublicacion switch { TipoPublicacion.Dueno => "Publicado por el dueño · Busca su objeto", TipoPublicacion.Hallazgo => "Objeto hallado · Busca a su dueño", _ => "Tipo de publicación pendiente de revisar" };
    public string EstadoVisible => Estado == EstadoObjeto.Perdido ? "Abierto" : "Resuelto";
    public EstadoObjeto Estado { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public string? FotoUrl { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}