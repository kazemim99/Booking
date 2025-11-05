using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProviderCityIdandProviceId1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddressCityId",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressFormattedAddress",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AddressProvinceId",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressCityId",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "AddressFormattedAddress",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "AddressProvinceId",
                schema: "ServiceCatalog",
                table: "Providers");
        }
    }
}
