using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifyServiceOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOptions_IsActive",
                schema: "ServiceCatalog",
                table: "ServiceOptions");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOptions_ServiceId",
                schema: "ServiceCatalog",
                table: "ServiceOptions");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_ServiceId_IsActive",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                columns: new[] { "ServiceId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOptions_ServiceId_IsActive",
                schema: "ServiceCatalog",
                table: "ServiceOptions");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_IsActive",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_ServiceId",
                schema: "ServiceCatalog",
                table: "ServiceOptions",
                column: "ServiceId");
        }
    }
}
