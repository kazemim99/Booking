using System.Globalization;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// The product's notification wording, in Persian, with Jalali dates and salon wall-clock times.
    /// </summary>
    /// <remarks>
    /// <para>Dates go through <see cref="BookingSmsText.PersianDate"/> rather than a second Jalali
    /// implementation: a booking time is a salon wall-clock value with no zone (FOLLOW-UPS #63), and two
    /// renderers would eventually disagree about what "Tuesday" means.</para>
    ///
    /// <para>Every catalogued notification must have wording here — a test enforces it — so a notification
    /// cannot reach a person as a blank or an English placeholder.</para>
    /// </remarks>
    public sealed class PersianNotificationCopyWriter : INotificationCopyWriter
    {
        public NotificationCopy Write(
            NotificationEventCode code,
            IReadOnlyDictionary<string, string> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);

            var customer = Name(parameters, NotificationParameter.CustomerName, "مشتری گرامی");
            // The salon reads about a customer in the third person; «مشتری گرامی» there reads as if addressed to it.
            var aCustomer = Name(parameters, NotificationParameter.CustomerName, "یک مشتری");
            var business = Value(parameters, NotificationParameter.BusinessName) ?? "سالن";
            var service = Value(parameters, NotificationParameter.ServiceName);
            var staff = Value(parameters, NotificationParameter.StaffName);
            var amount = Value(parameters, NotificationParameter.Amount);
            var reason = Value(parameters, NotificationParameter.Reason);
            var when = When(parameters);
            var forService = service is null ? string.Empty : $" برای {service}";

            return code switch
            {
                // ── Customer: booking ──
                NotificationEventCode.BookingRequested => Copy(
                    "درخواست نوبت ثبت شد",
                    $"{customer} عزیز، درخواست نوبت شما در {business}{forService} {when} ثبت شد و در انتظار تأیید سالن است."),

                NotificationEventCode.BookingConfirmed => Copy(
                    "نوبت شما تأیید شد",
                    $"{customer} عزیز، نوبت شما در {business}{forService} {when} تأیید شد."),

                NotificationEventCode.BookingRejected => Copy(
                    "درخواست نوبت تأیید نشد",
                    $"{customer} عزیز، متأسفانه {business} نتوانست نوبت شما{forService} {when} را تأیید کند"
                    + (reason is null ? "." : $": {reason}")),

                NotificationEventCode.BookingRescheduled => Copy(
                    "زمان نوبت تغییر کرد",
                    $"{customer} عزیز، زمان نوبت شما در {business}{forService} به {when} تغییر کرد."),

                NotificationEventCode.BookingCancelledByProvider => Copy(
                    "نوبت شما لغو شد",
                    $"{customer} عزیز، نوبت شما در {business}{forService} {when} از سوی سالن لغو شد"
                    + (reason is null ? "." : $": {reason}")),

                NotificationEventCode.BookingCancelledAck => Copy(
                    "نوبت لغو شد",
                    $"{customer} عزیز، نوبت شما در {business} {when} لغو شد."),

                NotificationEventCode.BookingReminder24h => Copy(
                    "یادآوری نوبت فردا",
                    $"{customer} عزیز، یادآوری می‌کنیم نوبت شما در {business}{forService} {when} است."),

                NotificationEventCode.BookingReminder2h => Copy(
                    "نوبت شما نزدیک است",
                    $"{customer} عزیز، نوبت شما در {business}{forService} {when} است. منتظرتان هستیم."),

                NotificationEventCode.BookingCompleted => Copy(
                    "نوبت شما انجام شد",
                    $"{customer} عزیز، از اینکه {business} را انتخاب کردید سپاسگزاریم."),

                NotificationEventCode.ReviewRequest => Copy(
                    "نظر شما چیست؟",
                    $"{customer} عزیز، لطفاً تجربه‌تان از {business} را ثبت کنید."),

                // Deliberately not a repeat of the wording above: a recipient who sees the same sentence
                // twice reads it as a bug rather than as a reminder.
                NotificationEventCode.ReviewReminder => Copy(
                    "هنوز نظرتان را نشنیده‌ایم",
                    $"{customer} عزیز، اگر فرصت دارید، تجربه‌تان از {business} را برای ما بنویسید."),

                NotificationEventCode.BookingNoShow => Copy(
                    "نوبت استفاده‌نشده",
                    $"{customer} عزیز، نوبت شما در {business} {when} استفاده نشد."),


                // ── Customer: money ──
                NotificationEventCode.PaymentReceived => Copy(
                    "پرداخت انجام شد",
                    $"{customer} عزیز، پرداخت شما{Money(amount)} در {business} با موفقیت ثبت شد."),

                NotificationEventCode.PaymentFailed => Copy(
                    "پرداخت ناموفق",
                    $"{customer} عزیز، پرداخت شما{Money(amount)} در {business} انجام نشد. لطفاً دوباره تلاش کنید."),

                NotificationEventCode.RefundProcessed => Copy(
                    "بازگشت وجه",
                    $"{customer} عزیز، مبلغ{Money(amount)} به شما بازگردانده شد."),

                // ── Customer: account ──
                NotificationEventCode.PhoneVerification => Copy(
                    "کد ورود",
                    $"کد ورود شما: {Value(parameters, NotificationParameter.Code) ?? "------"}"),

                NotificationEventCode.Welcome => Copy(
                    "خوش آمدید",
                    $"{customer} عزیز، به بوکسی خوش آمدید."),

                NotificationEventCode.PasswordReset => Copy(
                    "بازنشانی رمز عبور",
                    $"کد بازنشانی رمز عبور شما: {Value(parameters, NotificationParameter.Code) ?? "------"}"),

                NotificationEventCode.SecurityAlert => Copy(
                    "هشدار امنیتی",
                    $"{customer} عزیز، تغییری در تنظیمات امنیتی حساب شما ثبت شد"
                    + (reason is null ? "." : $": {reason}")),

                // ── Provider: booking ──
                NotificationEventCode.NewBookingRequest => Copy(
                    "درخواست نوبت جدید",
                    $"درخواست نوبت جدید از {aCustomer}{forService} {when} در انتظار تأیید شماست."),

                NotificationEventCode.NewBookingConfirmed => Copy(
                    "نوبت جدید",
                    $"نوبت جدید برای {aCustomer}{forService} {when} ثبت شد."),

                NotificationEventCode.BookingCancelledByCustomer => Copy(
                    "لغو نوبت از سوی مشتری",
                    $"{aCustomer} نوبت {when}{forService} را لغو کرد."),

                NotificationEventCode.BookingRescheduledByCustomer => Copy(
                    "تغییر زمان از سوی مشتری",
                    $"{aCustomer} زمان نوبت خود را به {when} تغییر داد."),

                NotificationEventCode.CustomerNoShow => Copy(
                    "عدم مراجعهٔ مشتری",
                    $"{aCustomer} در نوبت {when} مراجعه نکرد."),

                NotificationEventCode.NextAppointmentReminder => Copy(
                    "نوبت بعدی شما",
                    $"نوبت بعدی شما {when}{forService} با {aCustomer} است."),

                NotificationEventCode.DailyScheduleDigest => Copy(
                    "برنامهٔ امروز",
                    $"امروز {Value(parameters, NotificationParameter.Count) ?? "چند"} نوبت در {business} دارید."),

                // ── Provider: staff and organisation ──
                NotificationEventCode.InvitationSent => Copy(
                    "دعوت به همکاری",
                    $"شما به همکاری در {business} دعوت شده‌اید."),

                NotificationEventCode.InvitationAccepted => Copy(
                    "دعوت پذیرفته شد",
                    $"{Coalesce(staff, customer)} دعوت همکاری در {business} را پذیرفت."),


                // Second person, because these go to the member — see StaffMembershipNotificationTests.
                // Written in the third person they read as a leak from the owner's inbox.
                NotificationEventCode.StaffAdded => Copy(
                    "به تیم خوش آمدید",
                    $"شما به تیم {business} اضافه شدید."),

                NotificationEventCode.StaffRemoved => Copy(
                    "پایان همکاری",
                    $"همکاری شما با {business} پایان یافت"
                    + (reason is null ? "." : $": {reason}")),

                NotificationEventCode.StaffAssignedToBooking => Copy(
                    "نوبت جدید برای شما",
                    $"نوبت {when}{forService} با {aCustomer} به شما سپرده شد."),

                // ── Provider: money and account ──
                NotificationEventCode.PayoutCompleted => Copy(
                    "تسویه انجام شد",
                    $"مبلغ{Money(amount)} به حساب {business} واریز شد."),



                NotificationEventCode.ProviderActivated => Copy(
                    "سالن شما فعال شد",
                    $"{business} اکنون فعال است و می‌تواند نوبت بپذیرد."),

                // ── Reviews ──
                NotificationEventCode.ReviewPublished => Copy(
                    "نظر جدید",
                    $"یک مشتری به {business} امتیاز {Stars(parameters)} داد. می‌توانید آن را ببینید و پاسخ دهید."),

                // Worded as a change, not a new review: a reply the salon already wrote may now sit under
                // different words, and the salon needs to know to look.
                NotificationEventCode.ReviewRepublished => Copy(
                    "نظر ویرایش شد",
                    $"یکی از نظرهای {business} تغییر کرد؛ امتیاز فعلی {Stars(parameters)} است. نگاهی بیندازید."),

                NotificationEventCode.ReviewReplyPublished => Copy(
                    "پاسخ به نظر شما",
                    $"{customer} عزیز، {business} به نظر شما پاسخ داد."),

                // Says what happened and why, without blaming the reader: the reason is the moderator's, and the
                // author cannot resubmit (rejection is permanent), so the wording does not invite a retry.
                NotificationEventCode.ReviewRejected => Copy(
                    "نظر شما منتشر نشد",
                    $"{customer} عزیز، نظر شما دربارهٔ {business} پس از بررسی منتشر نشد"
                    + (reason is null ? "." : $": {reason}")),


                _ => throw new KeyNotFoundException(
                    $"Notification '{code}' has no wording. Add it to {nameof(PersianNotificationCopyWriter)} " +
                    "before raising it, or a recipient gets an empty message."),
            };
        }

        /// <summary>The body doubles as the SMS text: one wording, so the two can never disagree.</summary>
        private static NotificationCopy Copy(string subject, string body) => new(subject, body, body);

        private static string? Value(IReadOnlyDictionary<string, string> p, string key) =>
            p.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

        private static string Name(IReadOnlyDictionary<string, string> p, string key, string fallback) =>
            Value(p, key) ?? fallback;

        private static string Coalesce(string? first, string second) => first ?? second;

        private static string Money(string? amount) => amount is null ? string.Empty : $" {amount}";

        /// <summary>"۴٫۵ از ۵" — Persian digits and decimal separator — or a neutral phrase if none was captured.</summary>
        private static string Stars(IReadOnlyDictionary<string, string> p)
        {
            var raw = Value(p, NotificationParameter.Rating);
            if (raw is null || !decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var rating))
                return "جدیدی";

            var text = rating.ToString("0.#", CultureInfo.InvariantCulture)
                .Replace('.', '٫')
                .Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c)
                .ToArray();
            return $"{new string(text)} از ۵";
        }

        /// <summary>
        /// "جمعه ۱۴۰۵/۰۷/۰۱ ساعت ۱۴:۳۰", or a neutral phrase when the raise site captured no time — better
        /// than printing a wrong date from a missing value.
        /// </summary>
        private static string When(IReadOnlyDictionary<string, string> p)
        {
            var raw = Value(p, NotificationParameter.StartTime);
            if (raw is null)
                return "در زمان تعیین‌شده";

            if (!DateTime.TryParse(
                    raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when))
                return "در زمان تعیین‌شده";

            return BookingSmsText.PersianDateTime(when);
        }
    }
}
