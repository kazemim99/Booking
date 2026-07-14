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
  static const String descriptionOptional = 'توضیحات (اختیاری)';

  // Step 2 — category
  static const String categoryTitle = 'دسته‌بندی کسب‌و‌کار';
  static const String categorySubtitle = 'نوع کسب‌و‌کار خود را انتخاب کنید';

  // Step 3 — location
  static const String locationTitle = 'موقعیت مکانی';
  static const String locationSubtitle = 'آدرس کسب‌و‌کار خود را وارد کنید';
  static const String addressLine1 = 'آدرس';
  static const String addressLine2 = 'آدرس تکمیلی (اختیاری)';
  static const String city = 'شهر';
  static const String province = 'استان';
  static const String postalCode = 'کد پستی (اختیاری)';

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

  // Errors (mirrors error.interceptor.ts / customer app)
  static const String networkError =
      'خطا در برقراری ارتباط با سرور. لطفاً اتصال اینترنت خود را بررسی کنید';
  static const String genericError = 'خطای نامشخصی رخ داده است';
  static const String sendCodeError = 'خطا در ارسال کد تأیید';
  static const String verifyCodeError = 'کد وارد شده صحیح نیست';
  static const String offline = 'اتصال اینترنت برقرار نیست';
}
