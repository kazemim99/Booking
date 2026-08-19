using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceEventId",
                schema: "ServiceCatalog",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    GatewayMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => new { x.EventId, x.Channel, x.Recipient });
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_CreatedAt",
                schema: "ServiceCatalog",
                table: "NotificationDeliveries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationId",
                schema: "ServiceCatalog",
                table: "NotificationDeliveries",
                column: "NotificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeliveries",
                schema: "ServiceCatalog");

            migrationBuilder.DropColumn(
                name: "SourceEventId",
                schema: "ServiceCatalog",
                table: "Notifications");
        }
    }
}
