using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user_management");

            migrationBuilder.CreateTable(
                name: "customer_booking_history",
                schema: "user_management",
                columns: table => new
                {
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    service_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_booking_history", x => x.booking_id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_sms_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notification_email_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notification_reminder_timing = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "24h"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_store",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    aggregate_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_data = table.Column<string>(type: "jsonb", nullable: false),
                    event_version = table.Column<long>(type: "bigint", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_store", x => x.id);
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
                    last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when OTP was last sent"),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Timestamp of last verification attempt"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Creation timestamp"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Expiration timestamp for the verification code"),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when verification was completed"),
                    blocked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Timestamp until which verification is blocked"),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "IP address of the requester"),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "User agent of the requester"),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Session identifier"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_verifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    NationalNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    PhoneNumberVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_password_change_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activation_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    activation_token_created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activation_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_reset_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    password_reset_token_created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_reset_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_favorite_providers",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "authentication_sessions",
                schema: "user_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    device_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceName = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authentication_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_authentication_sessions_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "user_management",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_attempts",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    was_successful = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_login_attempts_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "user_management",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "user_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    replaced_by_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "user_management",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    phone_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    phone_national_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    alternate_phone_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    alternate_phone_national_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    preferences = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "user_management",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "user_management",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authentication_sessions_UserId",
                schema: "user_management",
                table: "authentication_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_token",
                schema: "user_management",
                table: "authentication_sessions",
                column: "session_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_booking_history_customer_time",
                schema: "user_management",
                table: "customer_booking_history",
                columns: new[] { "customer_id", "start_time" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_customer_booking_history_provider",
                schema: "user_management",
                table: "customer_booking_history",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_booking_history_status",
                schema: "user_management",
                table: "customer_booking_history",
                column: "status");

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
                name: "ix_event_store_aggregate_type",
                schema: "user_management",
                table: "event_store",
                column: "aggregate_type");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_aggregate_version",
                schema: "user_management",
                table: "event_store",
                columns: new[] { "aggregate_id", "event_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_store_event_type",
                schema: "user_management",
                table: "event_store",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_timestamp",
                schema: "user_management",
                table: "event_store",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_user_attempted",
                schema: "user_management",
                table: "login_attempts",
                columns: new[] { "UserId", "attempted_at" });

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

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                schema: "user_management",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "user_management",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_name",
                schema: "user_management",
                table: "user_profiles",
                columns: new[] { "first_name", "last_name" });

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_user_id",
                schema: "user_management",
                table: "user_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_expires_at",
                schema: "user_management",
                table: "user_roles",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_name",
                schema: "user_management",
                table: "user_roles",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id_name",
                schema: "user_management",
                table: "user_roles",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "user_management",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_registered_at",
                schema: "user_management",
                table: "users",
                column: "registered_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                schema: "user_management",
                table: "users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_users_type",
                schema: "user_management",
                table: "users",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authentication_sessions",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "customer_booking_history",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "customer_favorite_providers",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "event_store",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "login_attempts",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "phone_verifications",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "user_management");

            migrationBuilder.DropTable(
                name: "users",
                schema: "user_management");
        }
    }
}
