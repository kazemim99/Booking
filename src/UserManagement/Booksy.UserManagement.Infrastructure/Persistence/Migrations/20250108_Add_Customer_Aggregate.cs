// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Migrations/20250108_Add_Customer_Aggregate.cs
// ========================================
using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Booksy.UserManagement.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to add Customer aggregate and supporting tables
    /// </summary>
    public partial class Add_Customer_Aggregate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add customer_id column to user_profiles table (nullable for backward compatibility)
            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                schema: "user_management",
                table: "user_profiles",
                type: "uuid",
                nullable: true);

            // Create Customers table
            migrationBuilder.CreateTable(
                name: "customers",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_customers_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "user_management",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create CustomerFavoriteProviders table
            migrationBuilder.CreateTable(
                name: "customer_favorite_providers",
                schema: "user_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_customers_user_id",
                schema: "user_management",
                table: "customers",
                column: "user_id",
                unique: true);

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
                name: "ix_customer_favorite_providers_provider_id",
                schema: "user_management",
                table: "customer_favorite_providers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_favorite_providers_CustomerId",
                schema: "user_management",
                table: "customer_favorite_providers",
                column: "CustomerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop CustomerFavoriteProviders table
            migrationBuilder.DropTable(
                name: "customer_favorite_providers",
                schema: "user_management");

            // Drop Customers table
            migrationBuilder.DropTable(
                name: "customers",
                schema: "user_management");

            // Remove customer_id column from user_profiles
            migrationBuilder.DropColumn(
                name: "customer_id",
                schema: "user_management",
                table: "user_profiles");
        }
    }
}
