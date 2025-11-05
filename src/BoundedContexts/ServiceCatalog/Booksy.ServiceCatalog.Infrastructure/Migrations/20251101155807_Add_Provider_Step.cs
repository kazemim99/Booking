using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Provider_Step : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRegistrationComplete",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationStep",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRegistrationComplete",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "RegistrationStep",
                schema: "ServiceCatalog",
                table: "Providers");
        }
    }
}
