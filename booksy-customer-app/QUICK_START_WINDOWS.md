# Quick Start - Flutter Customer App (Windows)

## Prerequisites

You need Flutter installed. If you don't have it:

### Quick Flutter Installation (5 minutes)

1. **Download Flutter** (latest stable):
   ```
   https://storage.googleapis.com/flutter_infra_release/releases/stable/windows/flutter_windows_3.27.1-stable.zip
   ```

2. **Extract to** `C:\flutter`

3. **Add to PATH** (PowerShell as Admin):
   ```powershell
   [System.Environment]::SetEnvironmentVariable("Path", [System.Environment]::GetEnvironmentVariable("Path", "User") + ";C:\flutter\bin", "User")
   ```

4. **Verify** (in NEW terminal):
   ```bash
   flutter --version
   ```

## Automated Setup

Run the setup script:

```bash
# Using PowerShell
powershell -ExecutionPolicy Bypass -File setup.ps1

# Or using Command Prompt
setup.bat
```

This will:
- ✅ Check Flutter installation
- ✅ Run Flutter Doctor
- ✅ Install dependencies
- ✅ Generate code
- ✅ Check available devices

## Manual Setup

If you prefer manual steps:

```bash
# 1. Install packages
flutter pub get

# 2. Generate code
flutter pub run build_runner build --delete-conflicting-outputs

# 3. Check devices
flutter devices
```

## Run the App

### Step 1: Start Emulator

**Option A: Android Studio**
1. Open Android Studio
2. Click "Device Manager" (phone icon)
3. Click green play button on your emulator

**Option B: Command Line**
```bash
# List emulators
flutter emulators

# Launch emulator
flutter emulators --launch Pixel_7_API_34
```

### Step 2: Run App

```bash
# Run in debug mode
flutter run

# Or run in release mode (faster)
flutter run --release

# Or specify device
flutter run -d emulator-5554
```

## What to Test

Once the app launches:

### 1. Splash Screen ✅
- Should auto-navigate to Login

### 2. Login Screen ✅
- Enter phone: `09123456789`
- Click "ارسال کد تایید"
- Should show OTP screen

### 3. OTP Screen ✅
- Enter 6-digit code
- Click "تایید و ادامه"
- Should navigate to Home

### 4. Home Screen ✅
- Welcome message
- User info
- Bottom navigation

## Hot Reload

While app is running, make code changes and press:
- `r` - Hot reload (fast)
- `R` - Hot restart (full restart)
- `q` - Quit

## Common Issues

### "flutter: command not found"
→ Restart terminal after adding to PATH

### "No devices found"
→ Start Android emulator first

### Build fails
```bash
flutter clean
flutter pub get
flutter run
```

### "Waiting for another flutter command"
```bash
# Delete lock file
del "%LOCALAPPDATA%\Pub\Cache\.flutter_tool_state"
```

## Next Steps

1. ✅ Test authentication flow
2. ✅ Explore codebase (see [README.md](README.md))
3. ✅ Read architecture docs
4. ✅ Start developing features

## API Configuration

The app connects to: `http://napstar.ir`

To change API URL, edit:
```
lib/core/api/config/api_constants.dart
```

For local development (Android emulator):
```dart
static const String baseUrl = 'http://10.0.2.2:5000';
```

## Useful Commands

```bash
# Check everything is ready
flutter doctor -v

# Update Flutter
flutter upgrade

# Clean project
flutter clean

# Analyze code
flutter analyze

# Format code
dart format lib/

# Build APK
flutter build apk --release
```

## Resources

- [Full Setup Guide](WINDOWS_SETUP.md) - Detailed installation
- [README.md](README.md) - Architecture overview
- [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) - What's built
- Flutter Docs: https://docs.flutter.dev

## Need Help?

Check the detailed guide: [WINDOWS_SETUP.md](WINDOWS_SETUP.md)

---

**Estimated Time**: 15-30 minutes for first-time setup
