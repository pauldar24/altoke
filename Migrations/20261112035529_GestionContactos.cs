using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GRUPAL.Migrations
{
    /// <inheritdoc />
    public partial class GestionContactos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Archivado",
                table: "MensajesContacto",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Clasificacion",
                table: "MensajesContacto",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EsSpam",
                table: "MensajesContacto",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondidoEn",
                table: "MensajesContacto",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Respuesta",
                table: "MensajesContacto",
                type: "TEXT",
                maxLength: 3000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Archivado",
                table: "MensajesContacto");

            migrationBuilder.DropColumn(
                name: "Clasificacion",
                table: "MensajesContacto");

            migrationBuilder.DropColumn(
                name: "EsSpam",
                table: "MensajesContacto");

            migrationBuilder.DropColumn(
                name: "RespondidoEn",
                table: "MensajesContacto");

            migrationBuilder.DropColumn(
                name: "Respuesta",
                table: "MensajesContacto");
        }
    }
}
