using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                schema: "user_management",
                table: "user_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_customer_id",
                schema: "user_management",
                table: "user_profiles",
                column: "customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_profiles_customer_id",
                schema: "user_management",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "customer_id",
                schema: "user_management",
                table: "user_profiles");
        }
    }
}
