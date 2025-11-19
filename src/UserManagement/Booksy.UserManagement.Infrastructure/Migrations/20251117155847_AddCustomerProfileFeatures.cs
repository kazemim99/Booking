using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerProfileFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "registered_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "password_reset_token_expires_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "password_reset_token_created_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "locked_until",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_password_change_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_login_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "deactivated_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "activation_token_expires_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "activation_token_created_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "activated_at",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PhoneVerifiedAt",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "user_management",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "assigned_at",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_of_birth",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "revoked_at",
                schema: "user_management",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "verified_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Timestamp when verification was completed",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldComment: "Timestamp when verification was completed");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_sent_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Timestamp when OTP was last sent",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldComment: "Timestamp when OTP was last sent");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_attempt_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Timestamp of last verification attempt",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldComment: "Timestamp of last verification attempt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Expiration timestamp for the verification code",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldComment: "Expiration timestamp for the verification code");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Creation timestamp",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldComment: "Creation timestamp");

            migrationBuilder.AlterColumn<DateTime>(
                name: "blocked_until",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Timestamp until which verification is blocked",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldComment: "Timestamp until which verification is blocked");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "attempted_at",
                schema: "user_management",
                table: "login_attempts",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                schema: "user_management",
                table: "event_store",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified_at",
                schema: "user_management",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<bool>(
                name: "notification_email_enabled",
                schema: "user_management",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "notification_reminder_timing",
                schema: "user_management",
                table: "customers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "24h");

            migrationBuilder.AddColumn<bool>(
                name: "notification_sms_enabled",
                schema: "user_management",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "added_at",
                schema: "user_management",
                table: "customer_favorite_providers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "customer_favorite_providers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "user_management",
                table: "customer_favorite_providers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "started_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_activity_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ended_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_booking_history",
                schema: "user_management");

            migrationBuilder.DropColumn(
                name: "notification_email_enabled",
                schema: "user_management",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "notification_reminder_timing",
                schema: "user_management",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "notification_sms_enabled",
                schema: "user_management",
                table: "customers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "registered_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "password_reset_token_expires_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "password_reset_token_created_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "locked_until",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_password_change_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_login_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "deactivated_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "activation_token_expires_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "activation_token_created_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "activated_at",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PhoneVerifiedAt",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "user_management",
                table: "users",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "assigned_at",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_of_birth",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "revoked_at",
                schema: "user_management",
                table: "refresh_tokens",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "refresh_tokens",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "refresh_tokens",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "verified_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp without time zone",
                nullable: true,
                comment: "Timestamp when verification was completed",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Timestamp when verification was completed");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_sent_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp without time zone",
                nullable: true,
                comment: "Timestamp when OTP was last sent",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Timestamp when OTP was last sent");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_attempt_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp without time zone",
                nullable: true,
                comment: "Timestamp of last verification attempt",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Timestamp of last verification attempt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp without time zone",
                nullable: false,
                comment: "Expiration timestamp for the verification code",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Expiration timestamp for the verification code");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp without time zone",
                nullable: false,
                comment: "Creation timestamp",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Creation timestamp");

            migrationBuilder.AlterColumn<DateTime>(
                name: "blocked_until",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp without time zone",
                nullable: true,
                comment: "Timestamp until which verification is blocked",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Timestamp until which verification is blocked");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "phone_verifications",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "attempted_at",
                schema: "user_management",
                table: "login_attempts",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                schema: "user_management",
                table: "event_store",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified_at",
                schema: "user_management",
                table: "customers",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "user_management",
                table: "customers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "added_at",
                schema: "user_management",
                table: "customer_favorite_providers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "customer_favorite_providers",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "user_management",
                table: "customer_favorite_providers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "started_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_activity_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ended_at",
                schema: "user_management",
                table: "authentication_sessions",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
