using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeluCorte.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToPeluquero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Peluqueros",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Peluqueros_UserId",
                table: "Peluqueros",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Peluqueros_AspNetUsers_UserId",
                table: "Peluqueros",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Peluqueros_AspNetUsers_UserId",
                table: "Peluqueros");

            migrationBuilder.DropIndex(
                name: "IX_Peluqueros_UserId",
                table: "Peluqueros");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Peluqueros");
        }
    }
}
