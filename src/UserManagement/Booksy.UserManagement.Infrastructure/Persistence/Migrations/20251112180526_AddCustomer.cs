using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "phone_number",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                schema: "user_management",
                table: "user_profiles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_id",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                schema: "user_management",
                table: "user_profiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
