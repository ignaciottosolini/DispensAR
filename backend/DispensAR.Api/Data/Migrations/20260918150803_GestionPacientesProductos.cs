using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispensAR.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class GestionPacientesProductos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Dni = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AutorizacionHasta = table.Column<DateOnly>(type: "date", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Pacientes_Organizaciones_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Productos_Organizaciones_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lotes",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lotes", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Lotes_Productos_TenantId_ProductoId",
                        columns: x => new { x.TenantId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_TenantId_ProductoId_Codigo",
                table: "Lotes",
                columns: new[] { "TenantId", "ProductoId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_TenantId_Dni",
                table: "Pacientes",
                columns: new[] { "TenantId", "Dni" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TenantId_Codigo",
                table: "Productos",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lotes");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Productos");
        }
    }
}
