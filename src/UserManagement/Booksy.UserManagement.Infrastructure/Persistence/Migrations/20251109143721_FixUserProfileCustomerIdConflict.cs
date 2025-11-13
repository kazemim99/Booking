using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUserProfileCustomerIdConflict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneVerifications",
                schema: "user_management");

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                schema: "user_management",
                table: "user_profiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "phone_verifications",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Phone number in international format"),
                    country_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, comment: "Country code (e.g., +98, +1)"),
                    national_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "National number without country code"),
                    otp_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "SHA256 hashed OTP code"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Current status of verification"),
                    method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Verification method (SMS, Call, WhatsApp)"),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Purpose of verification (Registration, Login, etc.)"),
                    send_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of times OTP was sent"),
                    verification_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of verification attempts made"),
                    max_verification_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5, comment: "Maximum allowed verification attempts"),
                    last_sent_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Timestamp when OTP was last sent"),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Timestamp of last verification attempt"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, comment: "Creation timestamp"),
                    expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, comment: "Expiration timestamp for the verification code"),
                    verified_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Timestamp when verification was completed"),
                    blocked_until = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Timestamp until which verification is blocked"),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "IP address of the requester"),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "User agent of the requester"),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Session identifier"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_verifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_favorite_providers",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_favorite_providers", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_favorite_providers_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "user_management",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_favorite_providers_CustomerId",
                schema: "user_management",
                table: "customer_favorite_providers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "ix_customer_favorite_providers_provider_id",
                schema: "user_management",
                table: "customer_favorite_providers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_created_at",
                schema: "user_management",
                table: "customers",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_customers_is_active",
                schema: "user_management",
                table: "customers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_customers_user_id",
                schema: "user_management",
                table: "customers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_phone_verifications_created_at",
                schema: "user_management",
                table: "phone_verifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_phone_verifications_expires_at",
                schema: "user_management",
                table: "phone_verifications",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_phone_verifications_phone_number",
                schema: "user_management",
                table: "phone_verifications",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_phone_verifications_status",
                schema: "user_management",
                table: "phone_verifications",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_favorite_providers",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "phone_verifications",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "user_management");

            migrationBuilder.DropColumn(
                name: "phone_number",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.CreateTable(
                name: "PhoneVerifications",
                schema: "user_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of verification attempts made"),
                    CountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, comment: "ISO country code (e.g., DE, US, GB)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'", comment: "Creation timestamp"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, comment: "Expiration timestamp for the verification code"),
                    HashedCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Hashed OTP code (SHA256)"),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "IP address of the requester"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the phone number has been verified"),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3, comment: "Maximum allowed verification attempts"),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Phone number in E.164 format"),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "User agent of the requester"),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Timestamp when verification was completed")
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
    }
}
