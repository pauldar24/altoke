using System.ComponentModel.DataAnnotations;
namespace GRUPAL.Models;

public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int ObjetoPerdidoId { get; set; }
    public ObjetoPerdido? ObjetoPerdido { get; set; }
    public int? SolicitudRecuperacionId { get; set; }
    public int InformanteId { get; set; }
    [MaxLength(500)] public string Texto { get; set; } = "";
    public DateTime VistoEn { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public bool Leida { get; set; }
    public bool CorreoEnviado { get; set; }
}
