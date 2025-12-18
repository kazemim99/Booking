using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "provider_invitations",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    phone_country_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    phone_national_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    invitee_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_by_provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "provider_join_requests",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    review_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
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
                name: "IX_ProviderInvitations_ExpiresAt",
                schema: "ServiceCatalog",
                table: "provider_invitations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_OrganizationId",
                schema: "ServiceCatalog",
                table: "provider_invitations",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_Status",
                schema: "ServiceCatalog",
                table: "provider_invitations",
                column: "status");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_invitations",
                schema: "ServiceCatalog");

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
    }
}
