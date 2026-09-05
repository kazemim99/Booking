using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestoreBookingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                schema: "ServiceCatalog",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ProviderId",
                schema: "ServiceCatalog",
                table: "Bookings",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StaffId",
                schema: "ServiceCatalog",
                table: "Bookings",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                schema: "ServiceCatalog",
                table: "Bookings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ProviderId",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StaffId",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                schema: "ServiceCatalog",
                table: "Bookings");
        }
    }
}
