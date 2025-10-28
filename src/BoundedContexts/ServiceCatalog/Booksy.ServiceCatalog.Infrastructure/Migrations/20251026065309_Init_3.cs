using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderHolidays_Providers_ProviderId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays");

            migrationBuilder.DropIndex(
                name: "IX_ProviderHolidays_ProviderId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays");

            migrationBuilder.AddColumn<Guid>(
                name: "_providerId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHolidays__providerId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "_providerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderHolidays_Providers__providerId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "_providerId",
                principalSchema: "ServiceCatalog",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderHolidays_Providers__providerId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays");

            migrationBuilder.DropIndex(
                name: "IX_ProviderHolidays__providerId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays");

            migrationBuilder.DropColumn(
                name: "_providerId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHolidays_ProviderId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderHolidays_Providers_ProviderId",
                schema: "ServiceCatalog",
                table: "ProviderHolidays",
                column: "ProviderId",
                principalSchema: "ServiceCatalog",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
