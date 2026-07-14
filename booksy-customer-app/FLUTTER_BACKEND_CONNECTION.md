# راهنمای اتصال Flutter App به Backend

این راهنما نحوه اجرای Backend و اتصال اپلیکیشن Flutter (روی وب/Chrome، Android Emulator یا دستگاه واقعی) را توضیح می‌دهد.

> **نکته معماری**: Backend یک **modular monolith** است — یک پروسه واحد (`Booksy.Host` / پورت 5000) که هر دو bounded context (UserManagement و ServiceCatalog) را در خودش دارد. دیگر Gateway جداگانه یا سرویس‌های جداگانه روی پورت‌های 5001/5002 وجود ندارد.

## ✅ آدرس Backend به صورت خودکار تنظیم می‌شود

فایل `lib/core/api/config/api_constants.dart` اکنون بر اساس پلتفرم اجرا، آدرس مناسب را خودش انتخاب می‌کند:

```dart
static String get baseUrl {
  if (kIsWeb) return 'http://localhost:5000';
  if (defaultTargetPlatform == TargetPlatform.android) return 'http://10.0.2.2:5000';
  return 'http://localhost:5000'; // iOS Simulator / Desktop
}
```

- **وب (Chrome/Edge)**: `localhost:5000` — نیازی به تغییر ندارد.
- **Android Emulator**: `10.0.2.2:5000` (آدرس مخصوص اتصال Emulator به localhost کامپیوتر میزبان) — به صورت خودکار تنظیم می‌شود.
- **iOS Simulator / Windows Desktop**: `localhost:5000` — نیازی به تغییر ندارد.
- **دستگاه واقعی (فیزیکی)**: نیاز به تنظیم دستی دارد — به بخش «تست روی دستگاه واقعی» مراجعه کنید.

## 🚀 مراحل اجرا

### مرحله ۱: اجرای Backend

فقط **یک پروژه** (`Booksy.Host`) کافی است:

```bash
# از ریشه ریپازیتوری
OTP_SANDBOX_CODE=123456 Sms__SandboxMode=true ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project src/Host/Booksy.Host
```

یا در Visual Studio: پروژه `Booksy.Host` را به عنوان Startup Project انتخاب کنید، پروفایل **http** را انتخاب کنید و F5 بزنید — باید `http://localhost:5000` و صفحه Swagger باز شود.

Postgres و Redis هم باید بالا باشند (`docker-compose -f docker-compose.infrastructure.yml up -d` — جزئیات در `../VISUAL_STUDIO_DEBUGGING.md`).

### مرحله ۲: بررسی سلامت Backend

```powershell
curl http://localhost:5000/health
curl http://localhost:5000/api/v1/Categories/popular
```

اگر خروجی JSON دریافت کردید، عالی است! ✅

### مرحله ۳: اجرای Flutter App

**روی وب (Chrome)** — اولین بار پلتفرم وب را اضافه کنید:
```bash
cd C:\Repos\Booking\booksy-customer-app
flutter create --platforms=web .   # فقط بار اول لازم است
flutter run -d chrome --web-port=5173
```
پورت `5173` را عمداً انتخاب کنید — Backend از قبل این پورت را در `Cors:AllowedOrigins` مجاز کرده است (`src/Host/Booksy.Host/appsettings.json`)، پس نیازی به تغییر تنظیمات CORS نیست.

**روی Android Emulator یا دستگاه واقعی**:
```bash
cd C:\Repos\Booking\booksy-customer-app
flutter run
```

## 🔍 عیب‌یابی مشکلات رایج

### خطا: "اتصال به سرور برقرار نشد"

1. **Backend اجرا نشده است** — بررسی کنید: `netstat -ano | findstr :5000` (اگر خروجی نداد، به مرحله ۱ برگردید)
2. **پروفایل https به جای http** — در Visual Studio مطمئن شوید پروفایل **http** انتخاب شده
3. **پورت قبلاً استفاده می‌شود**:
   ```powershell
   netstat -ano | findstr :5000
   taskkill /F /PID <PID>
   ```

### خطای CORS در وب (Chrome DevTools Console)

اگر روی وب اجرا می‌کنید و خطای CORS دیدید، مطمئن شوید با `--web-port=5173` اجرا کرده‌اید (یا یکی از پورت‌های مجاز دیگر: `3000`, `3001`, `7002` — لیست کامل در `src/Host/Booksy.Host/appsettings.json` زیر `Cors:AllowedOrigins`).

### خطا: "502 Bad Gateway"

دیگر معنایی ندارد — این خطا مخصوص معماری قدیمی Gateway بود. اگر `Booksy.Host` سالم است، این خطا رخ نمی‌دهد.

## 📱 تست روی دستگاه واقعی

اگر از گوشی واقعی استفاده می‌کنید:

1. مطمئن شوید گوشی و کامپیوتر به **یک Wi-Fi** متصل هستند
2. IP کامپیوتر را پیدا کنید: `ipconfig`
3. در `api_constants.dart`، در تابع `baseUrl`، یک شرط موقت برای IP خودتان اضافه کنید (مثلاً `'http://192.168.1.x:5000'`) — یا مقدار را مستقیماً جایگزین کنید و قبل از commit برگردانید
4. فایروال را برای پورت 5000 باز کنید:
   ```powershell
   # PowerShell با دسترسی Administrator
   New-NetFirewallRule -DisplayName "Booksy Backend" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow -Profile Private,Public
   ```
5. `flutter run` را اجرا کنید

## 🆘 همچنان کار نمی‌کند؟

1. لاگ‌های Backend را در Seq بررسی کنید: `http://localhost:5341`
2. لاگ‌های Flutter را بررسی کنید: `flutter run --verbose`
3. Dio interceptor لاگ‌ها را در Console اپلیکیشن بررسی کنید
4. مطمئن شوید Antivirus/Firewall ترافیک را مسدود نکرده است

## 📚 فایل‌های مرتبط

- تنظیمات API: `lib/core/api/config/api_constants.dart`
- راهنمای Visual Studio: `../VISUAL_STUDIO_DEBUGGING.md`
- تنظیمات CORS: `../src/Host/Booksy.Host/appsettings.json` (`Cors:AllowedOrigins`)
