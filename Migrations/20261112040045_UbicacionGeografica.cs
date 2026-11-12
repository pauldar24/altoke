using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GRUPAL.Migrations
{
    /// <inheritdoc />
    public partial class UbicacionGeografica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "ObjetosPerdidos",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "ObjetosPerdidos",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "ObjetosPerdidos");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "ObjetosPerdidos");
        }
    }
}
