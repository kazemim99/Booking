using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreDomainEvent4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceTiers_Services_ServiceId",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Providers_ProviderId",
                schema: "ServiceCatalog",
                table: "Services");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PriceTiers",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.DropIndex(
                name: "IX_PriceTiers_Active_SortOrder",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.DropIndex(
                name: "IX_PriceTiers_CreatedAt",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.DropIndex(
                name: "IX_PriceTiers_IsActive",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.DropIndex(
                name: "IX_PriceTiers_IsDefault",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.DropIndex(
                name: "IX_PriceTiers_ServiceId",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.DropIndex(
                name: "IX_PriceTiers_SortOrder",
                schema: "servicecatalog",
                table: "PriceTiers");

            migrationBuilder.RenameTable(
                name: "Services",
                schema: "ServiceCatalog",
                newName: "Services",
                newSchema: "servicecatalog");

            migrationBuilder.RenameTable(
                name: "PriceTiers",
                schema: "servicecatalog",
                newName: "ServicePriceTiers",
                newSchema: "servicecatalog");

            migrationBuilder.RenameColumn(
                name: "PriceCurrency",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "PriceAmount",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                newName: "Price");

            migrationBuilder.AlterColumn<int>(
                name: "MaxAdvanceBookingDays",
                schema: "servicecatalog",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 90,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 30);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "servicecatalog",
                table: "Services",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "servicecatalog",
                table: "Services",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ActivatedAt",
                schema: "servicecatalog",
                table: "Services",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId1",
                schema: "servicecatalog",
                table: "Services",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualifiedStaff",
                schema: "servicecatalog",
                table: "Services",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Attributes",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePriceTiers",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CreatedAt",
                schema: "servicecatalog",
                table: "Services",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProviderId1",
                schema: "servicecatalog",
                table: "Services",
                column: "ProviderId1");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Status_AllowOnlineBooking",
                schema: "servicecatalog",
                table: "Services",
                columns: new[] { "Status", "AllowOnlineBooking" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceTiers_ServiceId_IsActive",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                columns: new[] { "ServiceId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePriceTiers_Services_ServiceId",
                schema: "servicecatalog",
                table: "ServicePriceTiers",
                column: "ServiceId",
                principalSchema: "servicecatalog",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Providers_ProviderId",
                schema: "servicecatalog",
                table: "Services",
                column: "ProviderId",
                principalSchema: "ServiceCatalog",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Providers_ProviderId1",
                schema: "servicecatalog",
                table: "Services",
                column: "ProviderId1",
                principalSchema: "ServiceCatalog",
                principalTable: "Providers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServicePriceTiers_Services_ServiceId",
                schema: "servicecatalog",
                table: "ServicePriceTiers");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Providers_ProviderId",
                schema: "servicecatalog",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Providers_ProviderId1",
                schema: "servicecatalog",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_CreatedAt",
                schema: "servicecatalog",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ProviderId1",
                schema: "servicecatalog",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_Status_AllowOnlineBooking",
                schema: "servicecatalog",
                table: "Services");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePriceTiers",
                schema: "servicecatalog",
                table: "ServicePriceTiers");

            migrationBuilder.DropIndex(
                name: "IX_ServicePriceTiers_ServiceId_IsActive",
                schema: "servicecatalog",
                table: "ServicePriceTiers");

            migrationBuilder.DropColumn(
                name: "ProviderId1",
                schema: "servicecatalog",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "QualifiedStaff",
                schema: "servicecatalog",
                table: "Services");

            migrationBuilder.RenameTable(
                name: "Services",
                schema: "servicecatalog",
                newName: "Services",
                newSchema: "ServiceCatalog");

            migrationBuilder.RenameTable(
                name: "ServicePriceTiers",
                schema: "servicecatalog",
                newName: "PriceTiers",
                newSchema: "servicecatalog");

            migrationBuilder.RenameColumn(
                name: "Price",
                schema: "servicecatalog",
                table: "PriceTiers",
                newName: "PriceAmount");

            migrationBuilder.RenameColumn(
                name: "Currency",
                schema: "servicecatalog",
                table: "PriceTiers",
                newName: "PriceCurrency");

            migrationBuilder.AlterColumn<int>(
                name: "MaxAdvanceBookingDays",
                schema: "ServiceCatalog",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 90);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "ServiceCatalog",
                table: "Services",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "ServiceCatalog",
                table: "Services",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ActivatedAt",
                schema: "ServiceCatalog",
                table: "Services",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "servicecatalog",
                table: "PriceTiers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Attributes",
                schema: "servicecatalog",
                table: "PriceTiers",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PriceTiers",
                schema: "servicecatalog",
                table: "PriceTiers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PriceTiers_Active_SortOrder",
                schema: "servicecatalog",
                table: "PriceTiers",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceTiers_CreatedAt",
                schema: "servicecatalog",
                table: "PriceTiers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PriceTiers_IsActive",
                schema: "servicecatalog",
                table: "PriceTiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PriceTiers_IsDefault",
                schema: "servicecatalog",
                table: "PriceTiers",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_PriceTiers_ServiceId",
                schema: "servicecatalog",
                table: "PriceTiers",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceTiers_SortOrder",
                schema: "servicecatalog",
                table: "PriceTiers",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceTiers_Services_ServiceId",
                schema: "servicecatalog",
                table: "PriceTiers",
                column: "ServiceId",
                principalSchema: "ServiceCatalog",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Providers_ProviderId",
                schema: "ServiceCatalog",
                table: "Services",
                column: "ProviderId",
                principalSchema: "ServiceCatalog",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
