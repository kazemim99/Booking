using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Gallery2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_provider_gallery_images_Providers_BusinessProfileProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images");

            migrationBuilder.DropIndex(
                name: "IX_provider_gallery_images_BusinessProfileProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images");

            migrationBuilder.DropColumn(
                name: "BusinessProfileProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderGalleryImages_ProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                column: "provider_id");

            migrationBuilder.AddForeignKey(
                name: "FK_provider_gallery_images_Providers_provider_id",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                column: "provider_id",
                principalSchema: "ServiceCatalog",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_provider_gallery_images_Providers_provider_id",
                schema: "ServiceCatalog",
                table: "provider_gallery_images");

            migrationBuilder.DropIndex(
                name: "IX_ProviderGalleryImages_ProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "ServiceCatalog",
                table: "provider_gallery_images");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessProfileProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_provider_gallery_images_BusinessProfileProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                column: "BusinessProfileProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_provider_gallery_images_Providers_BusinessProfileProviderId",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                column: "BusinessProfileProviderId",
                principalSchema: "ServiceCatalog",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
