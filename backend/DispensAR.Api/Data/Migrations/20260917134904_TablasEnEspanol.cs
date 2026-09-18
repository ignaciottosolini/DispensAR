using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispensAR.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TablasEnEspanol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Organizations_TenantId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_TenantId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Branches",
                table: "Branches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "PerfilesUsuarios");

            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "Organizaciones");

            migrationBuilder.RenameTable(
                name: "Branches",
                newName: "Sucursales");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "TokensUsuarios");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AccesosExternosUsuarios");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AtributosUsuarios");

            migrationBuilder.RenameIndex(
                name: "IX_Users_TenantId_Email",
                table: "PerfilesUsuarios",
                newName: "IX_PerfilesUsuarios_TenantId_Email");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "Usuarios",
                newName: "IX_Usuarios_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AccesosExternosUsuarios",
                newName: "IX_AccesosExternosUsuarios_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AtributosUsuarios",
                newName: "IX_AtributosUsuarios_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PerfilesUsuarios",
                table: "PerfilesUsuarios",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organizaciones",
                table: "Organizaciones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sucursales",
                table: "Sucursales",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TokensUsuarios",
                table: "TokensUsuarios",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccesosExternosUsuarios",
                table: "AccesosExternosUsuarios",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AtributosUsuarios",
                table: "AtributosUsuarios",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccesosExternosUsuarios_Usuarios_UserId",
                table: "AccesosExternosUsuarios",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AtributosUsuarios_Usuarios_UserId",
                table: "AtributosUsuarios",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PerfilesUsuarios_Organizaciones_TenantId",
                table: "PerfilesUsuarios",
                column: "TenantId",
                principalTable: "Organizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sucursales_Organizaciones_TenantId",
                table: "Sucursales",
                column: "TenantId",
                principalTable: "Organizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TokensUsuarios_Usuarios_UserId",
                table: "TokensUsuarios",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Organizaciones_TenantId",
                table: "Usuarios",
                column: "TenantId",
                principalTable: "Organizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccesosExternosUsuarios_Usuarios_UserId",
                table: "AccesosExternosUsuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_AtributosUsuarios_Usuarios_UserId",
                table: "AtributosUsuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_PerfilesUsuarios_Organizaciones_TenantId",
                table: "PerfilesUsuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Sucursales_Organizaciones_TenantId",
                table: "Sucursales");

            migrationBuilder.DropForeignKey(
                name: "FK_TokensUsuarios_Usuarios_UserId",
                table: "TokensUsuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Organizaciones_TenantId",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TokensUsuarios",
                table: "TokensUsuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sucursales",
                table: "Sucursales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PerfilesUsuarios",
                table: "PerfilesUsuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organizaciones",
                table: "Organizaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AtributosUsuarios",
                table: "AtributosUsuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccesosExternosUsuarios",
                table: "AccesosExternosUsuarios");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "TokensUsuarios",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "Sucursales",
                newName: "Branches");

            migrationBuilder.RenameTable(
                name: "PerfilesUsuarios",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Organizaciones",
                newName: "Organizations");

            migrationBuilder.RenameTable(
                name: "AtributosUsuarios",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AccesosExternosUsuarios",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_TenantId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PerfilesUsuarios_TenantId_Email",
                table: "Users",
                newName: "IX_Users_TenantId_Email");

            migrationBuilder.RenameIndex(
                name: "IX_AtributosUsuarios_UserId",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AccesosExternosUsuarios_UserId",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Branches",
                table: "Branches",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

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
                name: "FK_AspNetUsers_Organizations_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Organizations_TenantId",
                table: "Branches",
                column: "TenantId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
