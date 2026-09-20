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
                    $"{customer} عزیز، متأسفانه {business} نتوانست نوبت {when} را تأیید کند"
                    + (reason is null ? "." : $": {reason}")),

                NotificationEventCode.BookingRescheduled => Copy(
                    "زمان نوبت تغییر کرد",
                    $"{customer} عزیز، زمان نوبت شما در {business} به {when} تغییر کرد."),

                NotificationEventCode.BookingCancelledByProvider => Copy(
                    "نوبت شما لغو شد",
                    $"{customer} عزیز، نوبت شما در {business} {when} از سوی سالن لغو شد"
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

                NotificationEventCode.BookingNoShow => Copy(
                    "نوبت استفاده‌نشده",
                    $"{customer} عزیز، نوبت شما در {business} {when} استفاده نشد."),

                NotificationEventCode.DepositRequired => Copy(
                    "پرداخت بیعانه",
                    $"{customer} عزیز، برای قطعی‌شدن نوبت {when} در {business}"
                    + (amount is null ? " لازم است بیعانه پرداخت شود." : $" مبلغ {amount} بیعانه لازم است.")),

                NotificationEventCode.PaymentDeadlineReminder => Copy(
                    "مهلت پرداخت رو به پایان",
                    $"{customer} عزیز، مهلت پرداخت بیعانهٔ نوبت {when} در {business} رو به پایان است."),

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
                    $"درخواست نوبت جدید از {customer}{forService} {when} در انتظار تأیید شماست."),

                NotificationEventCode.NewBookingConfirmed => Copy(
                    "نوبت جدید",
                    $"نوبت جدید برای {customer}{forService} {when} ثبت شد."),

                NotificationEventCode.BookingCancelledByCustomer => Copy(
                    "لغو نوبت از سوی مشتری",
                    $"{customer} نوبت {when}{forService} را لغو کرد."),

                NotificationEventCode.BookingRescheduledByCustomer => Copy(
                    "تغییر زمان از سوی مشتری",
                    $"{customer} زمان نوبت خود را به {when} تغییر داد."),

                NotificationEventCode.CustomerNoShow => Copy(
                    "عدم مراجعهٔ مشتری",
                    $"{customer} در نوبت {when} مراجعه نکرد."),

                NotificationEventCode.NextAppointmentReminder => Copy(
                    "نوبت بعدی شما",
                    $"نوبت بعدی شما {when}{forService} با {customer} است."),

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

                NotificationEventCode.JoinRequestApproved => Copy(
                    "درخواست عضویت تأیید شد",
                    $"درخواست عضویت شما در {business} تأیید شد."),

                NotificationEventCode.StaffAdded => Copy(
                    "همکار جدید",
                    $"{Coalesce(staff, customer)} به {business} اضافه شد."),

                NotificationEventCode.StaffRemoved => Copy(
                    "پایان همکاری",
                    $"همکاری {Coalesce(staff, customer)} با {business} پایان یافت."),

                NotificationEventCode.StaffAssignedToBooking => Copy(
                    "نوبت جدید برای شما",
                    $"نوبت {when}{forService} با {customer} به شما سپرده شد."),

                // ── Provider: money and account ──
                NotificationEventCode.PayoutCompleted => Copy(
                    "تسویه انجام شد",
                    $"مبلغ{Money(amount)} به حساب {business} واریز شد."),

                NotificationEventCode.PayoutFailed => Copy(
                    "تسویه ناموفق",
                    $"واریز{Money(amount)} به حساب {business} انجام نشد"
                    + (reason is null ? "." : $": {reason}")),

                NotificationEventCode.PayoutOnHold => Copy(
                    "تسویه در انتظار بررسی",
                    $"تسویهٔ {business} موقتاً متوقف شده است"
                    + (reason is null ? "." : $": {reason}")),

                NotificationEventCode.InvoiceGenerated => Copy(
                    "صورت‌حساب جدید",
                    $"صورت‌حساب جدید برای {business} صادر شد."),

                NotificationEventCode.ProviderVerificationChanged => Copy(
                    "وضعیت احراز هویت",
                    $"وضعیت احراز هویت {business} تغییر کرد"
                    + (reason is null ? "." : $": {reason}")),

                NotificationEventCode.ProviderActivated => Copy(
                    "سالن شما فعال شد",
                    $"{business} اکنون فعال است و می‌تواند نوبت بپذیرد."),

                NotificationEventCode.ProviderDeactivated => Copy(
                    "سالن شما غیرفعال شد",
                    $"{business} غیرفعال شد و نوبت جدیدی نمی‌پذیرد"
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

            return $"{BookingSmsText.PersianDate(when)} ساعت {when:HH:mm}";
        }
    }
}
