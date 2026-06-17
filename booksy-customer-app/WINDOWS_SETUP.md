# Flutter Customer App - Windows Setup Guide

## Prerequisites Check

Before starting, verify you have:
- ✅ Windows 10 or later
- ✅ At least 10GB free disk space
- ✅ Administrator access
- ✅ Internet connection

## Step 1: Install Flutter SDK

### Download Flutter

1. Download Flutter SDK for Windows:
   ```
   https://docs.flutter.dev/get-started/install/windows
   ```

   Or direct download (latest stable):
   ```
   https://storage.googleapis.com/flutter_infra_release/releases/stable/windows/flutter_windows_3.27.1-stable.zip
   ```

2. Extract the zip file to a permanent location (NOT in Program Files):
   ```
   Recommended: C:\flutter
   ```

### Add Flutter to PATH

**Option A: Using PowerShell (Recommended)**

Open PowerShell as Administrator and run:

```powershell
# Add Flutter to PATH permanently
[System.Environment]::SetEnvironmentVariable(
    "Path",
    [System.Environment]::GetEnvironmentVariable("Path", "User") + ";C:\flutter\bin",
    "User"
)
```

**Option B: Using GUI**

1. Search for "Environment Variables" in Windows
2. Click "Edit the system environment variables"
3. Click "Environment Variables"
4. Under "User variables", select "Path" and click "Edit"
5. Click "New" and add: `C:\flutter\bin`
6. Click "OK" on all dialogs

### Verify Installation

Open a NEW Command Prompt or Git Bash and run:

```bash
flutter --version
```

You should see:
```
Flutter 3.27.1 • channel stable
...
```

## Step 2: Run Flutter Doctor

Check what else you need to install:

```bash
flutter doctor
```

You'll see a checklist like:
```
Doctor summary (to see all details, run flutter doctor -v):
[✓] Flutter (Channel stable, 3.27.1)
[✗] Android toolchain - develop for Android devices
    ✗ Android SDK is not installed
[✗] Chrome - develop for the web
    ✗ Chrome.exe not found
[✗] Visual Studio - develop Windows apps
[✗] Android Studio (not installed)
[!] VS Code
```

## Step 3: Install Android Studio (For Android Development)

### Download and Install

1. Download Android Studio:
   ```
   https://developer.android.com/studio
   ```

2. Run the installer with default settings

3. During first launch, Android Studio will download:
   - Android SDK
   - Android SDK Platform
   - Android Virtual Device

### Configure Flutter in Android Studio

1. Open Android Studio
2. Go to **Plugins** (File → Settings → Plugins)
3. Search for "Flutter" and install
4. Search for "Dart" and install (will be installed automatically with Flutter)
5. Restart Android Studio

### Set Android SDK Path

If needed, configure Flutter to find Android SDK:

```bash
flutter config --android-sdk "C:\Users\YourUsername\AppData\Local\Android\Sdk"
```

## Step 4: Accept Android Licenses

```bash
flutter doctor --android-licenses
```

Type `y` to accept all licenses.

## Step 5: Create Android Virtual Device (Emulator)

### Using Android Studio GUI

1. Open Android Studio
2. Click **More Actions** → **Virtual Device Manager**
3. Click **Create Device**
4. Select **Pixel 7** (or any phone)
5. Click **Next**
6. Select **System Image**: Choose latest (e.g., API 34 - Android 14)
7. Click **Download** if needed
8. Click **Next** → **Finish**

### Start the Emulator

From Android Studio: Click the green play button next to your device

Or from command line:
```bash
# List available emulators
flutter emulators

# Launch emulator
flutter emulators --launch Pixel_7_API_34
```

## Step 6: Install Project Dependencies

Navigate to the project directory and install Flutter packages:

```bash
cd c:\Repos\Booking\booksy-customer-app

# Install dependencies
flutter pub get
```

Expected output:
```
Running "flutter pub get" in booksy-customer-app...
Resolving dependencies...
+ bloc 8.1.4
+ cached_network_image 3.3.1
+ connectivity_plus 5.0.2
...
Got dependencies!
```

## Step 7: Run Code Generation

Generate required code for JSON serialization, Retrofit, and Dependency Injection:

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

This will create `*.g.dart` files throughout the project.

Expected output:
```
[INFO] Generating build script...
[INFO] Generating build script completed, took 442ms
[INFO] Running build...
[INFO] Running build completed, took 12.3s
```

## Step 8: Verify Setup

Check everything is ready:

```bash
flutter doctor -v
```

You should see mostly green checkmarks:
```
[✓] Flutter (Channel stable, 3.27.1)
[✓] Android toolchain - develop for Android devices (Android SDK version 34.0.0)
[✓] Chrome - develop for the web
[✓] Android Studio (version 2024.2)
[✓] VS Code (version 1.95.3)
[✓] Connected device (2 available)
```

## Step 9: Run the Flutter App

### Check Available Devices

```bash
flutter devices
```

You should see your emulator or connected device:
```
2 connected devices:

sdk gphone64 arm64 (mobile) • emulator-5554 • android-arm64  • Android 14 (API 34) (emulator)
Chrome (web)                 • chrome        • web-javascript • Google Chrome 130.0.6723.117
```

### Run the App

**Start the emulator first**, then run:

```bash
# Run on the first available device
flutter run

# Or specify a device
flutter run -d emulator-5554

# Or run in release mode (faster)
flutter run --release
```

Expected output:
```
Launching lib\main.dart on sdk gphone64 arm64 in debug mode...
Running Gradle task 'assembleDebug'...
✓ Built build\app\outputs\flutter-apk\app-debug.apk.
Installing build\app\outputs\flutter-apk\app-debug.apk...
Syncing files to device sdk gphone64 arm64...

Flutter run key commands.
r Hot reload.
R Hot restart.
h List all available interactive commands.
d Detach (terminate "flutter run" but leave application running).
c Clear the screen
q Quit (terminate the application on the device).

💪 Running with sound null safety 💪

An Observatory debugger and profiler on sdk gphone64 arm64 is available at: http://127.0.0.1:54321/
The Flutter DevTools debugger and profiler on sdk gphone64 arm64 is available at: http://127.0.0.1:9101?uri=http://127.0.0.1:54321/
```

## Step 10: Test the App

Once the app launches on the emulator:

### Test Authentication Flow

1. **Splash Screen**
   - Should show for 2-3 seconds
   - Auto-checks authentication status

2. **Login Screen**
   - Enter phone number: `09123456789`
   - Click "ارسال کد تایید" (Send Verification Code)
   - Should navigate to OTP screen

3. **OTP Screen**
   - Enter 6-digit code (from backend SMS)
   - Click "تایید و ادامه" (Verify and Continue)
   - Should navigate to Home screen

4. **Home Screen**
   - Should display welcome message
   - Show user phone number
   - Bottom navigation should be visible

### Test Hot Reload

While the app is running:

1. Edit a file (e.g., change text in `lib/features/auth/presentation/pages/login_page.dart`)
2. Press `r` in the terminal
3. Changes should appear instantly

## Troubleshooting

### Issue: "flutter: command not found"

**Solution**: Restart your terminal after adding Flutter to PATH

### Issue: "Android SDK not found"

**Solution**:
```bash
flutter config --android-sdk "C:\Users\YourUsername\AppData\Local\Android\Sdk"
```

### Issue: "No connected devices"

**Solution**:
1. Start Android emulator from Android Studio
2. Wait 30 seconds for emulator to boot
3. Run `flutter devices` again

### Issue: Gradle build fails

**Solution**:
```bash
cd android
./gradlew clean
cd ..
flutter clean
flutter pub get
flutter run
```

### Issue: "Waiting for another flutter command to release the startup lock"

**Solution**:
```bash
# Delete lock file
del "%LOCALAPPDATA%\Pub\Cache\.flutter_tool_state"

# Or on Git Bash
rm $HOME/AppData/Local/Pub/Cache/.flutter_tool_state
```

### Issue: Code generation fails

**Solution**:
```bash
flutter clean
flutter pub get
flutter pub run build_runner clean
flutter pub run build_runner build --delete-conflicting-outputs
```

## Development Workflow

### Daily Development

```bash
# 1. Start emulator (or connect physical device)
flutter emulators --launch Pixel_7_API_34

# 2. Run app in debug mode
flutter run

# 3. Make code changes and hot reload (press 'r')

# 4. If you change models/APIs, regenerate code:
flutter pub run build_runner build --delete-conflicting-outputs
```

### Testing on Physical Device

1. Enable Developer Options on your Android phone
2. Enable USB Debugging
3. Connect phone via USB
4. Run: `flutter run`
5. Select your device from the list

## API Configuration

The app connects to your backend API. Update the base URL if needed:

**File**: `lib/core/api/config/api_constants.dart`

```dart
class ApiConstants {
  // For production backend
  static const String baseUrl = 'http://napstar.ir';

  // For local development (use 10.0.2.2 for Android emulator)
  // static const String baseUrl = 'http://10.0.2.2:5000';
}
```

## Next Steps

After successful setup:

1. ✅ Test authentication flow
2. ✅ Explore the codebase structure
3. ✅ Read [README.md](README.md) for architecture overview
4. ✅ Start implementing Phase 2 features
5. ✅ Connect to real backend API

## Useful Commands

```bash
# Check Flutter version
flutter --version

# Update Flutter to latest
flutter upgrade

# List all devices
flutter devices

# List all emulators
flutter emulators

# Clean build cache
flutter clean

# Analyze code for issues
flutter analyze

# Format code
dart format lib/

# Run tests
flutter test

# Build APK for release
flutter build apk --release

# Build for iOS (macOS only)
flutter build ios --release
```

## Resources

- **Flutter Documentation**: https://docs.flutter.dev
- **Flutter Packages**: https://pub.dev
- **BLoC Documentation**: https://bloclibrary.dev
- **Retrofit**: https://pub.dev/packages/retrofit
- **Flutter Community**: https://flutter.dev/community

## Getting Help

If you encounter issues:

1. Check `flutter doctor -v` for detailed diagnostics
2. Read error messages carefully
3. Search Flutter documentation
4. Check GitHub issues
5. Ask in Flutter Discord/Slack

## Success!

If you see the login screen on your emulator, you're all set! 🎉

The Flutter customer app is now running and ready for development.
