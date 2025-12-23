using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Staff",
                schema: "ServiceCatalog");

            migrationBuilder.DropIndex(
                name: "IX_Providers_Type",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "CategoryDescription",
                schema: "ServiceCatalog",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CategoryIconUrl",
                schema: "ServiceCatalog",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                schema: "ServiceCatalog",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ProviderType",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "ServiceCatalog",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryCategory",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_PrimaryCategory",
                schema: "ServiceCatalog",
                table: "Providers",
                column: "PrimaryCategory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Providers_PrimaryCategory",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "ServiceCatalog",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PrimaryCategory",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.AddColumn<string>(
                name: "CategoryDescription",
                schema: "ServiceCatalog",
                table: "Services",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryIconUrl",
                schema: "ServiceCatalog",
                table: "Services",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                schema: "ServiceCatalog",
                table: "Services",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderType",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Staff",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Biography = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    ProfilePhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Type",
                schema: "ServiceCatalog",
                table: "Providers",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_CreatedAt",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Email",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_IsActive",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Name",
                schema: "ServiceCatalog",
                table: "Staff",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_ProviderId",
                schema: "ServiceCatalog",
                table: "Staff",
                column: "ProviderId");
        }
    }
}
