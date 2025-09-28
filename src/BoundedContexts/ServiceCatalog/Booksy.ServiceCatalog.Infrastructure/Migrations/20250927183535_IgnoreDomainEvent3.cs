using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreDomainEvent3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceTiers_ServiceId_IsActive",
                schema: "ServiceCatalog",
                table: "PriceTiers");

            migrationBuilder.RenameTable(
                name: "PriceTiers",
                schema: "ServiceCatalog",
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

            migrationBuilder.RenameColumn(
                name: "PriceTierId",
                schema: "servicecatalog",
                table: "PriceTiers",
                newName: "Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceId",
                schema: "servicecatalog",
                table: "ServiceOptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
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
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'::jsonb");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "PriceTiers",
                schema: "servicecatalog",
                newName: "PriceTiers",
                newSchema: "ServiceCatalog");

            migrationBuilder.RenameColumn(
                name: "PriceCurrency",
                schema: "ServiceCatalog",
                table: "PriceTiers",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "PriceAmount",
                schema: "ServiceCatalog",
                table: "PriceTiers",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "ServiceCatalog",
                table: "PriceTiers",
                newName: "PriceTierId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceId",
                schema: "servicecatalog",
                table: "ServiceOptions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "ServiceCatalog",
                table: "PriceTiers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Dictionary<string, string>>(
                name: "Attributes",
                schema: "ServiceCatalog",
                table: "PriceTiers",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceTiers_ServiceId_IsActive",
                schema: "ServiceCatalog",
                table: "PriceTiers",
                columns: new[] { "ServiceId", "IsActive" });
        }
    }
}
