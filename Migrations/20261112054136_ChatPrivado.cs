using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GRUPAL.Migrations
{
    /// <inheritdoc />
    public partial class ChatPrivado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsChat",
                table: "Notificaciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MensajesPrivados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudRecuperacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaveEnvio = table.Column<Guid>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajesPrivados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensajesPrivados_SolicitudesRecuperacion_SolicitudRecuperacionId",
                        column: x => x.SolicitudRecuperacionId,
                        principalTable: "SolicitudesRecuperacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensajesPrivados_Usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MensajesPrivados_AutorId",
                table: "MensajesPrivados",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_MensajesPrivados_SolicitudRecuperacionId_AutorId_ClaveEnvio",
                table: "MensajesPrivados",
                columns: new[] { "SolicitudRecuperacionId", "AutorId", "ClaveEnvio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensajesPrivados");

            migrationBuilder.DropColumn(
                name: "EsChat",
                table: "Notificaciones");
        }
    }
}
