using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsanRezerve.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewModerationAndVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CleanlinessRating",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "numeric(3,1)",
                precision: 3,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConductRating",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "numeric(3,1)",
                precision: 3,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstPublishedAt",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HelpfulVoteCount",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAt",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedBy",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationStatus",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<int>(
                name: "NotHelpfulVoteCount",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PunctualityRating",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "numeric(3,1)",
                precision: 3,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplyModerationReason",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplyModerationStatus",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SkillRating",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "numeric(3,1)",
                precision: 3,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublishedReviewCount",
                schema: "ServiceCatalog",
                table: "Providers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProviderRatingSummaries",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CleanlinessAverage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    CleanlinessCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SkillAverage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    SkillCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PunctualityAverage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    PunctualityCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ConductAverage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    ConductCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderRatingSummaries", x => x.ProviderId);
                });

            migrationBuilder.CreateTable(
                name: "ReviewReports",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    ReviewReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewReports", x => x.ReviewReportId);
                    table.ForeignKey(
                        name: "FK_ReviewReports_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Reviews",
                        principalColumn: "ReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewVotes",
                schema: "ServiceCatalog",
                columns: table => new
                {
                    ReviewVoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHelpful = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewVotes", x => x.ReviewVoteId);
                    table.ForeignKey(
                        name: "FK_ReviewVotes_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalSchema: "ServiceCatalog",
                        principalTable: "Reviews",
                        principalColumn: "ReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ModerationStatus_CreatedAt",
                schema: "ServiceCatalog",
                table: "Reviews",
                columns: new[] { "ModerationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Provider_ModerationStatus",
                schema: "ServiceCatalog",
                table: "Reviews",
                columns: new[] { "ProviderId", "ModerationStatus" });

            migrationBuilder.CreateIndex(
                name: "UX_ReviewReports_Review_Reporter",
                schema: "ServiceCatalog",
                table: "ReviewReports",
                columns: new[] { "ReviewId", "ReportedByUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ReviewVotes_Review_User",
                schema: "ServiceCatalog",
                table: "ReviewVotes",
                columns: new[] { "ReviewId", "UserId" },
                unique: true);

            // ── Backfill (hand-written) ──
            // Every review that exists before moderation was already public, and providers have already seen it.
            // Hiding them all would silently remove standing content and the only rating signal the platform has,
            // so they are published — and so are the replies attached to them. The column default ('Pending')
            // exists for the other case: a row INSERTed later by code that predates moderation.
            //
            // Idempotent: each statement only touches rows still in the state this migration found them in, so a
            // retried run changes nothing it already changed and never publishes something moderated since.
            migrationBuilder.Sql(
                """
                UPDATE "ServiceCatalog"."Reviews"
                SET "ModerationStatus" = 'Published',
                    "FirstPublishedAt" = "CreatedAt",
                    "ModeratedAt"      = NOW(),
                    "ModeratedBy"      = 'migration:AddReviewModerationAndVoting'
                WHERE "ModerationStatus" = 'Pending'
                  AND "ModeratedAt" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "ServiceCatalog"."Reviews"
                SET "ReplyModerationStatus" = 'Published'
                WHERE "ProviderResponse" IS NOT NULL
                  AND "ReplyModerationStatus" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderRatingSummaries",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ReviewReports",
                schema: "ServiceCatalog");

            migrationBuilder.DropTable(
                name: "ReviewVotes",
                schema: "ServiceCatalog");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ModerationStatus_CreatedAt",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_Provider_ModerationStatus",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CleanlinessRating",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ConductRating",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "FirstPublishedAt",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "HelpfulVoteCount",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ModeratedBy",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "NotHelpfulVoteCount",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "PunctualityRating",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReplyModerationReason",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReplyModerationStatus",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SkillRating",
                schema: "ServiceCatalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "PublishedReviewCount",
                schema: "ServiceCatalog",
                table: "Providers");
        }
    }
}
