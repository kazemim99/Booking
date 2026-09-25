using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsanRezerve.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelabelUsdPricesAsToman : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every price in this schema is a Toman amount — a salon charging 250,000 for a haircut
            // means Toman — but the catalogue stamped "USD" on them, which is how the customer app
            // showed «USD ۱۵۰۰۰۰۰». This relabels those rows; not one amount changes.
            //
            // Rows already marked IRR are left alone: the payment gateways (ZarinPal, Behpardakht)
            // genuinely settle in Rial, and whether a Toman price becomes a Rial charge x10 is a
            // money decision, not a rename (FOLLOW-UPS #67).
            migrationBuilder.Sql(@"
                DO $$
                DECLARE col RECORD;
                BEGIN
                    FOR col IN
                        SELECT table_name, column_name
                        FROM information_schema.columns
                        WHERE table_schema = 'ServiceCatalog'
                          AND column_name LIKE '%Currency%'
                          AND data_type IN ('text', 'character varying', 'character')
                    LOOP
                        EXECUTE format(
                            'UPDATE %I.%I SET %I = ''IRT'' WHERE %I = ''USD''',
                            'ServiceCatalog', col.table_name, col.column_name, col.column_name);
                    END LOOP;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately not reversed: putting "USD" back on Toman prices would restore a lie.
        }
    }
}
