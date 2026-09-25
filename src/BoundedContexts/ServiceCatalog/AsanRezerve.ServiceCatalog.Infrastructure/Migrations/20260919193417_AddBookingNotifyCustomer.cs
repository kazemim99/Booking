using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsanRezerve.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingNotifyCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyCustomer",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyCustomer",
                schema: "ServiceCatalog",
                table: "Bookings");
        }
    }
}
