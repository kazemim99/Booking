using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsDelted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_store_user_id",
                schema: "user_management",
                table: "event_store");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "user_management",
                table: "event_store");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "user_management",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "user_management",
                table: "user_roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "user_management",
                table: "user_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_roles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                schema: "user_management",
                table: "user_roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "user_management",
                table: "user_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "user_management",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_profiles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                schema: "user_management",
                table: "user_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                schema: "user_management",
                table: "event_store",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "user_management",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "user_management",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "user_management",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "user_management",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                schema: "user_management",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "user_management",
                table: "event_store",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                schema: "user_management",
                table: "event_store",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_store_user_id",
                schema: "user_management",
                table: "event_store",
                column: "user_id");
        }
    }
}
