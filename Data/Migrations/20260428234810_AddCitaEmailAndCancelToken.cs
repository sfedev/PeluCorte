using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeluCorte.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCitaEmailAndCancelToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelToken",
                table: "Citas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Citas",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Citas\" SET \"CancelToken\" = REPLACE(gen_random_uuid()::text, '-', '') WHERE \"CancelToken\" = '' OR \"CancelToken\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_CancelToken",
                table: "Citas",
                column: "CancelToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Citas_CancelToken",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "CancelToken",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Citas");
        }
    }
}
