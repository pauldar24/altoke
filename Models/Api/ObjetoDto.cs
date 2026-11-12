using System.ComponentModel.DataAnnotations;
namespace GRUPAL.Models.Api;

public class ObjetoDto
{
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public int Id { get; set; }
    [Required, StringLength(150)] public string Titulo { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Categoria { get; set; } = string.Empty;
    [Required, StringLength(3000)] public string Descripcion { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Ubicacion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public EstadoObjeto Estado { get; set; } = EstadoObjeto.Perdido;
    public string? FotoUrl { get; set; }
    public string? Autor { get; set; }
}
