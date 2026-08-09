using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPerBookingDedupConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // C2 payment-consistency (invariant I2 — money never duplicated):
            // at most ONE captured payment per booking. A partial unique index on
            // BookingId over the captured statuses ('Paid','PartiallyPaid') makes a
            // second concurrent charge for the same booking fail at the database, even
            // if the application-level checks race. It intentionally does NOT include
            // 'Pending' — abandoned/failed attempts may be retried (a new Pending row) —
            // so this is non-regressive without reconciliation. Direct (non-booking)
            // payments are excluded via BookingId IS NOT NULL.
            //
            // Deployment note: audit for existing bookings with >1 captured payment
            // before applying; the index creation fails if duplicates exist.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""UX_Payments_OneCapturedPerBooking""
                ON ""ServiceCatalog"".""Payments"" (""BookingId"")
                WHERE ""BookingId"" IS NOT NULL AND ""Status"" IN ('Paid', 'PartiallyPaid');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""ServiceCatalog"".""UX_Payments_OneCapturedPerBooking"";");
        }
    }
}
