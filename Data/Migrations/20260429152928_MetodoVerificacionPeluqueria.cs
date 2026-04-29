using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeluCorte.Data.Migrations
{
    /// <inheritdoc />
    public partial class MetodoVerificacionPeluqueria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetodoVerificacion",
                table: "Peluquerias",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetodoVerificacion",
                table: "Peluquerias");
        }
    }
}
