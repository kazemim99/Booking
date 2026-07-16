/// Centralized Persian (fa-IR) strings for the Provider app.
/// Strings mirror the Vue provider auth screens where equivalents exist.
class AppStrings {
  AppStrings._();

  // App
  static const String appName = 'بوکسی | پنل کسب‌وکار';

  // Login (ProviderLoginView.vue)
  static const String loginTitle = 'ورود به پنل کسب و کار';
  static const String loginSubtitle =
      'برای ورود به پنل ارائه‌دهندگان، شماره موبایل خود را وارد کنید';
  static const String phoneLabel = 'شماره موبایل';
  static const String phoneHint = '09123456789';
  static const String sendOtp = 'دریافت کد';
  static const String termsNotice =
      'با ورود به سیستم، شما قوانین و مقررات را می‌پذیرید';
  static const String customerLoginPrompt = 'مشتری هستید؟';

  // Validation (ProviderLoginView.vue)
  static const String phoneRequired = 'لطفاً شماره موبایل خود را وارد کنید';
  static const String phoneInvalid = 'شماره موبایل وارد شده معتبر نیست';
  static const String otpLengthInvalid = 'لطفاً کد ۶ رقمی را وارد کنید';

  // OTP (VerificationView.vue)
  static const String otpPageTitle = 'تأیید شماره تلفن';
  static const String otpTitle = 'تایید کد';
  static String otpSubtitle(String phone) =>
      'کد ۶ رقمی ارسال شده به شماره $phone را وارد کنید';
  static const String verifyAndContinue = 'تایید کد';
  static const String otpEditNumber = 'ویرایش شماره';
  static const String resendOtp = 'ارسال مجدد کد';
  static String resendCountdown(String seconds) =>
      'ارسال مجدد کد در $seconds ثانیه';
  static const String backToLogin = 'بازگشت به صفحه ورود';
  static const String otpResent = 'کد تأیید مجدداً ارسال شد';

  // Success
  static const String loginSuccess = 'ورود موفقیت‌آمیز بود!';
  static const String registerSuccess = 'ثبت‌نام شما با موفقیت انجام شد!';

  // Logout
  static const String logout = 'خروج';
  static const String loggedOut = 'با موفقیت خارج شدید';

  // Onboarding handoff
  static const String onboardingRequiredTitle = 'تکمیل پروفایل کسب‌وکار';
  static const String onboardingRequiredBody =
      'برای استفاده از پنل، ابتدا باید اطلاعات کسب‌وکار خود را تکمیل کنید';
  static const String onboardingStart = 'تکمیل ثبت‌نام';

  // Account blocked (E-14 / D-1 — Flutter improvement)
  static const String accountBlockedTitle = 'حساب کاربری در دسترس نیست';
  static String accountBlockedBody(String status) {
    switch (status) {
      case 'Suspended':
        return 'حساب کسب‌وکار شما به حالت تعلیق درآمده است. لطفاً با پشتیبانی تماس بگیرید.';
      case 'Inactive':
        return 'حساب کسب‌وکار شما غیرفعال است. لطفاً با پشتیبانی تماس بگیرید.';
      case 'Archived':
        return 'حساب کسب‌وکار شما بایگانی شده است. لطفاً با پشتیبانی تماس بگیرید.';
      default:
        return 'حساب کسب‌وکار شما در دسترس نیست. لطفاً با پشتیبانی تماس بگیرید.';
    }
  }

  static const String accountBlockedLogout = 'خروج از حساب';

  // ==================== Onboarding wizard ====================
  static const List<String> onboardingStepLabels = [
    'اطلاعات کسب‌و‌کار',
    'دسته‌بندی',
    'موقعیت مکانی',
    'خدمات',
    'ساعات کاری',
    'گالری تصاویر',
    'بررسی نهایی',
    'تکمیل',
  ];

  static String stepOf(int current, int total) => 'مرحله $current از $total';

  static const String next = 'بعدی';
  static const String previous = 'قبلی';
  static const String skip = 'رد کردن';
  static const String confirmAndSubmit = 'ثبت نهایی';
  static const String edit = 'ویرایش';

  // Step 1 — business info
  static const String businessInfoTitle = 'اطلاعات کسب‌و‌کار';
  static const String businessInfoSubtitle = 'اطلاعات کسب‌و‌کار خود را وارد کنید';
  static const String businessName = 'نام کسب‌و‌کار';
  static const String ownerFirstName = 'نام مالک';
  static const String ownerLastName = 'نام خانوادگی مالک';
  static const String emailOptional = 'ایمیل (اختیاری)';
  static const String businessPhone = 'شماره تماس';
  // Required by the backend validator (BusinessDescription .NotEmpty) — must
  // NOT be labelled optional.
  static const String businessDescription = 'توضیحات کسب‌و‌کار';

  // Step 2 — category
  static const String categoryTitle = 'دسته‌بندی کسب‌و‌کار';
  static const String categorySubtitle = 'نوع کسب‌و‌کار خود را انتخاب کنید';

  // Step 3 — location
  static const String locationTitle = 'موقعیت مکانی';
  static const String locationSubtitle = 'آدرس کسب‌و‌کار خود را وارد کنید';
  static const String addressLine1 = 'آدرس';
  static const String city = 'شهر';
  static const String cityHint = 'برای یافتن شهر جستجو کنید';
  // No province INPUT field: the province is derived from the selected city
  // (the backend validator still requires it). Kept only as a preview label.
  static const String province = 'استان';
  static const String mapLabel = 'موقعیت روی نقشه';
  static const String mapHint = 'روی نقشه بزنید تا موقعیت دقیق کسب‌وکار مشخص شود';
  static const String cityLoadError = 'بارگذاری فهرست شهرها ناموفق بود';
  static const String citiesLoading = 'در حال بارگذاری شهرها...';

  // Step 4 — services
  static const String servicesTitle = 'خدمات';
  static const String servicesSubtitle = 'خدماتی که ارائه می‌دهید را اضافه کنید';
  static const String addService = 'افزودن خدمت';
  static const String serviceName = 'نام خدمت';
  static const String serviceDuration = 'مدت زمان (دقیقه)';
  static const String servicePrice = 'قیمت (تومان)';
  static const String noServicesYet = 'هنوز خدمتی اضافه نشده است';
  static const String save = 'ذخیره';
  static const String cancel = 'انصراف';

  // Step 5 — working hours
  static const String hoursTitle = 'ساعات کاری';
  static const String hoursSubtitle = 'روزها و ساعات کاری خود را مشخص کنید';
  static const String openLabel = 'باز';
  static const String closedLabel = 'تعطیل';
  // Breaks (mirrors the Vue DayScheduleEditor)
  static const String breaksLabel = 'استراحت‌ها';
  static const String addBreak = 'افزودن استراحت';
  static const String noBreaks = 'بدون استراحت';
  static const String removeBreak = 'حذف استراحت';
  static const String copyToAllDays = 'کپی به همه روزها';
  static const String hoursCopied = 'ساعات کاری به همه روزها کپی شد';
  // Validation
  static String closeAfterOpenError(String day) =>
      'ساعت پایان باید بعد از ساعت شروع باشد ($day)';
  static String breakWithinHoursError(String day) =>
      'زمان استراحت باید در محدوده ساعات کاری باشد ($day)';
  static String breakEndAfterStartError(String day) =>
      'پایان استراحت باید بعد از شروع آن باشد ($day)';
  static const List<String> weekDays = [
    'یکشنبه',
    'دوشنبه',
    'سه‌شنبه',
    'چهارشنبه',
    'پنجشنبه',
    'جمعه',
    'شنبه',
  ];

  // Step 6 — gallery
  static const String galleryTitle = 'گالری تصاویر';
  static const String gallerySubtitle =
      'افزودن تصاویر اختیاری است و می‌توانید بعداً از پنل آن را تکمیل کنید';
  static const String galleryEmptyCaption = 'هنوز تصویری اضافه نشده است';
  static const String addPhotos = 'افزودن تصاویر';
  static const String galleryUploading = 'در حال آپلود تصاویر...';
  static String galleryCount(int n) => '$n تصویر';
  static String galleryLimit(int max) =>
      'حداکثر $max تصویر می‌توانید اضافه کنید';
  static const String galleryUploadError =
      'آپلود تصاویر ناموفق بود. لطفاً دوباره تلاش کنید';
  static const String mainImage = 'عکس اصلی';
  static const String setAsMainImage = 'انتخاب به عنوان عکس اصلی';
  static const String mainImageHint =
      'عکس اصلی به عنوان تصویر شاخص کسب‌وکار نمایش داده می‌شود';

  // Shared feedback states
  static const String retry = 'تلاش مجدد';

  // Step 7 — preview
  static const String previewTitle = 'بررسی نهایی';
  static const String previewSubtitle = 'اطلاعات خود را بررسی و تأیید کنید';

  // Step 8 — completion
  static const String completionTitle = 'ثبت‌نام تکمیل شد!';
  static const String completionBody =
      'ثبت‌نام کسب‌و‌کار شما با موفقیت انجام شد.';
  static const String goToDashboard = 'ورود به داشبورد';

  static const String onboardingSaved = 'اطلاعات شما ذخیره شد';

  // Dashboard (placeholder until the dashboard epic lands)
  static const String dashboardTitle = 'داشبورد';
  static const String dashboardWelcome = 'به پنل کسب‌وکار خوش آمدید';

  // ==================== Home (Today workspace) ====================
  // Microcopy per PROVIDER_HOME_SCREEN_DESIGNS.md.

  // App bar / greeting
  static const String homeGreetingMorning = 'صبح بخیر';
  static const String homeGreetingAfternoon = 'ظهر بخیر';
  static const String homeGreetingEvening = 'عصر بخیر';

  // Banners
  static const String homePendingBannerTitle =
      'کسب‌وکار شما در حال بررسی است';
  static const String homePendingBannerBody =
      'تا زمان تأیید، برای مشتریان قابل‌رزرو نیست؛ می‌توانید همچنان پروفایل خود را کامل کنید.';
  static const String homeOfflineBanner = 'اتصال اینترنت برقرار نیست';
  static const String homeStaleBanner = 'نمایش آخرین اطلاعات ذخیره‌شده';
  static const String homeVerifiedBannerTitle = 'کسب‌وکار شما فعال شد!';
  static const String contactSupport = 'تماس با پشتیبانی';

  // Activation checklist (Setup)
  static const String homeChecklistTitle = 'راه‌اندازی کسب‌وکار';
  static String homeChecklistProgress(int done, int total) =>
      '$done از $total انجام شد';
  static const String homeChecklistServices = 'خدمات و قیمت‌ها';
  static const String homeChecklistStaff = 'افزودن اعضای تیم';
  static const String homeChecklistGallery = 'افزودن تصاویر گالری';
  static const String homeChecklistShare = 'اشتراک‌گذاری لینک رزرو';
  static const String homeChecklistDone = 'انجام شد';

  // Get discovered (Growth)
  static const String homeDiscoverTitle = 'کسب‌وکار شما آماده است';
  static const String homeDiscoverBody =
      'برای دریافت اولین نوبت، لینک رزرو را به‌اشتراک بگذارید';
  static const String homeShareLink = 'اشتراک‌گذاری لینک رزرو';
  static String homeProfileCompleteness(String pct) => 'تکمیل پروفایل $pct٪';
  static const String homeAddWalkIn = 'افزودن نوبت دستی';

  // Agenda
  static const String homeAgendaTitle = 'برنامهٔ امروز';
  static String homeAgendaCount(int n) => '$n نوبت';
  static const String homeAgendaEmptyTitle = 'امروز نوبتی ندارید';
  static const String homeAgendaEmptyBodySetup =
      'اولین نوبت را به‌صورت دستی ثبت کنید';
  static String homeNextAppt(String when) => 'نوبت بعدی: $when';
  static const String homeAddAppointment = 'افزودن نوبت';
  static const String homeStatusDone = 'انجام شد';
  static const String homeStatusNoShow = 'عدم حضور';
  static const String homeStatusPending = 'در انتظار تأیید';
  static const String homeStatusNow = 'اکنون';

  // Now / next
  static const String homeNowLabel = 'اکنون';
  static const String homeNextLabel = 'بعدی';
  static const String homeActionComplete = 'تکمیل';
  static const String homeActionNoShow = 'عدم حضور';
  static const String homeActionCall = 'تماس';

  // Action queue (requests)
  static const String homeRequestsTitle = 'درخواست‌های در انتظار';
  static const String homeConfirm = 'تأیید';
  static const String homeDecline = 'رد';
  static const String homeDeclineReason = 'توسط کسب‌وکار رد شد';
  static const String homeAllCaughtUp = 'همه رسیدگی شد';
  static const String homeConfirmed = 'نوبت تأیید شد';
  static const String homeDeclined = 'نوبت رد شد';
  static const String homeCompleted = 'نوبت تکمیل شد';
  static const String homeNoShowMarked = 'عدم حضور ثبت شد';

  // End of day
  static String homeEndOfDay(int n) => 'کارِ امروز تمام شد — $n نوبت انجام شد';

  // Coming up
  static String homeTomorrowCount(int n) => 'فردا $n نوبت';
  static const String homeTomorrowEmpty = 'فردا نوبتی ثبت نشده';

  // Create action (⊕)
  static const String homeCreateTitle = 'افزودن';
  static const String homeCreateAppointment = 'نوبت جدید';
  static const String homeCreateBlockTime = 'مسدود کردن زمان';
  static const String comingSoon = 'به‌زودی در دسترس قرار می‌گیرد';

  // Account sheet
  static const String homeAccountTitle = 'حساب کسب‌وکار';
  static String providerStatusLabel(String status) {
    switch (status) {
      case 'PendingVerification':
        return 'در انتظار تأیید';
      case 'Verified':
      case 'Active':
        return 'فعال';
      default:
        return status;
    }
  }

  // Bottom nav
  static const String navHome = 'خانه';
  static const String navCalendar = 'تقویم';
  static const String navClients = 'مشتریان';
  static const String navMore = 'بیشتر';

  // Errors
  static const String homeLoadError = 'بارگذاری ناموفق بود';
  static const String linkCopied = 'لینک رزرو کپی شد';

  // ==================== Booking composer ====================
  static const String composerTitle = 'نوبت جدید';
  static const String composerServiceLabel = 'خدمت';
  static const String composerStaffLabel = 'کارمند';
  static const String composerDateLabel = 'تاریخ';
  static const String composerSlotsLabel = 'زمان‌های خالی';
  static const String composerPickService = 'انتخاب خدمت';
  static const String composerPickStaff = 'انتخاب کارمند';
  static const String composerToday = 'امروز';
  static const String composerTomorrow = 'فردا';
  static const String composerNoSlots =
      'در این روز زمان خالی وجود ندارد؛ روز دیگری را امتحان کنید';
  static const String composerSlotsError = 'دریافت زمان‌های خالی ناموفق بود';
  static const String composerClientName = 'نام مشتری (اختیاری)';
  static const String composerClientPhone = 'شماره مشتری (اختیاری)';
  static const String composerNotes = 'یادداشت (اختیاری)';
  static const String composerSubmit = 'ثبت نوبت';
  static const String composerCreated = 'نوبت با موفقیت ثبت شد';
  static String composerServiceMeta(String name, int minutes) =>
      '$name · $minutes دقیقه';

  // ==================== Calendar ====================
  static const String calendarTitle = 'تقویم';
  static const String calendarToday = 'امروز';
  static String calendarWeekOf(String from, String to) => 'از $from تا $to';
  static const String calendarEmptyDay = 'در این روز نوبتی ندارید';
  static const String calendarPrevWeek = 'هفتهٔ قبل';
  static const String calendarNextWeek = 'هفتهٔ بعد';
  static const String bookingSheetTitle = 'جزئیات نوبت';
  static const String bookingSheetNotes = 'یادداشت';

  // ==================== Clients ====================
  static const String clientsTitle = 'مشتریان';
  static String clientsCount(int n) => '$n مشتری';
  static const String clientsSearchHint = 'جستجوی نام یا شماره';
  static const String clientsEmptyTitle = 'هنوز مشتری‌ای ثبت نشده';
  static const String clientsEmptyBody =
      'با ثبت و دریافت نوبت، فهرست مشتریان شما ساخته می‌شود';
  static const String clientsSearchEmpty = 'مشتری‌ای مطابق جستجو یافت نشد';
  static const String clientUnknownName = 'مشتری';
  static String clientBookings(int total, int upcoming) =>
      '$total نوبت · $upcoming پیش رو';
  static String clientLastVisit(String date) => 'آخرین مراجعه: $date';
  static const String clientBookAgain = 'ثبت نوبت';
  static const String phoneCopied = 'شماره کپی شد';

  // ==================== More hub ====================
  static const String moreTitle = 'بیشتر';
  static const String moreBusinessSection = 'کسب‌وکار';
  static const String moreAccountSection = 'حساب کاربری';
  static const String moreServices = 'خدمات';
  static const String moreStaff = 'تیم';
  static const String moreInsights = 'گزارش‌ها';
  static const String moreBusinessProfile = 'مشخصات کسب‌وکار';
  static const String moreWorkingHours = 'ساعات کاری';
  static const String moreGallery = 'گالری';
  static const String moreShareLink = 'اشتراک‌گذاری لینک رزرو';
  static const String servicesEmpty = 'خدمتی ثبت نشده است';
  static const String staffEmpty = 'عضوی برای تیم ثبت نشده است';
  static const String staffInactive = 'غیرفعال';
  static String serviceMeta(int minutes, String price) =>
      '$minutes دقیقه · $price';
  static const String insightsAllTime = 'از ابتدا';
  static const String insightsLast30 = '۳۰ روز اخیر';
  static const String insightsTotal = 'کل نوبت‌ها';
  static const String insightsCompleted = 'انجام‌شده';
  static const String insightsCancelled = 'لغوشده';
  static const String insightsNoShow = 'عدم حضور';
  static const String insightsTurnover = 'گردش مالی';
  static const String insightsCompletedRevenue = 'درآمد تکمیل‌شده';

  // Errors (mirrors error.interceptor.ts / customer app)
  static const String networkError =
      'خطا در برقراری ارتباط با سرور. لطفاً اتصال اینترنت خود را بررسی کنید';
  static const String genericError = 'خطای نامشخصی رخ داده است';
  static const String sendCodeError = 'خطا در ارسال کد تأیید';
  static const String verifyCodeError = 'کد وارد شده صحیح نیست';
  static const String offline = 'اتصال اینترنت برقرار نیست';
}
