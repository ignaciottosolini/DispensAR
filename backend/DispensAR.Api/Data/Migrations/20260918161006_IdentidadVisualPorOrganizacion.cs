using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispensAR.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdentidadVisualPorOrganizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentidadesVisuales",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    LogoPrincipalRuta = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    LogoPrincipalContenido = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LogoCompactoRuta = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    LogoCompactoContenido = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ColorPrimario = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    ColorSecundario = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    ColorSidebarFondo = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    ColorSidebarTexto = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentidadesVisuales", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_IdentidadesVisuales_Organizaciones_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentidadesVisuales");
        }
    }
}
