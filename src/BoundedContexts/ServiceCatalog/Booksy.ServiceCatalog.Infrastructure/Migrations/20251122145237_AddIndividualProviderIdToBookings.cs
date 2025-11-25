using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndividualProviderIdToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IndividualProviderId",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_IndividualProviderId",
                schema: "ServiceCatalog",
                table: "Bookings",
                column: "IndividualProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_IndividualProviderId",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IndividualProviderId",
                schema: "ServiceCatalog",
                table: "Bookings");
        }
    }
}
