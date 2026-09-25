using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsanRezerve.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountsAndCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountOwner",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DiscountPromotionId",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountTitle",
                schema: "ServiceCatalog",
                table: "Bookings",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "promotions",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    activation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    discount_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    max_discount_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    minimum_subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    new_customers_only = table.Column<bool>(type: "boolean", nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    daily_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    daily_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_usage_limit = table.Column<int>(type: "integer", nullable: true),
                    per_customer_limit = table.Column<int>(type: "integer", nullable: true),
                    redemption_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    paused_by_platform = table.Column<bool>(type: "boolean", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    service_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaign_enrollments",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "FK_campaign_enrollments_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "ServiceCatalog",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_redemptions",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    redeemed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "ServiceCatalog",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_enrollments_provider",
                schema: "ServiceCatalog",
                table: "campaign_enrollments",
                columns: new[] { "provider_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_campaign_enrollments_promotion_provider",
                schema: "ServiceCatalog",
                table: "campaign_enrollments",
                columns: new[] { "promotion_id", "provider_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_redemptions_promotion_customer",
                schema: "ServiceCatalog",
                table: "promotion_redemptions",
                columns: new[] { "promotion_id", "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_promotion_redemptions_applied_booking",
                schema: "ServiceCatalog",
                table: "promotion_redemptions",
                column: "booking_id",
                unique: true,
                filter: "status = 'Applied'");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_owner_status",
                schema: "ServiceCatalog",
                table: "promotions",
                columns: new[] { "owner", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_promotions_provider_status",
                schema: "ServiceCatalog",
                table: "promotions",
                columns: new[] { "provider_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_promotions_scope_code",
                schema: "ServiceCatalog",
                table: "promotions",
                columns: new[] { "owner", "provider_id", "code" },
                unique: true,
                filter: "code IS NOT NULL AND status <> 'Ended'")
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_enrollments",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "promotion_redemptions",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "promotions",
                schema: "ServiceCatalog");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DiscountOwner",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DiscountPromotionId",
                schema: "ServiceCatalog",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DiscountTitle",
                schema: "ServiceCatalog",
                table: "Bookings");
        }
    }
}
