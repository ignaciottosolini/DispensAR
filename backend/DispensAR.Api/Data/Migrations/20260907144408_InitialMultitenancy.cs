using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DispensAR.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMultitenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Branches_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Users_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { "acme", "ACME" },
                    { "demo", "DispensAR Demo" }
                });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "TenantId", "Name" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "acme", "Sucursal Sur" },
                    { new Guid("10000000-0000-0000-0000-000000000001"), "demo", "Sucursal Centro" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "demo", "Sucursal Norte" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "TenantId", "DisplayName", "Email" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), "acme", "Usuario ACME", "acme@example.test" },
                    { new Guid("30000000-0000-0000-0000-000000000001"), "demo", "Usuario Demo", "demo@example.test" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
