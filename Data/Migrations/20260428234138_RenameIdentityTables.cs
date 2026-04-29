using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeluCorte.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameIdentityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Peluquerias_PeluqueriaId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Peluqueros_AspNetUsers_UserId",
                table: "Peluqueros");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "UsuarioTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "UsuarioRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "UsuarioLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "UsuarioClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "RolClaims");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_PeluqueriaId",
                table: "Usuarios",
                newName: "IX_Usuarios_PeluqueriaId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "UsuarioRoles",
                newName: "IX_UsuarioRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "UsuarioLogins",
                newName: "IX_UsuarioLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "UsuarioClaims",
                newName: "IX_UsuarioClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "RolClaims",
                newName: "IX_RolClaims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioTokens",
                table: "UsuarioTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioRoles",
                table: "UsuarioRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioLogins",
                table: "UsuarioLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioClaims",
                table: "UsuarioClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolClaims",
                table: "RolClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Peluqueros_Usuarios_UserId",
                table: "Peluqueros",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RolClaims_Roles_RoleId",
                table: "RolClaims",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioClaims_Usuarios_UserId",
                table: "UsuarioClaims",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioLogins_Usuarios_UserId",
                table: "UsuarioLogins",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Roles_RoleId",
                table: "UsuarioRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Usuarios_UserId",
                table: "UsuarioRoles",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Peluquerias_PeluqueriaId",
                table: "Usuarios",
                column: "PeluqueriaId",
                principalTable: "Peluquerias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioTokens_Usuarios_UserId",
                table: "UsuarioTokens",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Peluqueros_Usuarios_UserId",
                table: "Peluqueros");

            migrationBuilder.DropForeignKey(
                name: "FK_RolClaims_Roles_RoleId",
                table: "RolClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioClaims_Usuarios_UserId",
                table: "UsuarioClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioLogins_Usuarios_UserId",
                table: "UsuarioLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Roles_RoleId",
                table: "UsuarioRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Usuarios_UserId",
                table: "UsuarioRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Peluquerias_PeluqueriaId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioTokens_Usuarios_UserId",
                table: "UsuarioTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioTokens",
                table: "UsuarioTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioRoles",
                table: "UsuarioRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioLogins",
                table: "UsuarioLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioClaims",
                table: "UsuarioClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolClaims",
                table: "RolClaims");

            migrationBuilder.RenameTable(
                name: "UsuarioTokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "UsuarioRoles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "UsuarioLogins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "UsuarioClaims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "RolClaims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_PeluqueriaId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_PeluqueriaId");

            migrationBuilder.RenameIndex(
                name: "IX_UsuarioRoles_RoleId",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UsuarioLogins_UserId",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UsuarioClaims_UserId",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RolClaims_RoleId",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Peluquerias_PeluqueriaId",
                table: "AspNetUsers",
                column: "PeluqueriaId",
                principalTable: "Peluquerias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Peluqueros_AspNetUsers_UserId",
                table: "Peluqueros",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
