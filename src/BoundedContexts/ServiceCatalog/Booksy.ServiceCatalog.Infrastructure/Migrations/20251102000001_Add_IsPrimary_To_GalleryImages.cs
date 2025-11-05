using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_IsPrimary_To_GalleryImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                table: "provider_gallery_images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderGalleryImages_Provider_IsPrimary",
                table: "provider_gallery_images",
                columns: new[] { "provider_id", "is_primary" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderGalleryImages_Provider_IsPrimary",
                table: "provider_gallery_images");

            migrationBuilder.DropColumn(
                name: "is_primary",
                table: "provider_gallery_images");
        }
    }
}
