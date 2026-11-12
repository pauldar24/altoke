using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GRUPAL.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesAvistamientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    ObjetoPerdidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    InformanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    VistoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Leida = table.Column<bool>(type: "INTEGER", nullable: false),
                    CorreoEnviado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_ObjetosPerdidos_ObjetoPerdidoId",
                        column: x => x.ObjetoPerdidoId,
                        principalTable: "ObjetosPerdidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_ObjetoPerdidoId",
                table: "Notificaciones",
                column: "ObjetoPerdidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId_InformanteId_ObjetoPerdidoId_Texto",
                table: "Notificaciones",
                columns: new[] { "UsuarioId", "InformanteId", "ObjetoPerdidoId", "Texto" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificaciones");
        }
    }
}
