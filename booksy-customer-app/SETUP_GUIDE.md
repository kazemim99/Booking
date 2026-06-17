# Booksy Customer App - Setup Guide

Complete step-by-step guide to set up and run the Booksy Customer App.

## Prerequisites

### Required Software

1. **Flutter SDK** (version 3.0.0 or higher)
   - Download from: https://flutter.dev/docs/get-started/install
   - Verify installation: `flutter --version`

2. **Dart SDK** (comes with Flutter)
   - Verify: `dart --version`

3. **IDE** (choose one):
   - **VS Code** (recommended)
     - Install Flutter extension
     - Install Dart extension
   - **Android Studio**
     - Install Flutter plugin
     - Install Dart plugin

4. **Platform-Specific Tools**:

   **For Android:**
   - Android Studio
   - Android SDK (API level 21 or higher)
   - Android Emulator or physical device

   **For iOS (macOS only):**
   - Xcode (latest version)
   - CocoaPods: `sudo gem install cocoapods`
   - iOS Simulator or physical device

## Step 1: Clone the Repository

```bash
# If you have git access
git clone <repository-url>
cd booksy-customer-app

# Or navigate to existing directory
cd booksy-customer-app
```

## Step 2: Install Dependencies

```bash
# Install Flutter packages
flutter pub get
```

This will install all dependencies listed in `pubspec.yaml`.

## Step 3: Run Code Generation

The project uses code generation for JSON serialization, Retrofit API services, and dependency injection.

```bash
# Generate all required files
flutter pub run build_runner build --delete-conflicting-outputs
```

Expected output:
```
[INFO] Generating build script...
[INFO] Generating build script completed, took 1.2s
[INFO] Creating build script snapshot...
[INFO] Creating build script snapshot completed, took 3.4s
[INFO] Running build...
[INFO] Running build completed, took 10.5s
```

Generated files will have `.g.dart` extension:
- `*_models.g.dart` - JSON serialization code
- `*_api_service.g.dart` - Retrofit API implementation
- `injection.config.dart` - Dependency injection configuration

### Watch Mode (Optional)

For development, you can run code generation in watch mode:

```bash
flutter pub run build_runner watch
```

This will automatically regenerate files when you make changes.

## Step 4: Verify Setup

Check that Flutter can detect your devices:

```bash
flutter doctor
```

Expected output (all checkmarks):
```
Doctor summary (to see all details, run flutter doctor -v):
[✓] Flutter (Channel stable, 3.x.x, on macOS/Windows/Linux)
[✓] Android toolchain - develop for Android devices
[✓] Xcode - develop for iOS and macOS (macOS only)
[✓] Chrome - develop for the web
[✓] Android Studio (version 20xx.x)
[✓] VS Code (version 1.xx.x)
[✓] Connected device (X available)
```

Fix any issues before proceeding.

## Step 5: Configure API Endpoints

The app connects to the Booksy backend. Update the base URL if needed:

Edit: `lib/core/api/config/api_constants.dart`

```dart
class ApiConstants {
  // Change this to your backend URL
  static const String baseUrl = 'http://napstar.ir';

  // For local development, use:
  // static const String baseUrl = 'http://10.0.2.2:5000'; // Android emulator
  // static const String baseUrl = 'http://localhost:5000'; // iOS simulator
}
```

### Network Configuration

**Android (for local development):**

If testing with local backend on Android emulator, use `10.0.2.2` instead of `localhost`:

```dart
static const String baseUrl = 'http://10.0.2.2:5000';
```

**iOS:**

iOS simulator can use `localhost` directly:

```dart
static const String baseUrl = 'http://localhost:5000';
```

## Step 6: Run the App

### Using Command Line

```bash
# List available devices
flutter devices

# Run on specific device
flutter run -d <device-id>

# Run in debug mode (default)
flutter run

# Run in release mode (better performance)
flutter run --release
```

### Using VS Code

1. Open the project in VS Code
2. Open `lib/main.dart`
3. Press `F5` or click "Run" → "Start Debugging"
4. Select your target device from the device selector

### Using Android Studio

1. Open the project in Android Studio
2. Wait for Gradle sync to complete
3. Select your target device from the device dropdown
4. Click the "Run" button (green play icon)

## Step 7: Verify App Functionality

### Initial Screen Flow

1. **Splash Screen**
   - Shows Booksy logo
   - Checks authentication status
   - Auto-redirects to Login or Home

2. **Login Screen**
   - Enter Iranian phone number (09xxxxxxxxx)
   - Click "ارسال کد تایید" (Send Verification Code)
   - Backend sends 6-digit OTP

3. **OTP Verification Screen**
   - Enter 6-digit code
   - Click "تایید و ادامه" (Verify and Continue)
   - On success, redirects to Home

4. **Home Screen**
   - Shows welcome message
   - Displays user phone number
   - Bottom navigation bar

## Troubleshooting

### Common Issues

#### 1. Code Generation Errors

**Problem:** Build runner fails with conflicts

**Solution:**
```bash
# Delete existing generated files and regenerate
flutter pub run build_runner build --delete-conflicting-outputs
```

#### 2. "No devices found"

**Problem:** Flutter can't detect devices

**Solutions:**

**Android:**
```bash
# Check if device is connected
adb devices

# Start emulator from Android Studio
# Or from command line:
flutter emulators --launch <emulator-id>
```

**iOS (macOS):**
```bash
# Open iOS Simulator
open -a Simulator

# Or from Xcode:
# Xcode → Window → Devices and Simulators
```

#### 3. Gradle Sync Issues (Android)

**Problem:** "Could not resolve all dependencies"

**Solution:**
```bash
cd android
./gradlew clean
cd ..
flutter clean
flutter pub get
```

#### 4. CocoaPods Issues (iOS)

**Problem:** Pod install fails

**Solution:**
```bash
cd ios
pod deintegrate
pod install
cd ..
flutter clean
flutter pub get
```

#### 5. API Connection Errors

**Problem:** "Network error" or "Connection refused"

**Solutions:**

- Ensure backend is running: `docker-compose ps`
- Check API base URL in `api_constants.dart`
- For Android emulator, use `10.0.2.2` instead of `localhost`
- Check device internet connection
- Verify firewall settings

#### 6. Missing Generated Files

**Problem:** Import errors for `.g.dart` files

**Solution:**
```bash
# Run code generation
flutter pub run build_runner build --delete-conflicting-outputs
```

## Development Workflow

### Making Changes

1. **Edit code** in your IDE
2. **Hot reload**: Press `r` in terminal or save file (if using IDE)
3. **Hot restart**: Press `R` in terminal (for bigger changes)
4. **Full restart**: Stop and run again (for native code changes)

### After Adding New Models

If you add new models with JSON serialization:

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### After Changing Dependencies

After editing `pubspec.yaml`:

```bash
flutter pub get
```

## Testing

### Run All Tests

```bash
flutter test
```

### Run Specific Test

```bash
flutter test test/features/auth/auth_bloc_test.dart
```

### Generate Coverage Report

```bash
flutter test --coverage
genhtml coverage/lcov.info -o coverage/html
open coverage/html/index.html
```

## Building for Production

### Android APK

```bash
# Build release APK
flutter build apk --release

# Output: build/app/outputs/flutter-apk/app-release.apk
```

### Android App Bundle (for Google Play)

```bash
# Build app bundle
flutter build appbundle --release

# Output: build/app/outputs/bundle/release/app-release.aab
```

### iOS

```bash
# Build iOS app
flutter build ios --release

# Then open Xcode to archive and distribute
open ios/Runner.xcworkspace
```

## Project Structure Overview

```
booksy-customer-app/
├── lib/
│   ├── core/                  # Core functionality
│   │   ├── api/              # API configuration
│   │   ├── di/               # Dependency injection
│   │   ├── errors/           # Error handling
│   │   └── storage/          # Secure storage
│   │
│   ├── features/             # Feature modules
│   │   ├── auth/            # Authentication
│   │   ├── booking/         # Booking management
│   │   ├── home/            # Home screen
│   │   ├── profile/         # User profile
│   │   └── search/          # Provider search
│   │
│   ├── config/              # App configuration
│   └── main.dart            # App entry point
│
├── test/                     # Unit & widget tests
├── android/                  # Android native code
├── ios/                      # iOS native code
├── assets/                   # Images, fonts, etc.
└── pubspec.yaml             # Dependencies
```

## Environment Setup

### For Different Environments (Dev/Staging/Prod)

Create multiple configurations:

1. **Create environment files:**
   - `.env.development`
   - `.env.staging`
   - `.env.production`

2. **Use dart-define:**

```bash
# Development
flutter run --dart-define=ENV=development

# Production
flutter run --dart-define=ENV=production --release
```

3. **Access in code:**

```dart
const String env = String.fromEnvironment('ENV', defaultValue: 'development');
```

## Next Steps

1. **Read the Architecture Documentation**: See `README.md`
2. **Explore the Code**: Start with `lib/main.dart`
3. **Test Authentication**: Try the OTP login flow
4. **Review API Integration**: Check `lib/core/api/`
5. **Understand State Management**: Review BLoC implementation

## Getting Help

- **Documentation**: See `README.md`
- **API Docs**: Swagger at `http://napstar.ir:5001/swagger`
- **Flutter Docs**: https://flutter.dev/docs
- **BLoC Docs**: https://bloclibrary.dev/

## Useful Commands

```bash
# Check Flutter version
flutter --version

# Doctor (check setup)
flutter doctor -v

# List devices
flutter devices

# Clean project
flutter clean

# Update dependencies
flutter pub upgrade

# Analyze code
flutter analyze

# Format code
dart format lib/

# Run code generation
flutter pub run build_runner build --delete-conflicting-outputs

# Watch mode for code generation
flutter pub run build_runner watch
```

## IDE Setup

### VS Code Extensions

Install these recommended extensions:

1. Flutter
2. Dart
3. Bloc
4. Better Comments
5. Error Lens
6. GitLens

### VS Code Settings

Create `.vscode/settings.json`:

```json
{
  "dart.lineLength": 100,
  "editor.formatOnSave": true,
  "editor.rulers": [100],
  "dart.debugExternalPackageLibraries": false,
  "dart.debugSdkLibraries": false
}
```

## Ready to Start!

You're all set! The app should now be running on your device/emulator.

Try logging in with a valid Iranian phone number to test the authentication flow.
