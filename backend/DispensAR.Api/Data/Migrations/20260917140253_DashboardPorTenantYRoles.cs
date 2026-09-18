using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DispensAR.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DashboardPorTenantYRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WidgetsDashboard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Tamano = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetsDashboard", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WidgetsDashboardOrganizaciones",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WidgetId = table.Column<int>(type: "int", nullable: false),
                    Habilitado = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetsDashboardOrganizaciones", x => new { x.TenantId, x.WidgetId });
                    table.ForeignKey(
                        name: "FK_WidgetsDashboardOrganizaciones_Organizaciones_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WidgetsDashboardOrganizaciones_WidgetsDashboard_WidgetId",
                        column: x => x.WidgetId,
                        principalTable: "WidgetsDashboard",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "WidgetsDashboard",
                columns: new[] { "Id", "Codigo", "Descripcion", "Tamano" },
                values: new object[,]
                {
                    { 1, "pacientes_activos", "Pacientes activos", "normal" },
                    { 2, "cantidad_dispensada", "Cantidad dispensada", "normal" },
                    { 3, "stock_disponible", "Stock disponible", "normal" },
                    { 4, "sucursales", "Sucursales", "normal" },
                    { 5, "alertas", "Alertas", "ancho" },
                    { 6, "ultimas_dispensaciones", "Últimas dispensaciones", "ancho" }
                });

            migrationBuilder.InsertData(
                table: "WidgetsDashboardOrganizaciones",
                columns: new[] { "TenantId", "WidgetId", "Habilitado", "Orden" },
                values: new object[,]
                {
                    { 1, 1, true, 0 },
                    { 1, 2, true, 1 },
                    { 1, 3, true, 2 },
                    { 1, 4, true, 3 },
                    { 1, 5, false, 4 },
                    { 1, 6, false, 5 },
                    { 2, 1, true, 0 },
                    { 2, 2, true, 1 },
                    { 2, 3, false, 2 },
                    { 2, 4, true, 3 },
                    { 2, 5, false, 4 },
                    { 2, 6, false, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WidgetsDashboard_Codigo",
                table: "WidgetsDashboard",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WidgetsDashboardOrganizaciones_WidgetId",
                table: "WidgetsDashboardOrganizaciones",
                column: "WidgetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WidgetsDashboardOrganizaciones");

            migrationBuilder.DropTable(
                name: "WidgetsDashboard");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Usuarios");
        }
    }
}
