using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserCollections3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_authentication_sessions_users_user_id",
                schema: "user_management",
                table: "authentication_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_user_id",
                schema: "user_management",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_expires_at",
                schema: "user_management",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_revoked_at",
                schema: "user_management",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_authentication_sessions_ended_at",
                schema: "user_management",
                table: "authentication_sessions");

            migrationBuilder.DropIndex(
                name: "ix_authentication_sessions_expires_at",
                schema: "user_management",
                table: "authentication_sessions");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "user_management",
                table: "refresh_tokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "user_management",
                table: "refresh_tokens",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "user_management",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "user_agent",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "device_name",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "DeviceName");

            migrationBuilder.RenameColumn(
                name: "device_id",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "device_info");

            migrationBuilder.RenameIndex(
                name: "ix_authentication_sessions_user_id",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "IX_authentication_sessions_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_authentication_sessions_token",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "ix_sessions_token");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                schema: "user_management",
                table: "authentication_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceName",
                schema: "user_management",
                table: "authentication_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "device_info",
                schema: "user_management",
                table: "authentication_sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_authentication_sessions_users_UserId",
                schema: "user_management",
                table: "authentication_sessions",
                column: "UserId",
                principalSchema: "user_management",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                schema: "user_management",
                table: "refresh_tokens",
                column: "UserId",
                principalSchema: "user_management",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_authentication_sessions_users_UserId",
                schema: "user_management",
                table: "authentication_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                schema: "user_management",
                table: "refresh_tokens");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "user_management",
                table: "refresh_tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "user_management",
                table: "refresh_tokens",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "user_management",
                table: "refresh_tokens",
                newName: "ix_refresh_tokens_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "device_info",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "device_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "user_agent");

            migrationBuilder.RenameColumn(
                name: "DeviceName",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "device_name");

            migrationBuilder.RenameIndex(
                name: "ix_sessions_token",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "ix_authentication_sessions_token");

            migrationBuilder.RenameIndex(
                name: "IX_authentication_sessions_UserId",
                schema: "user_management",
                table: "authentication_sessions",
                newName: "ix_authentication_sessions_user_id");

            migrationBuilder.AlterColumn<string>(
                name: "device_id",
                schema: "user_management",
                table: "authentication_sessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                schema: "user_management",
                table: "authentication_sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "device_name",
                schema: "user_management",
                table: "authentication_sessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                schema: "user_management",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_revoked_at",
                schema: "user_management",
                table: "refresh_tokens",
                column: "revoked_at");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_sessions_ended_at",
                schema: "user_management",
                table: "authentication_sessions",
                column: "ended_at");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_sessions_expires_at",
                schema: "user_management",
                table: "authentication_sessions",
                column: "expires_at");

            migrationBuilder.AddForeignKey(
                name: "FK_authentication_sessions_users_user_id",
                schema: "user_management",
                table: "authentication_sessions",
                column: "user_id",
                principalSchema: "user_management",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_user_id",
                schema: "user_management",
                table: "refresh_tokens",
                column: "user_id",
                principalSchema: "user_management",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
