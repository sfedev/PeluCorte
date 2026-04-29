using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeluCorte.Data.Migrations
{
    /// <inheritdoc />
    public partial class DiasAbiertosBitmask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasAbiertosBitmask",
                table: "Peluquerias",
                type: "integer",
                nullable: false,
                defaultValue: 127);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasAbiertosBitmask",
                table: "Peluquerias");
        }
    }
}
