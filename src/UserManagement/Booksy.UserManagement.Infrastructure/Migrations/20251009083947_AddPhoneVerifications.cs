using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneVerifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "user_management",
                table: "users",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalNumber",
                schema: "user_management",
                table: "users",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "user_management",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberVerified",
                schema: "user_management",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerifiedAt",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PhoneVerifications",
                schema: "user_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Phone number in E.164 format"),
                    CountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, comment: "ISO country code (e.g., DE, US, GB)"),
                    HashedCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Hashed OTP code (SHA256)"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, comment: "Expiration timestamp for the verification code"),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the phone number has been verified"),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Timestamp when verification was completed"),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of verification attempts made"),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3, comment: "Maximum allowed verification attempts"),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "IP address of the requester"),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "User agent of the requester"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'", comment: "Creation timestamp"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneVerifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_ExpiresAt",
                schema: "user_management",
                table: "PhoneVerifications",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_PhoneNumber",
                schema: "user_management",
                table: "PhoneVerifications",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_PhoneNumber_Status",
                schema: "user_management",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "IsVerified", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneVerifications",
                schema: "user_management");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "user_management",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NationalNumber",
                schema: "user_management",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "user_management",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumberVerified",
                schema: "user_management",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAt",
                schema: "user_management",
                table: "users");
        }
    }
}
