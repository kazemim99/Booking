using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <summary>
    /// Data remediation for the one-category-per-provider model: backfills every unset
    /// <c>Providers.PrimaryCategory</c> and <c>Services.Category</c>, then makes "unset" impossible with CHECK
    /// constraints. Pure SQL — it changes no EF model, so its target model is identical to the previous migration's.
    /// </summary>
    /// <remarks>
    /// Re-runnable: every statement is guarded on the "&lt; 1" (unset) state or uses IF EXISTS.
    /// <c>Down()</c> deliberately does not un-backfill the categories — it only removes the constraints and
    /// restores the old zero defaults, because the inferred categories are better data than the zeros they replaced.
    /// </remarks>
    public partial class BackfillProviderPrimaryCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Infer each provider's primary category from the category their services use most.
            migrationBuilder.Sql(@"
                UPDATE ""ServiceCatalog"".""Providers"" p
                SET ""PrimaryCategory"" = inferred.category
                FROM (
                    SELECT DISTINCT ON (s.""ProviderId"")
                           s.""ProviderId"" AS provider_id,
                           s.""Category""   AS category
                    FROM ""ServiceCatalog"".""Services"" s
                    WHERE s.""Category"" >= 1
                    GROUP BY s.""ProviderId"", s.""Category""
                    ORDER BY s.""ProviderId"", COUNT(*) DESC, s.""Category""
                ) AS inferred
                WHERE p.""Id"" = inferred.provider_id
                  AND p.""PrimaryCategory"" < 1;");

            // 2) Providers with no categorised services fall back to HairSalon (1) — the most common category.
            migrationBuilder.Sql(@"
                UPDATE ""ServiceCatalog"".""Providers""
                SET ""PrimaryCategory"" = 1
                WHERE ""PrimaryCategory"" < 1;");

            // 3) Services inherit their provider's primary category.
            migrationBuilder.Sql(@"
                UPDATE ""ServiceCatalog"".""Services"" s
                SET ""Category"" = p.""PrimaryCategory""
                FROM ""ServiceCatalog"".""Providers"" p
                WHERE s.""ProviderId"" = p.""Id""
                  AND s.""Category"" < 1;");

            // 4) Anything still unset (orphaned service rows) gets the same fallback.
            migrationBuilder.Sql(@"
                UPDATE ""ServiceCatalog"".""Services""
                SET ""Category"" = 1
                WHERE ""Category"" < 1;");

            // 5) Make "unset" unrepresentable from here on.
            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Providers""
                DROP CONSTRAINT IF EXISTS ""CK_Providers_PrimaryCategory_Assigned"";");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Providers""
                ADD CONSTRAINT ""CK_Providers_PrimaryCategory_Assigned""
                CHECK (""PrimaryCategory"" >= 1);");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Services""
                DROP CONSTRAINT IF EXISTS ""CK_Services_Category_Assigned"";");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Services""
                ADD CONSTRAINT ""CK_Services_Category_Assigned""
                CHECK (""Category"" >= 1);");

            // 6) Drop the zero defaults that let rows be created unset in the first place.
            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Providers""
                ALTER COLUMN ""PrimaryCategory"" DROP DEFAULT;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Services""
                ALTER COLUMN ""Category"" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Providers""
                DROP CONSTRAINT IF EXISTS ""CK_Providers_PrimaryCategory_Assigned"";");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Services""
                DROP CONSTRAINT IF EXISTS ""CK_Services_Category_Assigned"";");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Providers""
                ALTER COLUMN ""PrimaryCategory"" SET DEFAULT 0;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Services""
                ALTER COLUMN ""Category"" SET DEFAULT 0;");
        }
    }
}
