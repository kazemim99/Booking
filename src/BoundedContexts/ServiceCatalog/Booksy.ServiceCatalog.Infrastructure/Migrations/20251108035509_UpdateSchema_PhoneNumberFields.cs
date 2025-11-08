using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema_PhoneNumberFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryPhoneNationalNumber",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryPhoneNationalNumber",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RecipientPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: true),
                    TemplateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateData = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    GatewayMessageId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    CampaignId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BatchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OpenedFrom = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClickedLink = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OpenCount = table.Column<int>(type: "int", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SupportedChannels = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EmailSubjectTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailBodyTemplate = table.Column<string>(type: "text", nullable: true),
                    EmailPlainTextTemplate = table.Column<string>(type: "text", nullable: true),
                    SmsBodyTemplate = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: true),
                    PushTitleTemplate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PushBodyTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InAppTitleTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InAppBodyTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TemplateVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequiredVariables = table.Column<string>(type: "jsonb", nullable: false),
                    OptionalVariables = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValue: "en"),
                    LocalizedVersions = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.TemplateId);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnabledChannels = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EnabledTypes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    QuietHoursStart = table.Column<TimeOnly>(type: "time", nullable: true),
                    QuietHoursEnd = table.Column<TimeOnly>(type: "time", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValue: "en"),
                    MarketingOptIn = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MaxNotificationsPerDay = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveryAttempts",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GatewayMessageId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryDelaySeconds = table.Column<int>(type: "int", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveryAttempts_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Notifications",
                        principalColumn: "NotificationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryAttempts_NotificationId",
                schema: "ServiceCatalog",
                table: "NotificationDeliveryAttempts",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BookingId_Type",
                schema: "ServiceCatalog",
                table: "Notifications",
                columns: new[] { "BookingId", "Type" },
                filter: "\"BookingId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Channel",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ScheduledFor",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "ScheduledFor",
                filter: "\"ScheduledFor\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status_ScheduledFor",
                schema: "ServiceCatalog",
                table: "Notifications",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                schema: "ServiceCatalog",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_IsActive",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Key_Version",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                columns: new[] { "TemplateKey", "TemplateVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_TemplateKey",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                column: "TemplateKey");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Type_Active",
                schema: "ServiceCatalog",
                table: "NotificationTemplates",
                columns: new[] { "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_LastUpdated",
                schema: "ServiceCatalog",
                table: "UserNotificationPreferences",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId",
                schema: "ServiceCatalog",
                table: "UserNotificationPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeliveryAttempts",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "NotificationTemplates",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "ServiceCatalog");

            migrationBuilder.DropColumn(
                name: "PrimaryPhoneNationalNumber",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "SecondaryPhoneNationalNumber",
                schema: "ServiceCatalog",
                table: "Providers");
        }
    }
}
