using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeluCorte.Data.Migrations
{
    /// <inheritdoc />
    public partial class HorariosPorDia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Horarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeluqueriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false),
                    Abierto = table.Column<bool>(type: "boolean", nullable: false),
                    Apertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Cierre = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Horarios_Peluquerias_PeluqueriaId",
                        column: x => x.PeluqueriaId,
                        principalTable: "Peluquerias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_PeluqueriaId_Dia",
                table: "Horarios",
                columns: new[] { "PeluqueriaId", "Dia" },
                unique: true);

            // Sembrar 7 horarios por cada peluquería existente, derivados del horario global
            // y el bitmask de días que tenían antes de este modelo.
            // Mapping DayOfWeek (Sun=0, Mon=1, ..., Sat=6) -> índice bitmask (Lun=0, ..., Dom=6)
            // bitmaskIdx = (dia + 6) % 7
            migrationBuilder.Sql(@"
                INSERT INTO ""Horarios"" (""Id"", ""PeluqueriaId"", ""Dia"", ""Abierto"", ""Apertura"", ""Cierre"")
                SELECT gen_random_uuid(), p.""Id"", d.dia,
                       (p.""DiasAbiertosBitmask"" & (1 << ((d.dia + 6) % 7))) <> 0,
                       p.""HoraApertura"",
                       p.""HoraCierre""
                FROM ""Peluquerias"" p
                CROSS JOIN (VALUES (0), (1), (2), (3), (4), (5), (6)) AS d(dia)
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Horarios"" h
                    WHERE h.""PeluqueriaId"" = p.""Id"" AND h.""Dia"" = d.dia
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Horarios");
        }
    }
}
