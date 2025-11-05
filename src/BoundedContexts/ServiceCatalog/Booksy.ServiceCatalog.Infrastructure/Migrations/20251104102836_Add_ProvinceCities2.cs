using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProvinceCities2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProvinceCities",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProvinceCode = table.Column<int>(type: "integer", nullable: false),
                    CityCode = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvinceCities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvinceCities_ProvinceCities_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "ProvinceCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_ParentId",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_ProvinceCode",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                column: "ProvinceCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_ProvinceCode_CityCode",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                columns: new[] { "ProvinceCode", "CityCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceCities_Type",
                schema: "ServiceCatalog",
                table: "ProvinceCities",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProvinceCities",
                schema: "ServiceCatalog");
        }
    }
}
