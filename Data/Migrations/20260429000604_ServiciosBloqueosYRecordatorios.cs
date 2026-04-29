using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeluCorte.Data.Migrations
{
    /// <inheritdoc />
    public partial class ServiciosBloqueosYRecordatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Peluquerias_PeluqueriaId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Citas_PeluqueroId_Fecha_Hora",
                table: "Citas");

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "Usuarios",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<int>(
                name: "DuracionMinutos",
                table: "Citas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordatorioEnviadoEl",
                table: "Citas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServicioId",
                table: "Citas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bloqueos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeluqueroId = table.Column<Guid>(type: "uuid", nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Fin = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloqueos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bloqueos_Peluqueros_PeluqueroId",
                        column: x => x.PeluqueroId,
                        principalTable: "Peluqueros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeluqueriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicios_Peluquerias_PeluqueriaId",
                        column: x => x.PeluqueriaId,
                        principalTable: "Peluquerias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PeluqueroId_Fecha_Hora",
                table: "Citas",
                columns: new[] { "PeluqueroId", "Fecha", "Hora" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_ServicioId",
                table: "Citas",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueos_PeluqueroId_Inicio",
                table: "Bloqueos",
                columns: new[] { "PeluqueroId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_PeluqueriaId",
                table: "Servicios",
                column: "PeluqueriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_Servicios_ServicioId",
                table: "Citas",
                column: "ServicioId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Peluquerias_PeluqueriaId",
                table: "Usuarios",
                column: "PeluqueriaId",
                principalTable: "Peluquerias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_Servicios_ServicioId",
                table: "Citas");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Peluquerias_PeluqueriaId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Bloqueos");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropIndex(
                name: "IX_Citas_PeluqueroId_Fecha_Hora",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "IX_Citas_ServicioId",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "DuracionMinutos",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "RecordatorioEnviadoEl",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "ServicioId",
                table: "Citas");

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "Usuarios",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PeluqueroId_Fecha_Hora",
                table: "Citas",
                columns: new[] { "PeluqueroId", "Fecha", "Hora" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Peluquerias_PeluqueriaId",
                table: "Usuarios",
                column: "PeluqueriaId",
                principalTable: "Peluquerias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
