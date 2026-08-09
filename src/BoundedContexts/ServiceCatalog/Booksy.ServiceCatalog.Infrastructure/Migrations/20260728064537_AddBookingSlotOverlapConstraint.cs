using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSlotOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // C3 booking-slot-integrity: database-level guarantee that a staff member
            // never has two overlapping ACTIVE bookings. This mirrors the application
            // conflict check (BookingReadRepository.GetConflictingBookingsAsync:
            // same StaffId, overlapping [StartTime,EndTime), Status in Requested/Confirmed)
            // and is the defense-in-depth backstop that holds even when availability
            // rows are missing or the read-check races under concurrency.
            //
            // Uses a GiST exclusion constraint over (StaffId =, tstzrange(Start,End) &&),
            // partial on active statuses so cancelled/completed/no-show bookings do not
            // block the slot. Requires the btree_gist extension (equality on a scalar
            // inside a GiST index). Deployment note: the migrating role needs CREATE
            // EXTENSION privilege, or btree_gist must be pre-created by an admin.
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Bookings""
                ADD CONSTRAINT ""EXC_Bookings_Staff_NoOverlap""
                EXCLUDE USING gist (
                    ""StaffId"" WITH =,
                    tstzrange(""StartTime"", ""EndTime"") WITH &&
                ) WHERE (""Status"" IN ('Requested', 'Confirmed'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceCatalog"".""Bookings""
                DROP CONSTRAINT IF EXISTS ""EXC_Bookings_Staff_NoOverlap"";");
        }
    }
}
