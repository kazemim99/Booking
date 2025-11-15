using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "ProviderAvailability",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    AvailabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HoldExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderAvailability", x => x.AvailabilityId);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RatingValue = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ProviderResponse = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProviderResponseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HelpfulCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NotHelpfulCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ReviewId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAvailability_BookingId",
                schema: "ServiceCatalog",
                table: "ProviderAvailability",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAvailability_Date_Status",
                schema: "ServiceCatalog",
                table: "ProviderAvailability",
                columns: new[] { "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAvailability_HoldExpiration_Status",
                schema: "ServiceCatalog",
                table: "ProviderAvailability",
                columns: new[] { "HoldExpiresAt", "Status" },
                filter: "\"HoldExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAvailability_Provider_Date_StartTime",
                schema: "ServiceCatalog",
                table: "ProviderAvailability",
                columns: new[] { "ProviderId", "Date", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                schema: "ServiceCatalog",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                schema: "ServiceCatalog",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Provider_CreatedAt",
                schema: "ServiceCatalog",
                table: "Reviews",
                columns: new[] { "ProviderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Provider_Rating",
                schema: "ServiceCatalog",
                table: "Reviews",
                columns: new[] { "ProviderId", "RatingValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProviderId",
                schema: "ServiceCatalog",
                table: "Reviews",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Verified_CreatedAt",
                schema: "ServiceCatalog",
                table: "Reviews",
                columns: new[] { "IsVerified", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderAvailability",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Reviews",
                schema: "ServiceCatalog");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                schema: "ServiceCatalog",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
