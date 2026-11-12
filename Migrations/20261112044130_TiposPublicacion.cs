using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GRUPAL.Migrations
{
    /// <inheritdoc />
    public partial class TiposPublicacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoPublicacion",
                table: "ObjetosPerdidos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoPublicacion",
                table: "ObjetosPerdidos");
        }
    }
}
