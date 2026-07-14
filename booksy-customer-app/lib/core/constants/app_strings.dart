/// All user-facing strings in one place so a future localization pass
/// is mechanical. Widgets must reference these by name, never inline text.
class AppStrings {
  AppStrings._();

  // App
  static const String appName = 'Booksy';
  static const String appTagline = 'رزرو آنلاین خدمات زیبایی';

  // Navigation tabs
  static const String tabHome = 'خانه';
  static const String tabExplore = 'جستجو';
  static const String tabAppointments = 'نوبت‌ها';
  static const String tabProfile = 'پروفایل';

  // Common actions
  static const String retry = 'تلاش مجدد';
  static const String confirm = 'تایید';
  static const String cancel = 'انصراف';
  static const String back = 'بازگشت';
  static const String undo = 'بازگردانی';
  static const String login = 'ورود';
  static const String logout = 'خروج از حساب';
  static const String edit = 'ویرایش';
  static const String save = 'ذخیره';
  static const String clearFilters = 'حذف فیلترها';

  // Common states
  static const String genericError = 'خطایی رخ داد. دوباره تلاش کنید';
  static const String offlineBanner = 'اتصال اینترنت برقرار نیست';
  static const String offlineActionError =
      'برای انجام این عملیات به اینترنت نیاز دارید';
  static const String loading = 'در حال بارگذاری…';

  // Auth — login
  static const String loginTitle = 'ورود / ثبت‌نام';
  static const String loginSubtitle = 'برای ادامه شماره موبایل خود را وارد کنید';
  static const String phoneLabel = 'شماره موبایل';
  static const String phoneHint = '09123456789';
  static const String phoneRequired = 'لطفا شماره موبایل را وارد کنید';
  static const String phoneInvalid = 'شماره موبایل معتبر نیست (مثال: 09123456789)';
  static const String sendOtp = 'ارسال کد تایید';
  static const String termsNotice =
      'با ورود به اپلیکیشن، شرایط و قوانین استفاده از خدمات را می‌پذیرید';

  // Auth — OTP
  static const String otpTitle = 'کد تایید';
  static const String otpPageTitle = 'تایید شماره موبایل';
  static String otpSubtitle(String phoneNumber) =>
      'کد ۶ رقمی ارسال شده به شماره $phoneNumber را وارد کنید';
  static const String otpEditNumber = 'ویرایش شماره';
  static const String otpRequired = 'لطفا کد تایید را وارد کنید';
  static const String otpInvalidLength = 'کد تایید باید ۶ رقم باشد';
  static const String otpWrongCode = 'کد وارد شده صحیح نیست';
  static const String verifyAndContinue = 'تایید و ادامه';
  static const String resendOtp = 'ارسال مجدد کد تایید';
  static String resendCountdown(String seconds) =>
      'ارسال مجدد کد تا $seconds ثانیه دیگر';
  static const String otpResent = 'کد تایید مجدداً ارسال شد';
  static const String loginSuccess = 'ورود موفقیت‌آمیز بود';

  // Home
  static const String searchPlaceholder = 'جستجوی سالن، آرایشگاه، خدمات زیبایی...';
  static const String upcomingBookingTitle = 'نوبت بعدی شما';
  static const String categoriesTitle = 'دسته‌بندی خدمات';
  static const String topProvidersTitle = 'برترین سالن‌ها';
  static const String promotionsTitle = 'پیشنهادهای ویژه';
  static const String recentAndFavoritesTitle = 'بازدید شده و علاقه‌مندی‌ها';
  static const String noVisitedProviders = 'هنوز سالنی را بازدید نکرده‌اید';
  static const String recentBadge = 'اخیر';
  static const String sectionLoadFailed = 'بارگذاری این بخش ناموفق بود';

  // Explore
  static const String exploreWhere = 'کجا؟';
  static const String exploreWhen = 'کی؟';
  static const String allCategories = 'همه';
  static const String noResultsTitle = 'نتیجه‌ای یافت نشد';
  static const String noResultsSubtitle = 'فیلترها یا کلمات جستجو را تغییر دهید';

  // Provider detail
  static const String servicesTitle = 'خدمات';
  static const String workingHoursTitle = 'ساعات کاری';
  static const String aboutTitle = 'درباره';
  static const String bookAction = 'رزرو نوبت';
  static const String showOnMap = 'نمایش روی نقشه';
  static const String providerNotFound = 'سالن مورد نظر یافت نشد';

  // Booking flow
  static const String bookingSelectService = 'انتخاب خدمت';
  static const String bookingSelectStaff = 'انتخاب متخصص';
  static const String bookingSelectTime = 'انتخاب زمان';
  static const String bookingConfirmTitle = 'تایید نوبت';
  static const String bookingConfirmCta = 'تایید و رزرو نوبت';
  static const String bookingSuccessTitle = 'نوبت شما ثبت شد';
  static const String bookingSuccessSubtitle =
      'جزئیات نوبت در بخش نوبت‌ها قابل مشاهده است';
  static const String bookingViewAppointments = 'مشاهده نوبت‌ها';
  static const String bookingAnyStaff = 'فرقی نمی‌کند';
  static const String bookingNoSlots = 'برای این روز زمان خالی وجود ندارد';
  static const String bookingSlotTaken =
      'این زمان دیگر در دسترس نیست. لطفاً زمان دیگری انتخاب کنید';
  static const String bookingDuration = 'مدت زمان';
  static const String bookingPrice = 'قیمت';
  static const String bookingDate = 'تاریخ';
  static const String bookingTime = 'ساعت';
  static const String bookingStaff = 'متخصص';
  static const String bookingService = 'خدمت';
  static const String bookingProvider = 'سالن';

  // Appointments
  static const String appointmentsTitle = 'نوبت‌ها';
  static const String appointmentsUpcoming = 'پیش رو';
  static const String appointmentsPast = 'گذشته';
  static const String appointmentsEmptyTitle = 'نوبت آینده‌ای ندارید';
  static const String appointmentsEmptySubtitle = 'اولین نوبت خود را رزرو کنید';
  static const String appointmentsGuestTitle = 'نوبت‌ها';
  static const String appointmentsGuestSubtitle =
      'برای مشاهده نوبت‌های خود وارد شوید';
  static const String appointmentsGuestQuestion = 'قبلاً از بوکسی استفاده کرده‌اید؟';
  static const String findProviders = 'یافتن سالن‌های نزدیک';
  static const String findProvider = 'یافتن سالن';
  static const String cancelBooking = 'لغو نوبت';
  static const String cancelBookingConfirmTitle = 'لغو نوبت';
  static const String cancelBookingConfirmBody =
      'آیا از لغو این نوبت مطمئن هستید؟ این عملیات قابل بازگشت نیست.';
  static const String cancelBookingSuccess = 'نوبت لغو شد';
  static const String rescheduleBooking = 'تغییر زمان';
  static const String rescheduleSuccess = 'زمان نوبت تغییر کرد';

  // Booking statuses
  static const String statusConfirmed = 'تایید شده';
  static const String statusPending = 'در انتظار تایید';
  static const String statusCompleted = 'انجام شده';
  static const String statusCancelled = 'لغو شده';
  static const String statusNoShow = 'عدم مراجعه';

  // Profile
  static const String profileTitle = 'پروفایل';
  static const String profileEditTitle = 'ویرایش پروفایل';
  static const String firstNameLabel = 'نام';
  static const String lastNameLabel = 'نام خانوادگی';
  static const String profileUpdated = 'پروفایل به‌روزرسانی شد';
  static const String logoutConfirmTitle = 'خروج از حساب';
  static const String logoutConfirmBody = 'آیا می‌خواهید از حساب خود خارج شوید؟';
}
