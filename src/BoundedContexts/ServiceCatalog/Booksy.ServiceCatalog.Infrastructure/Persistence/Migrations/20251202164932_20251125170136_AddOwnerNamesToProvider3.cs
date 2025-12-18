using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20251125170136_AddOwnerNamesToProvider3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "ServiceCatalog",
                table: "provider_gallery_images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "ServiceCatalog",
                table: "provider_gallery_images",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
