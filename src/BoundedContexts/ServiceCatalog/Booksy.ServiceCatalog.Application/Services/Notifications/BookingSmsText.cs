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

        private static readonly string[] Months =
        {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند",
        };

        /// <summary>
        /// «سه‌شنبه ۲۸ مهر» for a salon wall-clock date — the way people say it. It used to be «جمعه 1405/07/03»:
        /// Latin digits in a slash date, which right-to-left text scrambles (read back as "۳ هفته ۴ ساعت ۵ جمعه" in
        /// the QA walkthrough of 2026-09-22). No year: a booking is days away, never a year.
        /// </summary>
        public static string PersianDate(DateTime when) =>
            $"{WeekDays[(int)when.DayOfWeek]} {PersianDigits(Persian.GetDayOfMonth(when).ToString(CultureInfo.InvariantCulture))} {Months[Persian.GetMonth(when) - 1]}";

        /// <summary>«۱۴:۳۰» — the salon's wall-clock time, in Persian digits.</summary>
        public static string PersianTime(DateTime when) =>
            PersianDigits(when.ToString("HH:mm", CultureInfo.InvariantCulture));

        /// <summary>«سه‌شنبه ۲۸ مهر، ساعت ۱۴:۳۰».</summary>
        public static string PersianDateTime(DateTime when) => $"{PersianDate(when)}، ساعت {PersianTime(when)}";

        private static string PersianDigits(string s)
        {
            var chars = s.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
                if (chars[i] is >= '0' and <= '9')
                    chars[i] = (char)('۰' + (chars[i] - '0'));
            return new string(chars);
        }

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
                   $"{PersianDateTime(startTime)} ثبت شد.";
        }
    }
}
