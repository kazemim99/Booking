using System.Diagnostics.Metrics;

namespace AsanRezerve.ServiceCatalog.Application.Telemetry
{
    /// <summary>
    /// Booking-funnel counters (created → confirmed → completed / cancelled / no-show).
    /// Emitted on the "AsanRezerve.ServiceCatalog.Bookings" meter, which OpenTelemetry collects
    /// automatically via <c>AddMeter("AsanRezerve.*")</c> (see OpenTelemetryExtensions). A static
    /// meter keeps emission dependency-free at the call sites; OTel attributes by meter name.
    /// </summary>
    public static class BookingMetrics
    {
        public const string MeterName = "AsanRezerve.ServiceCatalog.Bookings";

        private static readonly Meter Meter = new(MeterName, "1.0.0");

        private static readonly Counter<long> Created =
            Meter.CreateCounter<long>("asanrezerve.bookings.created", "{booking}", "Bookings created");
        private static readonly Counter<long> Confirmed =
            Meter.CreateCounter<long>("asanrezerve.bookings.confirmed", "{booking}", "Bookings confirmed");
        private static readonly Counter<long> Completed =
            Meter.CreateCounter<long>("asanrezerve.bookings.completed", "{booking}", "Bookings completed");
        private static readonly Counter<long> Cancelled =
            Meter.CreateCounter<long>("asanrezerve.bookings.cancelled", "{booking}", "Bookings cancelled");
        private static readonly Counter<long> NoShow =
            Meter.CreateCounter<long>("asanrezerve.bookings.noshow", "{booking}", "Bookings marked no-show");

        public static void BookingCreated() => Created.Add(1);
        public static void BookingConfirmed() => Confirmed.Add(1);
        public static void BookingCompleted() => Completed.Add(1);
        public static void BookingCancelled() => Cancelled.Add(1);
        public static void BookingNoShow() => NoShow.Add(1);
    }
}
