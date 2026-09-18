using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispensAR.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AbmCatalogoElementos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "WidgetsDashboard",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoIndicador",
                table: "WidgetsDashboard",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "WidgetsDashboard",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsAdministradorPlataforma",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "WidgetsDashboard",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Activo", "TipoIndicador" },
                values: new object[] { true, "pacientes_activos" });

            migrationBuilder.UpdateData(
                table: "WidgetsDashboard",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Activo", "TipoIndicador" },
                values: new object[] { true, "cantidad_dispensada" });

            migrationBuilder.UpdateData(
                table: "WidgetsDashboard",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Activo", "TipoIndicador" },
                values: new object[] { true, "stock_disponible" });

            migrationBuilder.UpdateData(
                table: "WidgetsDashboard",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Activo", "TipoIndicador" },
                values: new object[] { true, "sucursales" });

            migrationBuilder.UpdateData(
                table: "WidgetsDashboard",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Activo", "TipoIndicador" },
                values: new object[] { true, "alertas" });

            migrationBuilder.UpdateData(
                table: "WidgetsDashboard",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Activo", "TipoIndicador" },
                values: new object[] { true, "ultimas_dispensaciones" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "WidgetsDashboard");

            migrationBuilder.DropColumn(
                name: "TipoIndicador",
                table: "WidgetsDashboard");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WidgetsDashboard");

            migrationBuilder.DropColumn(
                name: "EsAdministradorPlataforma",
                table: "Usuarios");
        }
    }
}
