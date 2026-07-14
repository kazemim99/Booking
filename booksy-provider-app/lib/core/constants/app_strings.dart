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
