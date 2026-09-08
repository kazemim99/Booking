using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProviderHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_join_requests",
                schema: "ServiceCatalog");

            migrationBuilder.DropIndex(
                name: "IX_Providers_Hierarchy_Independent",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Providers_HierarchyType",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Providers_ParentProviderId",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "HierarchyType",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "IsIndependent",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ParentProviderId",
                schema: "ServiceCatalog",
                table: "Providers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HierarchyType",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Organization");

            migrationBuilder.AddColumn<bool>(
                name: "IsIndependent",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentProviderId",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provider_join_requests",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_join_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Hierarchy_Independent",
                schema: "ServiceCatalog",
                table: "Providers",
                columns: new[] { "HierarchyType", "IsIndependent" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_HierarchyType",
                schema: "ServiceCatalog",
                table: "Providers",
                column: "HierarchyType");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ParentProviderId",
                schema: "ServiceCatalog",
                table: "Providers",
                column: "ParentProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderJoinRequests_OrganizationId",
                schema: "ServiceCatalog",
                table: "provider_join_requests",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderJoinRequests_OrgId_Status",
                schema: "ServiceCatalog",
                table: "provider_join_requests",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderJoinRequests_RequesterId",
                schema: "ServiceCatalog",
                table: "provider_join_requests",
                column: "requester_id");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderJoinRequests_Status",
                schema: "ServiceCatalog",
                table: "provider_join_requests",
                column: "status");
        }
    }
}
