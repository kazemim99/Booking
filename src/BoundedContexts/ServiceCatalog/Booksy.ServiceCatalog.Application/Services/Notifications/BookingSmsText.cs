using System.Globalization;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// What a customer reads when the salon books them. Booking times are salon wall-clock values
    /// with no zone (FOLLOW-UPS #63), so the date and time go into the message exactly as the salon
    /// entered them, written in the Persian calendar the customer reads.
    /// </summary>
    public static class BookingSmsText
    {
        private static readonly PersianCalendar Persian = new();

        private static readonly string[] WeekDays =
        {
            "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه",
        };

        /// <summary>«{روز} {۱۴۰۵/۰۷/۰۱}» for a salon wall-clock date.</summary>
        public static string PersianDate(DateTime when) =>
            $"{WeekDays[(int)when.DayOfWeek]} {Persian.GetYear(when)}/{Persian.GetMonth(when):00}/{Persian.GetDayOfMonth(when):00}";

        /// <summary>
        /// The confirmation the customer gets. No payment link yet: there is no payment gateway, and
        /// a link that leads nowhere is worse than none.
        /// </summary>
        public static string Confirmed(
            string customerFirstName,
            string businessName,
            string serviceName,
            DateTime startTime)
        {
            var name = string.IsNullOrWhiteSpace(customerFirstName) ? "مشتری گرامی" : customerFirstName.Trim();
            var service = string.IsNullOrWhiteSpace(serviceName) ? string.Empty : $" برای {serviceName.Trim()}";
            return $"{name} عزیز، نوبت شما در {businessName.Trim()}{service} " +
                   $"{PersianDate(startTime)} ساعت {startTime:HH:mm} ثبت شد.";
        }
    }
}
