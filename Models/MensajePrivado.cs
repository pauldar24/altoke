using System.ComponentModel.DataAnnotations;
namespace GRUPAL.Models;

public class MensajePrivado
{
    public int Id { get; set; }
    public int SolicitudRecuperacionId { get; set; }
    public SolicitudRecuperacion SolicitudRecuperacion { get; set; } = null!;
    public int AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;
    public Guid ClaveEnvio { get; set; }
    [MaxLength(1000)] public string Texto { get; set; } = "";
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
