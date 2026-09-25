import '../utils/persian_digits.dart';

/// Centralized Persian (fa-IR) strings for the Provider app.
/// Strings mirror the Vue provider auth screens where equivalents exist.
class AppStrings {
  AppStrings._();

  // App
  static const String appName = 'آسان رزرو | پنل کسب‌وکار';

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
  static const String logoutConfirmTitle = 'خروج از حساب';
  static const String logoutConfirmBody =
      'آیا مطمئن هستید که می‌خواهید از حساب کاربری خود خارج شوید؟';

  // Generic confirm dialog default (callers usually pass a specific label)
  static const String confirm = 'تأیید';

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
  static const String businessInfoSubtitle =
      'اطلاعات کسب‌و‌کار خود را وارد کنید';
  static const String businessName = 'نام کسب‌و‌کار';
  static const String ownerFirstName = 'نام مالک';
  static const String ownerLastName = 'نام خانوادگی مالک';
  // One field for the owner's full name (split into first/last before sending —
  // see core/utils/person_name.dart).
  static const String ownerFullName = 'نام و نام خانوادگی مالک';
  static const String ownerFullNameHint = 'مثلاً علی رضایی';

  /// Names the still-empty required fields, so the user never has to hunt.
  static String completeTheseFields(List<String> fields) =>
      'این فیلدها را کامل کنید: ${fields.join('، ')}';

  static const String fieldRequired = 'این فیلد الزامی است';
  static const String fullNameNeedsBoth =
      'لطفاً نام و نام خانوادگی را کامل وارد کنید';
  static const String businessPhoneFromAccount =
      'این شماره هنگام ورود تأیید شده و قابل تغییر نیست';
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
  static const String mapHint =
      'روی نقشه بزنید تا موقعیت دقیق کسب‌وکار مشخص شود';
  static const String cityLoadError = 'بارگذاری فهرست شهرها ناموفق بود';
  static const String citiesLoading = 'در حال بارگذاری شهرها...';

  // Step 4 — services
  static const String servicesTitle = 'خدمات';
  static const String servicesSubtitle =
      'خدماتی که ارائه می‌دهید را اضافه کنید';
  static const String addService = 'افزودن خدمت';
  static const String serviceName = 'نام خدمت';
  static const String serviceDuration = 'مدت زمان (دقیقه)';
  static const String servicePrice = 'قیمت (تومان)';
  static const String noServicesYet = 'هنوز خدمتی اضافه نشده است';

  // Photo upload progress (gallery).
  static const String uploadQueued = 'در صف';
  static const String uploadFailed = 'آپلود نشد';
  static const String uploadRetry = 'تلاش دوباره';
  static const String uploadCancelAll = 'لغو همه';
  static const String uploadAllDone = 'همه‌ی عکس‌ها آپلود شد';
  static String uploadRemaining(int remaining, int total) =>
      '$remaining از $total عکس در حال آپلود…';

  // Suggested-services catalogue (onboarding step 4).
  static const String searchServices = 'جستجوی خدمت';
  static const String searchServicesHint = 'مثلاً کراتینه';
  static const String suggestedPrice = 'قیمت پیشنهادی';
  static const String toman = 'تومان';
  static const String customServices = 'خدمات دلخواه';
  static const String servicesPickHint =
      'خدمات خود را از فهرست زیر انتخاب کنید و در صورت نیاز قیمت را تغییر دهید';
  static String servicesSelectedCount(int count) =>
      '$count خدمت انتخاب شده است';
  static const String save = 'ذخیره';

  // ---- Reviews (provider-reviews-and-ratings) ----
  static const String reviewsTitle = 'نظرهای مشتریان';
  static const String reviewsNoneYet = 'هنوز نظری ندارد';
  static const String reviewsEmpty = 'هنوز نظری برای کسب‌وکار شما ثبت نشده است';
  static const String reviewsLoadFailed = 'دریافت نظرها ناموفق بود';
  static String reviewsPublishedCount(int count) => '${PersianDigits.toPersian('$count')} نظر منتشرشده';
  static String reviewsAwaitingReply(int count) => '${PersianDigits.toPersian('$count')} نظر منتظر پاسخ شما';
  static const String reviewStatusPending = 'در انتظار تأیید';
  static const String reviewStatusPublished = 'منتشر شده';
  static const String reviewStatusRejected = 'رد شده';
  static const String reviewStatusHidden = 'پنهان شده';
  static const String reviewPendingNoReply =
      'پس از تأیید این نظر می‌توانید به آن پاسخ دهید';
  static const String replyAction = 'پاسخ';
  static const String replyEditAction = 'ویرایش پاسخ';
  static const String replyRemoveAction = 'حذف پاسخ';
  static const String yourReply = 'پاسخ شما';
  static const String replyStatusPending = 'پاسخ شما در انتظار تأیید است و هنوز نمایش داده نمی‌شود';
  static const String replyStatusPublished = 'پاسخ شما منتشر شده است';
  static String replyStatusRejected(String reason) => 'پاسخ شما رد شد: $reason — می‌توانید آن را بازنویسی کنید';
  static const String replyDialogTitle = 'پاسخ به نظر';
  static const String replyHint = 'پاسخ شما (حداکثر ۱۰۰۰ حرف)';
  static const String replyRequired = 'متن پاسخ را بنویسید';
  static const String replySend = 'ارسال پاسخ';
  static const String replySent = 'پاسخ ارسال شد و پس از تأیید نمایش داده می‌شود';
  static const String replyRemoved = 'پاسخ حذف شد';
  static const String cancel = 'انصراف';

  // ---- Discounts and campaigns (add-discounts-and-campaigns) ----
  static const String promotionsTitle = 'تخفیف‌ها';
  static const String promotionsMine = 'تخفیف‌های من';
  static const String promotionsCampaigns = 'کمپین‌ها';
  static const String promotionsLoadFailed = 'دریافت تخفیف‌ها ناموفق بود';
  static const String promotionsEmpty = 'هنوز تخفیفی تعریف نکرده‌اید';
  static const String promotionsEmptyHint =
      'با تخفیف ساعت‌های خلوت، اولین نوبت مشتری جدید یا یک کد برای مشتریان وفادار، نوبت‌های بیشتری بگیرید.';
  static const String promotionsNew = 'تخفیف جدید';
  static const String promotionsEdit = 'ویرایش تخفیف';
  static const String promotionsSaved = 'تخفیف ذخیره شد';
  static const String promotionsCampaignsEmpty = 'فعلاً کمپینی برای پیوستن وجود ندارد';
  static const String promotionsCampaignsHint =
      'کمپین‌های آسان‌رزرو اختیاری‌اند: اگر بپیوندید، تخفیف کمپین روی نوبت‌های جدید سالن شما اعمال می‌شود و هزینه آن با سالن است.';
  static const String promotionsJoin = 'پیوستن';
  static const String promotionsLeave = 'خروج';
  static const String promotionsJoined = 'عضو هستید';
  static const String promotionsJoinedToast = 'به کمپین پیوستید';
  static const String promotionsLeftToast = 'از کمپین خارج شدید';
  static const String promotionsLeaveConfirmTitle = 'خروج از کمپین';
  static const String promotionsLeaveConfirmBody =
      'از این به بعد تخفیف کمپین روی نوبت‌های جدید شما اعمال نمی‌شود. نوبت‌های ثبت‌شده تغییری نمی‌کنند.';
  static const String promotionsPause = 'توقف';
  static const String promotionsResume = 'ادامه';
  static const String promotionsEnd = 'پایان';
  static const String promotionsEndConfirmTitle = 'پایان تخفیف';
  static const String promotionsEndConfirmBody =
      'این تخفیف برای همیشه پایان می‌یابد. نوبت‌هایی که با آن ثبت شده‌اند تغییری نمی‌کنند.';
  static const String promotionsPausedByPlatform = 'توسط پشتیبانی متوقف شده است';
  static const String promotionsStateScheduled = 'زمان‌بندی‌شده';
  static const String promotionsStateActive = 'فعال';
  static const String promotionsStatePaused = 'متوقف';
  static const String promotionsStateExpired = 'منقضی';
  static const String promotionsStateExhausted = 'ظرفیت تکمیل';
  static const String promotionsStateEnded = 'پایان‌یافته';
  static const String promotionsAutomatic = 'خودکار';
  static const String promotionsWithCode = 'با کد';
  static const String promotionsNewCustomers = 'فقط مشتری جدید';
  static const String promotionsAllServices = 'همه خدمات';
  static String promotionsServiceCount(int n) => '${PersianDigits.toPersian('$n')} خدمت';
  static String promotionsMinimum(String amount) => 'حداقل $amount تومان';
  static String promotionsHours(String from, String to) =>
      'ساعت ${PersianDigits.toPersian(from)} تا ${PersianDigits.toPersian(to)}';
  static String promotionsPerCustomer(int n) => 'هر مشتری ${PersianDigits.toPersian('$n')} بار';
  static String promotionsUsage(int uses, int? limit, String discount) => limit == null
      ? '${PersianDigits.toPersian('$uses')} بار استفاده · $discount تومان تخفیف داده‌اید'
      : '${PersianDigits.toPersian('$uses')} از ${PersianDigits.toPersian('$limit')} استفاده · $discount تومان تخفیف داده‌اید';
  static String promotionsDaysLeft(int days) =>
      days <= 0 ? 'امروز تمام می‌شود' : '${PersianDigits.toPersian('$days')} روز مانده';
  static String promotionsStartsIn(int days) =>
      days <= 0 ? 'امروز شروع می‌شود' : '${PersianDigits.toPersian('$days')} روز دیگر شروع می‌شود';
  static const String promotionsNoEnd = 'بدون تاریخ پایان';

  // Form
  static const String promotionFormTitle = 'عنوان تخفیف';
  static const String promotionFormTitleHint = 'مثلاً: تخفیف صبح‌های وسط هفته';
  static const String promotionFormHow = 'نحوه اعمال';
  static const String promotionFormHowAutomatic = 'خودکار روی قیمت';
  static const String promotionFormHowCode = 'با کد تخفیف';
  static const String promotionFormHowAutomaticHint = 'به همه مشتری‌ها روی صفحه سالن نشان داده و خودکار اعمال می‌شود.';
  static const String promotionFormHowCodeHint = 'فقط کسی که کد را دارد هنگام رزرو از آن استفاده می‌کند.';
  static const String promotionFormCode = 'کد تخفیف';
  static const String promotionFormCodeGenerate = 'ساخت کد';
  static const String promotionFormCodeLocked = 'این کد استفاده شده و قابل تغییر نیست';
  static const String promotionFormKind = 'نوع تخفیف';
  static const String promotionFormPercent = 'درصدی';
  static const String promotionFormFixed = 'مبلغ ثابت';
  static const String promotionFormPercentValue = 'درصد تخفیف';
  static const String promotionFormAmountValue = 'مبلغ تخفیف (تومان)';
  static const String promotionFormCap = 'سقف تخفیف (تومان، اختیاری)';
  static const String promotionFormServices = 'روی کدام خدمات؟';
  static const String promotionFormWhen = 'چه زمانی؟';
  static const String promotionFormDaysHint = 'روزی انتخاب نکنید یعنی همه روزها';
  static const String promotionFormHoursToggle = 'فقط ساعت‌های مشخص (مثلاً ساعت‌های خلوت)';
  static const String promotionFormFrom = 'از';
  static const String promotionFormTo = 'تا';
  static const String promotionFormDuration = 'مدت اعتبار';
  static const String promotionFormKeepEnd = 'تاریخ پایان فعلی';
  static const String promotionFormMore = 'شرایط بیشتر';
  static const String promotionFormMinimum = 'حداقل مبلغ نوبت (تومان)';
  static const String promotionFormNewCustomers = 'فقط برای اولین نوبت مشتری در سالن شما';
  static const String promotionFormTotalLimit = 'سقف کل استفاده';
  static const String promotionFormPerCustomer = 'سقف استفاده هر مشتری';
  static const String promotionFormUnlimited = 'نامحدود';
  static const String promotionFormPreview = 'مشتری این‌طور می‌بیند';
  static const String promotionFormSave = 'ذخیره تخفیف';
  static const String promotionFormFundingNote = 'هزینه تخفیف با سالن است و مبلغ نهایی نوبت همان مبلغ پس از تخفیف است.';
  static const List<String> promotionDurations = ['بدون پایان', '۱ هفته', '۲ هفته', '۱ ماه', '۳ ماه'];
  static const Map<int, String> weekdayShort = {
    6: 'ش', 0: 'ی', 1: 'د', 2: 'س', 3: 'چ', 4: 'پ', 5: 'ج',
  };

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

  // Step 7 — "do you personally provide services?" onboarding branch
  static const String providesServicesQuestion =
      'آیا خودتان خدمات ارائه می‌دهید؟';
  static const String providesServicesHint =
      'اگر بله، شما به عنوان اولین عضو تیم اضافه می‌شوید و می‌توانید نوبت بگیرید.';
  static const String providesServicesYes = 'بله، خودم خدمات ارائه می‌دهم';
  static const String providesServicesNo = 'خیر، فقط کسب‌وکار را مدیریت می‌کنم';

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
  static const String homePendingBannerTitle = 'کسب‌وکار شما در حال بررسی است';
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

  /// Growth-zone copy while the business is still awaiting approval. It must
  /// not claim the business is "ready": customers cannot book it yet, and the
  /// pending banner sits directly above this card.
  static const String homeDiscoverPendingTitle = 'پروفایل شما کامل است';
  static const String homeDiscoverPendingBody =
      'پس از تأیید کسب‌وکار، می‌توانید لینک رزرو را به‌اشتراک بگذارید.';
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
  static const String homeStatusConfirmed = 'قطعی';
  static const String homeStatusCancelled = 'لغو شده';
  static const String homeStatusNow = 'اکنون';

  // Now / next
  static const String homeNowLabel = 'اکنون';
  static const String homeNextLabel = 'بعدی';
  static const String homeActionComplete = 'تکمیل';
  static const String homeActionNoShow = 'عدم حضور';
  static const String homeNextAwaitsConfirmation = 'این درخواست منتظر تأیید شماست.';
  // Why a confirmed booking still ahead offers no «تکمیل» (openspec/changes/_inline/customer-reviews-and-nahal-seed).
  static const String homeCompleteLaterHint =
      'از ۱۵ دقیقه پیش از شروع نوبت می‌توانید آن را «انجام‌شده» ثبت کنید؛ پس از آن مشتری می‌تواند برایتان نظر بنویسد.';
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

  // Notifications inbox
  static const String notificationsTitle = 'اعلان‌ها';
  static const String notificationsEmpty = 'هنوز اعلانی ندارید.';
  static const String notificationsLoadFailed = 'اعلان‌ها بارگذاری نشد.';
  static const String notificationsRetry = 'تلاش دوباره';
  static const String notificationsMarkAllRead = 'خواندن همه';
  static const String notificationsMarkFailed = 'علامت‌گذاری اعلان انجام نشد.';

  // Push on this device (the More row and the one-time card on Home)
  static const String pushEnableAction = 'فعال‌سازی اعلان‌ها';
  static const String pushEnableSubtitle = 'نوبت‌های جدید و لغو نوبت‌ها را روی همین دستگاه خبر می‌دهیم.';
  static const String pushEnabledTitle = 'اعلان‌ها روی این دستگاه فعال است';
  static const String pushEnabledSnack = 'اعلان‌ها فعال شد.';
  static const String pushBlockedTitle = 'اعلان‌ها روی این دستگاه مسدود است';
  static const String pushBlockedHint =
      'برای دریافت اعلان، از تنظیمات مرورگر یا گوشی اجازهٔ اعلان را برای پنل آسان رزرو بدهید.';
  static const String pushPromptTitle = 'از نوبت‌های جدید باخبر شوید';
  static const String pushPromptBody =
      'وقتی مشتری نوبت رزرو کرد، روی همین دستگاه خبرتان می‌کنیم تا زود تأییدش کنید.';
  static const String pushPromptLater = 'بعداً';
  static const String pushUnreachableTitle = 'اعلان‌ها هنوز به این دستگاه نمی‌رسد';
  static const String pushUnreachableHint =
      'اتصال به سرویس اعلان برقرار نشد. برای تلاش دوباره بزنید.';

  /// The action on a push that arrives while the app is open.
  static const String pushNoticeOpen = 'مشاهده';

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
  static const String composerServiceLabel = 'خدمات';
  static const String composerServicesHint = 'انتخاب خدمت‌ها';
  static String composerServicesCount(int n) => '$n خدمت انتخاب شده';
  static String composerServicesSummary(int minutes, String price) =>
      'مجموع: $minutes دقیقه · $price';
  static const String composerDone = 'تأیید';
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

  /// Shown when the business has no staff at all: no date can ever have a free
  /// time until someone is added, so we say so up front instead of letting the
  /// provider search date by date.
  static const String composerNoStaffTitle = 'هنوز کارمندی اضافه نکرده‌اید';
  static const String composerNoStaffBody =
      'برای ثبت نوبت باید حداقل یک کارمند داشته باشید؛ تا آن زمان هیچ زمان خالی نمایش داده نمی‌شود.';
  static const String composerNoStaffCta = 'افزودن کارمند';
  static const String composerClientName = 'نام مشتری';
  static const String composerClientPhone = 'شماره موبایل مشتری';
  static const String composerNotifyCustomer = 'ارسال پیامک تأیید برای مشتری';
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
      'مشتریان ثابت خود را اضافه کنید یا از مخاطبین گوشی انتخاب کنید؛ مشتریانی که آنلاین نوبت می‌گیرند هم اینجا می‌آیند';
  static const String clientsSearchEmpty = 'مشتری‌ای مطابق جستجو یافت نشد';
  static const String clientUnknownName = 'مشتری';
  static String clientBookings(int total, int upcoming) =>
      '$total نوبت · $upcoming پیش رو';
  static String clientLastVisit(String date) => 'آخرین مراجعه: $date';
  static const String clientBookAgain = 'ثبت نوبت';
  static const String phoneCopied = 'شماره کپی شد';

  // ==================== Customer book ====================
  static const String moreMyName = 'نام شما';
  static const String myNameTitle = 'نام و نام خانوادگی شما';
  static const String myNameHint =
      'این نامی است که همکاران و مشتریان شما می‌بینند';
  static const String myNameSaved = 'نام شما ذخیره شد';

  // A person with no real name — phone sign-in leaves «ارائه‌دهنده <digits>». The number is never shown as a
  // name (production QA 2026-09-23); these stand in, and the phone appears only labelled as the phone.
  static const String ownNameMissing = 'نام شما ثبت نشده';
  static const String memberNameMissing = 'بدون نام';
  static String phoneLabeled(String phone) => 'موبایل $phone';
  static String unnamedWithPhone(String phone) =>
      '$memberNameMissing · ${phoneLabeled(phone)}';

  // Asked once, right after OTP, when the account has no real name.
  static const String completeNameTitle = 'نام شما';
  static const String completeNameSubtitle =
      'نام و نام خانوادگی خود را وارد کنید تا مشتریان و همکاران شما را با نام بشناسند، نه با شماره موبایل.';
  static const String completeNameSkip = 'بعداً';
  static const String completeNameSaveFailed =
      'ذخیره نام ناموفق بود. دوباره تلاش کنید.';

  static const String customerAdd = 'افزودن مشتری';
  static const String customerEdit = 'ویرایش مشتری';
  static const String customerFirstName = 'نام';
  static const String customerLastName = 'نام خانوادگی';
  static const String customerPhone = 'شماره موبایل';
  static const String customerNotes = 'یادداشت';
  static const String customerPhoneInvalid = 'شماره موبایل معتبر نیست';
  static const String customerSaved = 'مشتری ذخیره شد';
  static const String customerRemoved = 'مشتری حذف شد';
  static const String customerRemove = 'حذف';
  static const String customerRemoveConfirm =
      'این مشتری از فهرست شما حذف شود؟ نوبت‌های گذشته‌اش باقی می‌ماند.';
  static const String customerSaveToBook = 'ذخیره در فهرست مشتریان';
  static const String customerImportContacts = 'از مخاطبین گوشی';
  static const String customerPickSaved = 'انتخاب از مشتریان';
  static const String customerPickContact = 'انتخاب از مخاطبین';
  static const String customerPickerTitle = 'انتخاب مشتری';
  static const String customerFromContacts = 'از مخاطبین';
  static String customerImportResult(int added, int alreadySaved, int invalid) => [
        '$added مشتری اضافه شد',
        if (alreadySaved > 0) '$alreadySaved مورد از قبل در فهرست بود',
        if (invalid > 0) '$invalid مورد شماره معتبر نداشت',
      ].join(' · ');
  static const String customerContactsNothing =
      'مخاطبی با شماره موبایل انتخاب نشد';

  // Onboarding: optional customers step
  static const String onboardingCustomersTitle = 'مشتریان شما';
  static const String onboardingCustomersBody =
      'اگر مشتری ثابت دارید، همین حالا اضافه‌شان کنید تا ثبت نوبت برایشان سریع باشد. این مرحله اختیاری است.';
  static String onboardingCustomersAdded(int n) => '$n مشتری اضافه شد';

  // ==================== More hub ====================
  static const String moreTitle = 'بیشتر';
  static const String moreBusinessSection = 'کسب‌وکار';
  static const String moreAccountSection = 'حساب کاربری';
  static const String moreServices = 'خدمات';
  static const String moreStaff = 'تیم';
  static const String moreMemberships = 'سالن‌های من';
  static const String membershipsEmpty = 'شما عضو هیچ سالنی نیستید';
  static const String membershipOwner = 'مالک';
  static const String membershipProvidesServices = 'ارائه‌دهنده خدمات';
  static String membershipSwitched(String org) => 'به «$org» تغییر کرد';
  static const String moreInsights = 'گزارش‌ها';
  static const String moreBusinessProfile = 'مشخصات کسب‌وکار';
  static const String moreWorkingHours = 'ساعات کاری';
  static const String moreGallery = 'گالری';
  static const String moreShareLink = 'اشتراک‌گذاری لینک رزرو';
  static const String servicesEmpty = 'خدمتی ثبت نشده است';
  static const String serviceAdd = 'افزودن خدمت';
  static const String serviceEdit = 'ویرایش خدمت';
  // (serviceName/serviceDuration/servicePrice reuse the onboarding strings.)
  static const String serviceDescription = 'توضیحات (اختیاری)';
  static const String serviceSave = 'ذخیره';
  static const String serviceAdded = 'خدمت ثبت شد';
  static const String serviceUpdated = 'خدمت ویرایش شد';
  static const String serviceRemoved = 'خدمت حذف شد';
  static const String serviceRemoveConfirmTitle = 'حذف خدمت';
  static String serviceRemoveConfirmBody(String name) =>
      'خدمت «$name» حذف شود؟';
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

  // ==================== Staff management ====================
  static const String staffAdd = 'افزودن عضو تیم';
  static const String staffEdit = 'ویرایش عضو تیم';
  static const String staffFirstName = 'نام';
  static const String staffLastName = 'نام خانوادگی (اختیاری)';
  static const String staffPhone = 'شماره تماس (اختیاری)';
  static const String staffRole = 'نقش (اختیاری)';
  static const String staffSave = 'ذخیره';
  static const String staffRemove = 'حذف عضو';
  static const String staffRemoveConfirmTitle = 'حذف عضو تیم';
  static String staffRemoveConfirmBody(String name) =>
      '«$name» از تیم حذف شود؟';
  static const String staffRemoveConfirm = 'حذف';
  static const String staffAdded = 'عضو تیم اضافه شد';

  // Invite-by-phone (membership model)
  static const String staffInvite = 'دعوت با شماره موبایل';
  static const String staffInviteHint =
      'یک پیامک دعوت ارسال می‌شود. اگر این شماره از قبل حساب داشته باشد، همان حساب استفاده می‌شود.';
  static const String staffInviteNameOptional = 'نام (اختیاری)';
  static const String staffInviteSend = 'ارسال دعوت';
  static const String staffInviteSent = 'دعوت‌نامه ارسال شد';
  static const String staffInvitePending = 'در انتظار پذیرش';
  static const String staffPendingInvitations = 'دعوت‌های در انتظار';
  static const String staffInvitationCancel = 'لغو دعوت';
  static const String staffInvitationCancelConfirmTitle = 'لغو دعوت؟';
  static String staffInvitationCancelConfirmBody(String who) =>
      'دعوت $who لغو می‌شود و لینک آن دیگر کار نمی‌کند.';
  static const String staffInvitationCancelled = 'دعوت لغو شد';

  // Accept-invitation screen (from an SMS link)
  static const String acceptInvitationTitle = 'پذیرش دعوت';
  static const String acceptInvitationNotFound =
      'دعوت‌نامه یافت نشد یا منقضی شده است';
  static String acceptInvitationInvitedTo(String org) =>
      'شما به «$org» دعوت شده‌اید';
  static const String acceptInvitationAccept = 'پذیرش و پیوستن';
  static const String acceptInvitationAccepted = 'شما به تیم پیوستید';
  static const String acceptInvitationGoDashboard = 'رفتن به داشبورد';
  static const String acceptInvitationExpired = 'این دعوت‌نامه دیگر معتبر نیست';
  static const String acceptInvitationLoginPrompt =
      'برای پذیرش دعوت، ابتدا وارد حساب خود شوید.';
  static const String acceptInvitationLogin = 'ورود';
  static const String acceptInvitationNoAccountPrompt = 'حساب کاربری ندارید؟';
  static const String acceptInvitationRegisterInstead = 'ثبت‌نام کنید';

  // Register-and-accept screen (new invitee, no account yet)
  static const String registerAcceptTitle = 'ثبت‌نام و پیوستن';
  static const String registerAcceptFirstName = 'نام';
  static const String registerAcceptLastName = 'نام خانوادگی';
  static const String registerAcceptEmailOptional = 'ایمیل (اختیاری)';
  static const String registerAcceptSendCode = 'ارسال کد تایید';
  static String registerAcceptCodeSentTo(String maskedPhone) =>
      'کد تایید به شماره $maskedPhone ارسال شد';
  static const String registerAcceptVerifying = 'در حال بررسی...';
  static const String registerAcceptChangeInfo = 'ویرایش اطلاعات';
  static const String registerAcceptSuccess = 'ثبت‌نام شما با موفقیت انجام شد';
  static const String registerAcceptSuccessHint =
      'برای ورود به حساب خود، شماره تلفن خود را وارد کنید.';
  static const String registerAcceptGoToLogin = 'ورود به حساب';
  static const String registerAcceptFieldsRequired =
      'لطفاً نام و نام خانوادگی را وارد کنید';

  // ==================== Business profile editing ====================
  static const String businessProfileName = 'نام کسب‌وکار';
  static const String businessProfileDescription = 'توضیحات (اختیاری)';
  static const String businessProfileSave = 'ذخیره';
  static const String businessProfileSaved = 'مشخصات کسب‌وکار ذخیره شد';

  // ==================== Working hours editing ====================
  static const String hoursSave = 'ذخیرهٔ ساعات کاری';
  static const String hoursSaved = 'ساعات کاری ذخیره شد';
  static const String hoursClosedDay = 'تعطیل';
  static const String hoursBreak = 'استراحت';
  static const String hoursAddBreak = 'استراحت';
  static const String hoursBreakInvalid =
      'پایان استراحت باید بعد از شروع آن باشد';

  // ==================== Holidays ====================
  static const String moreHolidays = 'تعطیلات و مرخصی';
  static const String holidaysEmpty = 'تعطیلی‌ای ثبت نشده است';
  static const String holidayAdd = 'ثبت تعطیلی';
  static const String holidayReason = 'دلیل (مثلاً مرخصی)';
  static const String holidayRecurring = 'هر سال تکرار شود';
  static const String holidayRecurringBadge = 'سالانه';
  static const String holidayPickDate = 'انتخاب تاریخ';
  static const String holidaySave = 'ثبت';
  static const String holidayAdded = 'تعطیلی ثبت شد';
  static const String holidayRemoved = 'تعطیلی حذف شد';
  static const String holidayRemoveConfirmTitle = 'حذف تعطیلی';
  static String holidayRemoveConfirmBody(String date) =>
      'تعطیلی $date حذف شود؟';

  // ==================== Gallery management ====================
  static const String galleryEmpty = 'هنوز تصویری بارگذاری نشده';
  static const String galleryEmptyBody =
      'با افزودن تصاویر، پروفایل شما برای مشتریان جذاب‌تر می‌شود';
  static const String galleryUpload = 'افزودن تصویر';
  static const String galleryUploaded = 'تصاویر بارگذاری شد';
  static const String galleryPrimaryBadge = 'اصلی';
  static const String gallerySetPrimary = 'انتخاب به‌عنوان تصویر اصلی';
  static const String galleryPrimarySet = 'تصویر اصلی تغییر کرد';
  static const String galleryRemove = 'حذف تصویر';
  static const String galleryRemoved = 'تصویر حذف شد';
  static const String galleryRemoveConfirmTitle = 'حذف تصویر';
  static const String galleryRemoveConfirmBody = 'این تصویر از گالری حذف شود؟';

  // ==================== Block time (availability exceptions) ====================
  static const String blockTimeTitle = 'مسدود کردن زمان';
  static const String blockTimeAllDay = 'تمام روز تعطیل';
  static const String blockTimeFrom = 'باز از';
  static const String blockTimeTo = 'تا';
  static const String blockTimeReason = 'دلیل';
  static const String blockTimeSubmit = 'مسدود کردن';
  static const String blockTimeCreated = 'زمان مسدود شد';
  static const String exceptionsSection = 'ساعات استثنائی';
  static const String exceptionClosedAllDay = 'تمام روز تعطیل';
  static const String exceptionRemoveConfirmTitle = 'حذف ساعت استثنائی';
  static String exceptionRemoveConfirmBody(String date) =>
      'ساعت استثنائی $date حذف شود؟';
  static const String exceptionRemoved = 'ساعت استثنائی حذف شد';

  // Banner copy for availability states (now reachable).
  static const String homeClosedTodayBanner = 'امروز تعطیل هستید';
  static const String homeClosedTodayBody =
      'امروز در تقویم شما تعطیل ثبت شده و نوبت جدیدی پذیرفته نمی‌شود.';
  static const String homeVacationBanner = 'در حالت مرخصی هستید';
  static const String staffUpdated = 'عضو تیم ویرایش شد';
  static const String staffRemoved = 'عضو تیم حذف شد';

  // Errors (mirrors error.interceptor.ts / customer app)
  static const String networkError =
      'خطا در برقراری ارتباط با سرور. لطفاً اتصال اینترنت خود را بررسی کنید';
  static const String genericError = 'خطای نامشخصی رخ داده است';
  static const String sendCodeError = 'خطا در ارسال کد تأیید';
  static const String verifyCodeError = 'کد وارد شده صحیح نیست';
  static const String offline = 'اتصال اینترنت برقرار نیست';
}
