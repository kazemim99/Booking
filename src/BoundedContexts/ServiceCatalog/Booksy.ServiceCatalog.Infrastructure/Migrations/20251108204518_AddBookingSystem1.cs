using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSystem1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Authority",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardPan",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeeCurrency",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentUrl",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefNumber",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Authority",
                schema: "ServiceCatalog",
                table: "Payments",
                column: "Authority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Authority",
                schema: "ServiceCatalog",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Authority",
                schema: "ServiceCatalog",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CardPan",
                schema: "ServiceCatalog",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Fee",
                schema: "ServiceCatalog",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FeeCurrency",
                schema: "ServiceCatalog",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentUrl",
                schema: "ServiceCatalog",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefNumber",
                schema: "ServiceCatalog",
                table: "Payments");
        }
    }
}
