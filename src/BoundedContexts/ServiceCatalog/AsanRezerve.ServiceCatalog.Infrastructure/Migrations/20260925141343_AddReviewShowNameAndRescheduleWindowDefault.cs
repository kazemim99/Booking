using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsanRezerve.ServiceCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewShowNameAndRescheduleWindowDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The author's «نامم نمایش داده نشود» choice (openspec/changes/_inline/reviews-and-reschedule-round2 D2).
            // Every existing review keeps showing its author's name: true, not the CLR default. The default lives only
            // here, never in the model — EF would read a false the app sets as "unset" and write this default instead.
            migrationBuilder.AddColumn<bool>(
                name: "ShowName",
                schema: "ServiceCatalog",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // D5: a customer may move a booking until 2 hours before it (was 24). Rows still at the old DEFAULT move to
            // the new one; a salon that chose any other value keeps it. Bookings copy their policy when made, so the
            // open ones (Requested, Confirmed — the status is stored as its name) move too; closed ones keep the
            // record of what applied.
            migrationBuilder.Sql(@"
                UPDATE ""ServiceCatalog"".""Providers""
                   SET ""BookingPolicyRescheduleWindowHours"" = 2
                 WHERE ""BookingPolicyRescheduleWindowHours"" = 24;

                UPDATE ""ServiceCatalog"".""Services""
                   SET ""BookingPolicyRescheduleWindowHours"" = 2
                 WHERE ""BookingPolicyRescheduleWindowHours"" = 24;

                UPDATE ""ServiceCatalog"".""Bookings""
                   SET ""PolicyRescheduleWindowHours"" = 2
                 WHERE ""PolicyRescheduleWindowHours"" = 24
                   AND ""Status"" IN ('Requested', 'Confirmed');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The window change is not reversed: 2 cannot be told apart from a salon's own choice of 2.
            migrationBuilder.DropColumn(
                name: "ShowName",
                schema: "ServiceCatalog",
                table: "Reviews");
        }
    }
}
