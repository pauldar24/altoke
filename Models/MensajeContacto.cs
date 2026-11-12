using System.ComponentModel.DataAnnotations;
namespace GRUPAL.Models;

public class MensajeContacto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Ingresa tu nombre.")]
    [StringLength(100)] public string Nombre { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Correo { get; set; } = string.Empty;
    [Required, StringLength(140)] public string Asunto { get; set; } = string.Empty;
    [Required, StringLength(3000, MinimumLength = 10)] public string Mensaje { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public bool Atendido { get; set; }
    public bool Archivado { get; set; }
    public bool EsSpam { get; set; }
    [StringLength(30)] public string Clasificacion { get; set; } = "Sin clasificar";
    [StringLength(3000)] public string? Respuesta { get; set; }
    public DateTime? RespondidoEn { get; set; }
    [StringLength(20)] public string Sentimiento { get; set; } = "Neutro";
}
