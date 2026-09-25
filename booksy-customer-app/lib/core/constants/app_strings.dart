import '../utils/persian_formatter.dart';

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

  /// Shown when the session is gone and the refresh token could not renew it —
  /// the user has to sign in again, so say that instead of leaking Dio's own
  /// untranslated "invalid status code of 401" text.
  static const String sessionExpiredError =
      'نشست شما به پایان رسیده است. لطفاً دوباره وارد شوید';
  static const String forbiddenError =
      'شما به این بخش دسترسی ندارید';
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

  // Discovery — nearby & area
  static const String nearMe = 'اطراف من';
  static const String nearbyTitle = 'سالن‌های اطراف';
  static const String nearbyEmpty = 'سالنی در این محدوده پیدا نشد';
  static const String searchByArea = 'جستجو در محله';
  static const String areaSearchHint = 'نام محله یا منطقه…';
  static const String areaNotFound = 'محله‌ای با این نام پیدا نشد';
  static const String locationPermissionNeeded =
      'برای نمایش سالن‌های نزدیک، اجازه دسترسی به موقعیت لازم است';
  static const String locationServiceDisabled =
      'موقعیت‌مکانی دستگاه خاموش است. آن را روشن کنید یا در محله جستجو کنید';

  // Provider detail
  static const String servicesTitle = 'خدمات';
  static const String workingHoursTitle = 'ساعات کاری';
  static const String aboutTitle = 'درباره';
  static const String bookAction = 'رزرو نوبت';
  static const String showOnMap = 'نمایش روی نقشه';
  static const String providerNotFound = 'سالن مورد نظر یافت نشد';

  // Booking flow
  static const String bookingSelectService = 'انتخاب خدمت';
  static const String bookingSelectServices = 'انتخاب خدمات';
  static const String bookingSelectServicesHint =
      'می‌توانید چند خدمت را همزمان انتخاب کنید';
  static const String bookingSelectAtLeastOneService =
      'حداقل یک خدمت انتخاب کنید';
  static const String bookingServicesSelectedSuffix = 'خدمت انتخاب شده';
  static const String bookingServiceSelectedA11y = 'انتخاب شده';
  static const String bookingTotalDuration = 'مدت کل';
  static const String bookingTotalPrice = 'مجموع';
  static const String bookingContinue = 'ادامه';
  static const String bookingSelectStaff = 'انتخاب ارائه‌دهنده';
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
  static const String bookingStaff = 'ارائه‌دهنده';
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

  // Checkout / payment
  static const String checkoutTitle = 'پرداخت بیعانه';
  static const String checkoutDepositLabel = 'مبلغ بیعانه';
  static const String checkoutTotalLabel = 'مبلغ کل';
  static const String checkoutRemainingLabel = 'پرداخت در محل';
  static const String checkoutPayCta = 'پرداخت آنلاین';
  static const String checkoutGatewayNotice =
      'برای پرداخت به درگاه بانکی منتقل می‌شوید. پس از پرداخت به برنامه بازگردید.';

  static const String checkoutAwaitingTitle = 'در انتظار پرداخت';
  static const String checkoutAwaitingBody =
      'پرداخت را در مرورگر کامل کنید. پس از پرداخت، وضعیت را بررسی کنید.';
  static const String checkoutCheckStatusCta = 'بررسی وضعیت پرداخت';
  static const String checkoutCancelPaymentCta = 'لغو پرداخت';

  static const String checkoutPaidTitle = 'پرداخت انجام شد';
  static const String checkoutPaidBody = 'بیعانه پرداخت شد و نوبت شما تایید است.';
  static const String checkoutRefNumberLabel = 'شماره پیگیری';

  static const String checkoutFailedTitle = 'پرداخت انجام نشد';
  static const String checkoutFailedBody = 'مبلغی از حساب شما کسر نشده است. می‌توانید دوباره تلاش کنید.';

  static const String checkoutUnknownTitle = 'وضعیت پرداخت مشخص نیست';
  static const String checkoutUnknownBody =
      'اگر مبلغی کسر شده باشد، پرداخت شما ثبت می‌شود؛ لطفاً دوباره پرداخت نکنید و کمی بعد وضعیت را بررسی کنید.';

  static const String checkoutNothingDueTitle = 'پرداخت آنلاین لازم نیست';
  static const String checkoutNothingDueBody = 'برای این نوبت بیعانه‌ای لازم نیست. هزینه در محل پرداخت می‌شود.';

  static const String checkoutPayLaterCta = 'پرداخت بعداً';

  // ---------------------------------------------------------------------
  // Home — redesigned discovery surface
  // ---------------------------------------------------------------------
  static const String homeTitle = 'بوکسی';
  static const String homeSearchHint = 'جستجوی آرایشگاه، سالن، اسپا…';
  static const String mapSearch = 'جستجو روی نقشه';

  // Notifications inbox
  static const String notificationsTitle = 'اعلان‌ها';
  static const String notificationsEmpty = 'هنوز اعلانی ندارید.';
  static const String notificationsLoadFailed = 'اعلان‌ها بارگذاری نشد.';
  static const String notificationsMarkAllRead = 'خواندن همه';
  static const String notificationsMarkFailed = 'علامت‌گذاری اعلان انجام نشد.';

  // Push on this device (the profile row and the one-time card after a booking)
  static const String pushEnableAction = 'فعال‌سازی اعلان‌ها';
  static const String pushEnableSubtitle = 'تأیید، تغییر یا لغو نوبت را روی همین دستگاه خبر می‌دهیم.';
  static const String pushEnabledTitle = 'اعلان‌ها روی این دستگاه فعال است';
  static const String pushEnabledSnack = 'اعلان‌ها فعال شد.';
  static const String pushBlockedTitle = 'اعلان‌ها روی این دستگاه مسدود است';
  static const String pushBlockedHint =
      'برای دریافت اعلان، از تنظیمات مرورگر یا گوشی اجازهٔ اعلان را برای بوکسی بدهید.';
  static const String pushPromptTitle = 'از تأیید نوبت باخبر شوید';
  static const String pushPromptBody = 'وقتی سالن نوبت شما را تأیید کرد، روی همین دستگاه خبرتان می‌کنیم.';
  static const String pushPromptLater = 'بعداً';
  static const String pushUnreachableTitle = 'اعلان‌ها هنوز به این دستگاه نمی‌رسد';
  static const String pushUnreachableHint =
      'اتصال به سرویس اعلان برقرار نشد. برای تلاش دوباره بزنید.';

  /// The action on a push that arrives while the app is open.
  static const String pushNoticeOpen = 'مشاهده';
  static const String nearestTitle = 'نزدیک‌ترین‌ها';
  static const String bookNowShort = 'رزرو';
  static const String viewProfile = 'مشاهده پروفایل';

  // Service-category tiles/chips — display labels only. Each label's API value
  // (the ServiceCategory enum name) lives next to it in `kServiceCategories`
  // so the Persian text is never sent on the wire.
  static const String categoryBarbershop = 'آرایشگاه مردانه';
  static const String categoryHairSalon = 'آرایشگاه زنانه';
  static const String categorySpa = 'اسپا';
  static const String categoryNailSalon = 'سالن ناخن';
  static const String categoryBeautySalon = 'پوست و زیبایی';
  static const String categoryMassage = 'ماساژ';
  static const String categoryMore = 'بیشتر';

  // ---------------------------------------------------------------------
  // Provider detail — redesigned profile surface
  // ---------------------------------------------------------------------
  static const String openNow = 'باز است';
  static const String closedDay = 'تعطیل';
  static const String contactAndLocationTitle = 'تماس و موقعیت';
  static const String breakTimeLabel = 'استراحت';
  static const String onBreakNow = 'در زمان استراحت';
  static String todayBreak(String from, String to) => 'استراحت امروز: $from تا $to';
  static const String mapLocationImpreciseNotice =
      'موقعیت دقیق شما به دست نیامد (اگر VPN روشن است، موقعیت کشور دیگری خوانده می‌شود). نام شهر یا محله را جستجو کنید.';
  // Asking a new customer for their name (QA walkthrough 2026-09-22): sign-up is a phone number only, so the
  // account starts as «مشتری <digits>» and the salon sees that on the booking.
  static const String completeNameTitle = 'نام شما';
  static const String completeNameSubtitle =
      'برای اینکه سالن بداند نوبت برای چه کسی است، نام و نام خانوادگی خود را وارد کنید.';
  static const String completeNameSkip = 'بعداً';
  static const String firstNameRequired = 'لطفاً نام خود را وارد کنید';
  static const String lastNameRequired = 'لطفاً نام خانوادگی خود را وارد کنید';
  // A booking needs a real name (QA recording 2026-09-23 #9): asked at the confirm step, with no skip there.
  static const String bookingNameTitle = 'نام شما برای این نوبت';
  static const String bookingNameBody =
      'سالن باید بداند نوبت برای چه کسی است. برای ثبت نوبت، نام و نام خانوادگی خود را وارد کنید.';
  static const String bookingNameSaveAndBook = 'ذخیره و ثبت نوبت';
  static const String reviewsTitle = 'نظرها';
  static const String reviewsEmpty = 'هنوز نظری ثبت نشده است';
  static const String reviewsLoadFailed = 'نظرها بارگذاری نشد.';
  static const String reviewsMore = 'نظرهای بیشتر';
  static String reviewsShowAll(String count) => 'مشاهده همه نظرها ($count)';
  static const String reviewVerifiedVisit = 'مراجعه تأییدشده';
  static String reviewDistributionLabel(String stars, String count) => '$stars ستاره: $count نظر';
  // "Where do I leave my review?" (QA recording 2026-09-23 #10).
  static const String reviewsHowToWrite =
      'پس از انجام نوبت، از همان نوبت در «$appointmentsTitle» می‌توانید برای این سالن نظر ثبت کنید.';
  static const String reviewAnonymous = 'مشتری';
  static const String reviewProviderReply = 'پاسخ سالن';
  static const String reviewWriteAction = 'ثبت نظر';
  static const String reviewDialogTitle = 'نظر شما دربارهٔ این سالن';
  static const String reviewRatingLabel = 'امتیاز شما';
  static const String reviewCommentLabel = 'نظر شما (اختیاری)';
  static const String reviewRatingRequired = 'لطفاً امتیاز را انتخاب کنید';
  static const String reviewCommentTooShort = 'نظر باید حداقل ۱۰ حرف باشد';
  static const String reviewSaved =
      'نظر شما ثبت شد و پس از تأیید نمایش داده می‌شود. ممنون!';
  static const String reviewEdited =
      'تغییرات ذخیره شد و پس از تأیید دوباره نمایش داده می‌شود';
  static const String reviewEditTitle = 'ویرایش نظر';
  static const String reviewEditNotice =
      'نظر ویرایش‌شده تا تأیید دوباره نمایش داده نمی‌شود';
  static const String reviewSaveAction = 'ذخیره';
  // Where a visit's review stands (openspec/changes/_inline/customer-reviews-and-nahal-seed).
  static const String reviewPromptTitle = 'تجربه‌تان چطور بود؟';
  static const String reviewPromptSubtitle =
      'با امتیاز و نظرتان به دیگران کمک کنید سالن مناسب را پیدا کنند.';
  static const String reviewSubmittedTitle = 'نظر شما ثبت شد';
  static const String reviewSubmittedPending =
      'در انتظار تأیید است و پس از تأیید برای همه نمایش داده می‌شود.';
  static const String reviewSubmittedPublished =
      'منتشر شده و دیگران آن را می‌بینند. ممنون از شما!';
  static const String reviewSubmittedRejected =
      'تأیید نشد؛ دلیلش را در «نظرهای من» ببینید.';
  static const String reviewSubmittedHidden =
      'فعلاً پنهان است؛ جزئیات در «نظرهای من».';
  static String reviewCardStatus(String status) => 'نظر شما: $status';
  static const String reviewDimensionsToggle = 'امتیاز جزئی‌تر (اختیاری)';
  static const String reviewHelpful = 'مفید بود';
  static const String reviewNotHelpful = 'مفید نبود';
  static const String noReviewsYet = 'هنوز نظری ندارد';
  static const String myReviewsTitle = 'نظرهای من';
  static const String myReviewsEmpty = 'شما هنوز نظری ثبت نکرده‌اید';
  static const String reviewStatusPending = 'در انتظار تأیید';
  static const String reviewStatusPublished = 'منتشر شده';
  static const String reviewStatusRejected = 'رد شده';
  static const String reviewStatusHidden = 'پنهان شده';
  static const String reviewEditedLabel = '(ویرایش شده)';
  static const String reviewEditAction = 'ویرایش';
  static String reviewModerationReason(String reason) => 'دلیل: $reason';
  static const String today = 'امروز';
  static const String tomorrow = 'فردا';
  static String freeSlotsOn(String when, int count) =>
      '$when ${PersianFormatter.formatNumber(count)} وقت خالی';
  static const String directionsAction = 'مسیریابی';
  static const String directionsSheetTitle = 'با کدام برنامه مسیریابی شود؟';
  static const String directionsNeshan = 'نشان';
  static const String directionsBalad = 'بلد';
  static const String directionsGoogleMaps = 'گوگل مپ';
  static const String providerPhoneLabel = 'تلفن تماس';
  static const String providerAddressLabel = 'نشانی';
  static const String noServicesYet = 'خدمتی برای این سالن ثبت نشده است';
  static String reviewCountLabel(String count) => '$count نظر';
  static String distanceKmLabel(String km) => '$km کیلومتر';

  // ---------------------------------------------------------------------
  // Map discovery — the map + carousel surface that replaced the
  // list-only "nearby" and "area" pages.
  // ---------------------------------------------------------------------
  static const String mapTitle = 'نقشه';
  static const String mapAreaSearchHint = 'نام شهر یا محله را وارد کنید';

  /// Where the app starts before the device location is known: Parsabad in
  /// Ardabil province, the launch city.
  static const String mapDefaultAreaLabel = 'پارس‌آباد، اردبیل';
  static const String mapSearchThisArea = 'جستجو در این محدوده';
  static const String mapMyLocation = 'موقعیت من';
  static const String mapEmptyTitle = 'سالنی در این محدوده پیدا نشد';
  static const String mapEmptySubtitle =
      'محدوده نقشه را تغییر دهید یا فیلتر دسته‌بندی را بردارید';
  static const String mapAreaNotFound = 'شهر یا محله‌ای با این نام پیدا نشد';
  static const String mapLocationFallbackNotice =
      'موقعیت شما در دسترس نیست؛ نقشه روی پارس‌آباد تنظیم شد';
  static const String mapAttribution = 'مشارکت‌کنندگان OpenStreetMap';
  static String mapClusterLabel(String count) => '$count سالن';
  static String mapPinLabel(String name) => 'نشانگر $name';
  static const String mapProvidersCarouselLabel = 'فهرست سالن‌های روی نقشه';

  // ---------------------------------------------------------------------
  // UX review 2026-09-23 (openspec/changes/customer-app-ux-review-fixes).
  // One block per slice so the slices never edit the same lines.
  // ---------------------------------------------------------------------

  // --- slice A: theme, navigation, formatting ---
  // --- end of slice A ---

  // --- slice B: web ---
  /// The browser tab and installed-app name: the app's existing Persian name and tagline (web/index.html and
  /// web/manifest.json carry the same text; test/config/web_shell_test.dart keeps them in step).
  static const String appDocumentTitle = '$homeTitle | $appTagline';
  // --- end of slice B ---

  // --- slice C: salon profile ---
  /// On a service cell of the salon profile: tapping it books that service.
  static const String serviceBookAction = 'رزرو این خدمت';
  static String serviceDurationMinutes(String minutes) => '$minutes دقیقه';
  static const String favoriteAdd = 'افزودن به علاقه‌مندی‌ها';
  static const String favoriteRemove = 'حذف از علاقه‌مندی‌ها';
  static const String favoriteAddFailed =
      'این سالن به علاقه‌مندی‌ها اضافه نشد. دوباره تلاش کنید.';
  static const String favoriteRemoveFailed =
      'این سالن از علاقه‌مندی‌ها حذف نشد. دوباره تلاش کنید.';
  // --- end of slice C ---

  // --- slice D: booking wizard ---
  static const String bookingMovedFromToday =
      'امروز وقت خالی ندارد؛ نزدیک‌ترین روز با وقت خالی انتخاب شد';
  static const String bookingMovedFromPickedDay =
      'این روز وقت خالی ندارد؛ نزدیک‌ترین روز با وقت خالی انتخاب شد';
  static const String bookingNoFreeDayInWindow =
      'تا پایان روزهای قابل رزرو این سالن وقت خالی پیدا نشد';
  static const String bookingFindNextFreeDay = 'نزدیک‌ترین روز با وقت خالی';
  static const String bookingDayClosed = 'تعطیل';
  static const String bookingWhatNextTitle = 'بعد از ثبت چه می‌شود؟';
  static const String bookingWhatNextBody =
      'درخواست شما با وضعیت «$statusPending» ثبت می‌شود. سالن آن را بررسی و تایید می‌کند و نتیجه به شما اطلاع داده می‌شود.';
  static const String bookingSuccessRequestedTitle = 'درخواست نوبت ثبت شد';
  static const String bookingSuccessAwaiting =
      'نوبت شما در انتظار تایید سالن است. پس از تایید به شما اطلاع می‌دهیم.';
  // --- end of slice D ---

  // --- slice E: appointments ---
  static const String appointmentDetailTitle = 'جزئیات نوبت';
  static const String appointmentViewSalon = 'مشاهده سالن';
  // What «در انتظار تایید» means (QA recording 2026-09-23 #8).
  static const String appointmentPendingExplanation =
      'سالن هنوز درخواست شما را تأیید نکرده؛ پس از تأیید به شما خبر می‌دهیم.';
  static const String appointmentsPastEmptyTitle = 'نوبت گذشته‌ای ندارید';
  static const String appointmentsPastEmptySubtitle =
      'نوبت‌هایی که انجام شوند اینجا نمایش داده می‌شوند';
  static const String bookAgain = 'رزرو مجدد';
  static String rescheduleCurrentTime(String when) => 'زمان فعلی: $when';
  // --- end of slice E ---

  // --- slice F: discovery cards, map, home ---
  static const String mapAreaSuggestionsLabel = 'پیشنهادهای شهر و محله';
  // --- end of slice F ---

  // --- slice G: reviews, inbox, profile, auth ---
  static const String relativeTimeNow = 'همین حالا';
  static String relativeMinutesAgo(String minutes) => '$minutes دقیقه پیش';
  static String relativeHoursAgo(String hours) => '$hours ساعت پیش';
  static const String relativeYesterday = 'دیروز';
  static String reviewStarLabel(String count) => '$count ستاره';
  static String reviewDimensionStarLabel(String dimension, String count) =>
      '$dimension، $count ستاره';
  static const String completeNameSaveFailed =
      'نام ذخیره نشد. دوباره تلاش کنید.';
  // --- end of slice G ---
}
