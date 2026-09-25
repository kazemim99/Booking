using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsanRezerve.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffProfilePhotoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_url",
                schema: "ServiceCatalog",
                table: "staff_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_url",
                schema: "ServiceCatalog",
                table: "staff_profiles");
        }
    }
}
