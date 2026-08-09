using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositTypeAndProviderBookingPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BookingPolicyDepositFixedAmount",
                schema: "ServiceCatalog",
                table: "Services",
                type: "numeric(18,2)",
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BookingPolicyDepositType",
                schema: "ServiceCatalog",
                table: "Services",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Percentage");

            migrationBuilder.AddColumn<bool>(
                name: "BookingPolicyAllowRescheduling",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BookingPolicyCancellationFeePercentage",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingPolicyCancellationWindowHours",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BookingPolicyDepositFixedAmount",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BookingPolicyDepositPercentage",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingPolicyDepositType",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingPolicyMaxAdvanceBookingDays",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingPolicyMinAdvanceBookingHours",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BookingPolicyRequireDeposit",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingPolicyRescheduleWindowHours",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PolicyDepositFixedAmount",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PolicyDepositType",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Percentage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingPolicyDepositFixedAmount",
                schema: "ServiceCatalog",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "BookingPolicyDepositType",
                schema: "ServiceCatalog",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "BookingPolicyAllowRescheduling",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyCancellationFeePercentage",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyCancellationWindowHours",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyDepositFixedAmount",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyDepositPercentage",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyDepositType",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyMaxAdvanceBookingDays",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyMinAdvanceBookingHours",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyRequireDeposit",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingPolicyRescheduleWindowHours",
                schema: "ServiceCatalog",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "PolicyDepositFixedAmount",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PolicyDepositType",
                schema: "ServiceCatalog",
                table: "Bookings");
        }
    }
}
