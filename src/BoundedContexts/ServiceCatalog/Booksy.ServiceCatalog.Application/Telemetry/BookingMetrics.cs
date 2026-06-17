using System.Diagnostics.Metrics;

namespace Booksy.ServiceCatalog.Application.Telemetry
{
    /// <summary>
    /// Booking-funnel counters (created → confirmed → completed / cancelled / no-show).
    /// Emitted on the "Booksy.ServiceCatalog.Bookings" meter, which OpenTelemetry collects
    /// automatically via <c>AddMeter("Booksy.*")</c> (see OpenTelemetryExtensions). A static
    /// meter keeps emission dependency-free at the call sites; OTel attributes by meter name.
    /// </summary>
    public static class BookingMetrics
    {
        public const string MeterName = "Booksy.ServiceCatalog.Bookings";

        private static readonly Meter Meter = new(MeterName, "1.0.0");

        private static readonly Counter<long> Created =
            Meter.CreateCounter<long>("booksy.bookings.created", "{booking}", "Bookings created");
        private static readonly Counter<long> Confirmed =
            Meter.CreateCounter<long>("booksy.bookings.confirmed", "{booking}", "Bookings confirmed");
        private static readonly Counter<long> Completed =
            Meter.CreateCounter<long>("booksy.bookings.completed", "{booking}", "Bookings completed");
        private static readonly Counter<long> Cancelled =
            Meter.CreateCounter<long>("booksy.bookings.cancelled", "{booking}", "Bookings cancelled");
        private static readonly Counter<long> NoShow =
            Meter.CreateCounter<long>("booksy.bookings.noshow", "{booking}", "Bookings marked no-show");

        public static void BookingCreated() => Created.Add(1);
        public static void BookingConfirmed() => Confirmed.Add(1);
        public static void BookingCompleted() => Completed.Add(1);
        public static void BookingCancelled() => Cancelled.Add(1);
        public static void BookingNoShow() => NoShow.Add(1);
    }
}
