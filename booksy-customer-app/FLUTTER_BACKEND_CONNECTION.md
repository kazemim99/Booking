# راهنمای اتصال Flutter App به Backend در Visual Studio

## مشکل: "اتصال به سرور برقرار نشد"

این راهنما برای حل مشکل اتصال اپلیکیشن Flutter به Backend است که در Visual Studio اجرا می‌شود.

## ✅ تنظیمات انجام شده

### 1. IP Address به‌روزرسانی شد

فایل: `lib/core/api/config/api_constants.dart`

```dart
static const String baseUrl = 'http://172.20.105.136:5000';
```

**توجه**: اگر IP کامپیوتر شما تغییر کرد، این آدرس را به‌روزرسانی کنید.

برای پیدا کردن IP فعلی:
```powershell
ipconfig
# یا
powershell -Command "Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias 'Wi-Fi'"
```

## 🚀 مراحل اجرا

### مرحله ۱: اجرای Backend Services در Visual Studio

باید **سه سرویس** را به صورت همزمان اجرا کنید:

#### روش A: اجرای تک‌تک (پیشنهادی برای Debug)

1. **UserManagement API** (پورت 5001):
   ```
   - راست کلیک روی پروژه Booksy.UserManagement.API
   - Debug → Start New Instance
   - پروفایل: http (نه https)
   - باید ببینید: "Now listening on: http://0.0.0.0:5001"
   ```

2. **ServiceCatalog API** (پورت 5002):
   ```
   - راست کلیک روی پروژه Booksy.ServiceCatalog.Api
   - Debug → Start New Instance
   - پروفایل: http
   - باید ببینید: "Now listening on: http://0.0.0.0:5002"
   ```

3. **Gateway** (پورت 5000):
   ```
   - راست کلیک روی پروژه Booksy.Gateway
   - Debug → Start New Instance
   - پروفایل: http
   - باید ببینید: "Starting Ocelot API Gateway..." و "Now listening on: http://0.0.0.0:5000"
   ```

#### روش B: Multiple Startup Projects

1. راست کلیک روی Solution
2. "Configure Startup Projects"
3. انتخاب "Multiple startup projects"
4. سه پروژه زیر را روی **Start** قرار دهید:
   - `Booksy.UserManagement.API`
   - `Booksy.ServiceCatalog.Api`
   - `Booksy.Gateway`
5. OK → F5

### مرحله ۲: بررسی پورت‌ها

در PowerShell یا CMD بررسی کنید که همه پورت‌ها Listening هستند:

```powershell
netstat -ano | findstr "500[0-2]"
```

باید ببینید:
```
TCP    0.0.0.0:5000    ...    LISTENING    <PID>
TCP    0.0.0.0:5001    ...    LISTENING    <PID>
TCP    0.0.0.0:5002    ...    LISTENING    <PID>
```

### مرحله ۳: تنظیم Firewall (فقط یک بار لازم است)

**با دسترسی Administrator** این دستور را اجرا کنید:

```powershell
# راست کلیک روی PowerShell → Run as Administrator
cd C:\Repos\Booking\booksy-customer-app
.\setup-firewall.ps1
```

یا به صورت دستی:
```powershell
# باز کردن PowerShell با Administrator
New-NetFirewallRule -DisplayName "Booksy Backend Ports" `
    -Direction Inbound `
    -LocalPort 5000,5001,5002 `
    -Protocol TCP `
    -Action Allow `
    -Profile Private,Public
```

### مرحله ۴: تست اتصال

قبل از اجرای Flutter app، بررسی کنید که API ها کار می‌کنند:

```bash
# تست Gateway
curl http://localhost:5000/api/v1/Categories/popular

# تست از روی شبکه (با IP واقعی)
curl http://172.20.105.136:5000/api/v1/Categories/popular
```

اگر خروجی JSON دریافت کردید، عالی است! ✅

### مرحله ۵: اجرای Flutter App

```bash
cd C:\Repos\Booking\booksy-customer-app

# اجرا روی دستگاه واقعی یا emulator
flutter run
```

## 🔍 عیب‌یابی مشکلات رایج

### خطا: "اتصال به سرور برقرار نشد"

**علل احتمالی**:

#### 1. Backend اجرا نشده است
```powershell
# بررسی کنید
netstat -ano | findstr "5000"

# اگر خروجی نداد، Backend اجرا نشده است
# به مرحله ۱ برگردید
```

#### 2. IP Address اشتباه است
```powershell
# IP فعلی خود را پیدا کنید
ipconfig

# در فایل api_constants.dart آدرس را به‌روزرسانی کنید
# مثلا اگر IP جدید 192.168.1.100 است:
static const String baseUrl = 'http://192.168.1.100:5000';
```

#### 3. Firewall ترافیک را مسدود می‌کند

برای تست موقت Firewall را خاموش کنید (فقط برای تست!):
```
Windows Defender Firewall → Turn Windows Defender Firewall on or off
→ Turn off (Private network)
```

اگر کار کرد، یعنی مشکل از Firewall است. دوباره روشن کنید و مرحله ۳ را انجام دهید.

#### 4. Visual Studio روی پروفایل https اجرا شده

مطمئن شوید که پروفایل **http** انتخاب شده (نه https):
- در Visual Studio، کنار دکمه Run/Debug، باید "http" نوشته باشد
- اگر https است، آن را به http تغییر دهید

#### 5. پورت قبلاً استفاده می‌شود

```powershell
# پیدا کردن process که پورت را اشغال کرده
netstat -ano | findstr :5000

# کشتن process (PID را جایگزین کنید)
taskkill /F /PID <PID>
```

### خطا: "502 Bad Gateway"

**علت**: Gateway اجرا شده اما UserManagement یا ServiceCatalog اجرا نشده.

**راه‌حل**: مطمئن شوید هر سه سرویس (5000, 5001, 5002) اجرا هستند.

### Android Emulator اتصال ندارد

اگر از Android Emulator استفاده می‌کنید، IP آدرس متفاوت است:

```dart
// برای Emulator از این استفاده کنید:
static const String baseUrl = 'http://10.0.2.2:5000';

// برای Physical Device از این استفاده کنید:
static const String baseUrl = 'http://172.20.105.136:5000';
```

## 📋 Checklist کامل

قبل از اجرای Flutter app:

- [ ] Visual Studio اجرا شده است
- [ ] سه پروژه Backend اجرا هستند (UserManagement, ServiceCatalog, Gateway)
- [ ] پروفایل **http** انتخاب شده (نه https)
- [ ] پورت‌ها در حال Listening هستند (`netstat -ano | findstr "500[0-2]"`)
- [ ] IP Address در `api_constants.dart` صحیح است
- [ ] Firewall تنظیم شده یا خاموش است (برای تست)
- [ ] تست `curl http://localhost:5000/api/v1/Categories/popular` موفقیت‌آمیز است
- [ ] Flutter app اجرا شده است (`flutter run`)

## 🎯 آدرس‌های مهم

| سرویس | آدرس محلی | آدرس شبکه | Swagger |
|-------|-----------|-----------|---------|
| Gateway | http://localhost:5000 | http://172.20.105.136:5000 | http://localhost:5000/swagger |
| UserManagement | http://localhost:5001 | http://172.20.105.136:5001 | http://localhost:5001/swagger |
| ServiceCatalog | http://localhost:5002 | http://172.20.105.136:5002 | http://localhost:5002/swagger |

## 📱 تست روی دستگاه واقعی

اگر از گوشی واقعی استفاده می‌کنید:

1. مطمئن شوید گوشی و کامپیوتر به **یک Wi-Fi** متصل هستند
2. IP کامپیوتر را پیدا کنید (`ipconfig`)
3. در `api_constants.dart` آدرس را به‌روزرسانی کنید
4. Firewall را تنظیم کنید (مرحله ۳)
5. Flutter app را اجرا کنید

## 🆘 همچنان کار نمی‌کند؟

1. لاگ‌های Visual Studio را بررسی کنید (Output window)
2. لاگ‌های Flutter را بررسی کنید:
   ```bash
   flutter run --verbose
   ```
3. Dio interceptor لاگ‌ها را بررسی کنید (در Console اپلیکیشن)
4. مطمئن شوید که Antivirus ترافیک را مسدود نکرده است

## 📚 فایل‌های مرتبط

- تنظیمات API: `lib/core/api/config/api_constants.dart`
- راهنمای Visual Studio: `../VISUAL_STUDIO_DEBUG.md`
- تنظیمات Gateway: `../src/APIGateway/Booksy.Gateway/Configuration/ocelot.development.json`
- Script Firewall: `setup-firewall.ps1`
