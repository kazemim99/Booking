using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsanRezerve.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProviderCustomerId",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provider_customers",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_customers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ProviderCustomerId",
                schema: "ServiceCatalog",
                table: "Bookings",
                column: "ProviderCustomerId");

            migrationBuilder.CreateIndex(
                name: "ux_provider_customers_provider_phone",
                schema: "ServiceCatalog",
                table: "provider_customers",
                columns: new[] { "provider_id", "phone_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_customers",
                schema: "ServiceCatalog");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ProviderCustomerId",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProviderCustomerId",
                schema: "ServiceCatalog",
                table: "Bookings");
        }
    }
}
