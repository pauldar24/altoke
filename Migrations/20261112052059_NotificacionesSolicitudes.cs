using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GRUPAL.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesSolicitudes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SolicitudRecuperacionId",
                table: "Notificaciones",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.Sql("""
                INSERT INTO Notificaciones (UsuarioId, ObjetoPerdidoId, InformanteId, Texto, VistoEn, CreadoEn, Leida, CorreoEnviado, SolicitudRecuperacionId)
                SELECT o.UsuarioId, s.ObjetoPerdidoId, s.SolicitanteId, 'Solicitud pendiente sobre tu publicación: ' || o.Título,
                       s.CreadoEn, s.CreadoEn, 0, 0, s.Id
                FROM SolicitudesRecuperacion s JOIN ObjetosPerdidos o ON o.Id = s.ObjetoPerdidoId
                WHERE s.Estado = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SolicitudRecuperacionId",
                table: "Notificaciones");
        }
    }
}
