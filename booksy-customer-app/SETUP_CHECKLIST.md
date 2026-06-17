# Flutter Setup Checklist - Android Studio

## Current Status

✅ Android Studio installed
❌ Flutter SDK not installed yet

---

## Quick Setup Checklist

Follow these steps in order:

### 1. Download & Install Flutter SDK

- [ ] Download Flutter: https://docs.flutter.dev/get-started/install/windows
- [ ] Extract to `C:\flutter` (exact location)
- [ ] Add `C:\flutter\bin` to PATH
- [ ] Open NEW terminal and verify: `flutter --version`

**Time**: 5-10 minutes

---

### 2. Install Flutter Plugins in Android Studio

- [ ] Open Android Studio
- [ ] File → Settings → Plugins
- [ ] Search "Flutter" and click Install
- [ ] Restart Android Studio
- [ ] Verify "New Flutter Project" appears

**Time**: 3 minutes

---

### 3. Configure Android SDK

- [ ] Open Android Studio → Settings → Android SDK
- [ ] Note SDK location
- [ ] Go to SDK Tools tab
- [ ] Install:
  - [ ] Android SDK Command-line Tools
  - [ ] Android SDK Build-Tools
  - [ ] Android SDK Platform-Tools
  - [ ] Android Emulator
- [ ] Click Apply and wait for download

**Time**: 5-10 minutes (download time)

---

### 4. Accept Android Licenses

- [ ] Open terminal
- [ ] Run: `flutter doctor --android-licenses`
- [ ] Type `y` for all prompts

**Time**: 1 minute

---

### 5. Create Android Emulator

- [ ] Android Studio → Device Manager
- [ ] Click "Create Device"
- [ ] Select Pixel 7
- [ ] Select API 33 (download if needed)
- [ ] Finish and create
- [ ] Click ▶ to start emulator

**Time**: 5-15 minutes (first download)

---

### 6. Verify Setup

- [ ] Run: `flutter doctor -v`
- [ ] All items should be ✓ (green)

**Expected output**:
```
[✓] Flutter
[✓] Android toolchain
[✓] Android Studio
[✓] Connected device
```

---

### 7. Install Project Dependencies

- [ ] Open terminal
- [ ] `cd c:\Repos\Booking\booksy-customer-app`
- [ ] Run: `flutter pub get`
- [ ] Run: `flutter pub run build_runner build --delete-conflicting-outputs`

**Time**: 2-3 minutes

---

### 8. Run the App!

- [ ] Make sure emulator is running
- [ ] Run: `flutter run`
- [ ] Wait for build (2-3 minutes first time)
- [ ] App should launch on emulator!

---

## Total Time Estimate

- **Downloads**: 15-20 minutes
- **Configuration**: 10-15 minutes
- **First build**: 2-3 minutes

**Total**: 30-40 minutes

---

## What If Something Goes Wrong?

Check [ANDROID_STUDIO_SETUP.md](ANDROID_STUDIO_SETUP.md) for:
- Detailed step-by-step guide
- Troubleshooting section
- Alternative options

---

## Quick Commands

```bash
# After Flutter is installed:

# Check setup
flutter doctor

# Install packages
flutter pub get

# Generate code
flutter pub run build_runner build --delete-conflicting-outputs

# Run app
flutter run
```

---

## Current Step

👉 **START HERE**: Step 1 - Download Flutter SDK

Download from: https://docs.flutter.dev/get-started/install/windows

Or read full guide: [ANDROID_STUDIO_SETUP.md](ANDROID_STUDIO_SETUP.md)

---

Good luck! 🚀
