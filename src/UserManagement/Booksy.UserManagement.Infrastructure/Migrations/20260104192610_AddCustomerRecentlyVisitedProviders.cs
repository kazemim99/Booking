using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.UserManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerRecentlyVisitedProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_recently_visited_providers",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    view_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_recently_visited_providers", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_recently_visited_providers_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "user_management",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_recently_visited_providers_customer_visited_at",
                schema: "user_management",
                table: "customer_recently_visited_providers",
                columns: new[] { "CustomerId", "visited_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_customer_recently_visited_providers_provider_id",
                schema: "user_management",
                table: "customer_recently_visited_providers",
                column: "provider_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_recently_visited_providers",
                schema: "user_management");
        }
    }
}
