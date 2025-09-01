using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SerUserIdLoginAttempt3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_login_attempts_users_user_id",
                schema: "user_management",
                table: "login_attempts");

            migrationBuilder.DropIndex(
                name: "ix_login_attempts_attempted_at",
                schema: "user_management",
                table: "login_attempts");

            migrationBuilder.DropIndex(
                name: "ix_login_attempts_is_successful",
                schema: "user_management",
                table: "login_attempts");

            migrationBuilder.DropIndex(
                name: "ix_login_attempts_user_id",
                schema: "user_management",
                table: "login_attempts");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "user_management",
                table: "login_attempts",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "is_successful",
                schema: "user_management",
                table: "login_attempts",
                newName: "was_successful");

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_user_attempted",
                schema: "user_management",
                table: "login_attempts",
                columns: new[] { "UserId", "attempted_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_login_attempts_users_UserId",
                schema: "user_management",
                table: "login_attempts",
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
                name: "FK_login_attempts_users_UserId",
                schema: "user_management",
                table: "login_attempts");

            migrationBuilder.DropIndex(
                name: "ix_login_attempts_user_attempted",
                schema: "user_management",
                table: "login_attempts");

            migrationBuilder.RenameColumn(
                name: "was_successful",
                schema: "user_management",
                table: "login_attempts",
                newName: "is_successful");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "user_management",
                table: "login_attempts",
                newName: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_attempted_at",
                schema: "user_management",
                table: "login_attempts",
                column: "attempted_at");

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_is_successful",
                schema: "user_management",
                table: "login_attempts",
                column: "is_successful");

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_user_id",
                schema: "user_management",
                table: "login_attempts",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_login_attempts_users_user_id",
                schema: "user_management",
                table: "login_attempts",
                column: "user_id",
                principalSchema: "user_management",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
