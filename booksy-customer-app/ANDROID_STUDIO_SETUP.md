# Flutter Setup with Android Studio - Fresh Install

## What You Have

✅ Android Studio installed (fresh)

## What You Need

1. ⏳ Flutter SDK
2. ⏳ Flutter & Dart plugins in Android Studio
3. ⏳ Android SDK (comes with Android Studio)
4. ⏳ Android Emulator

---

## Step-by-Step Setup

### Step 1: Download Flutter SDK (5 minutes)

**Download latest Flutter**:
```
https://docs.flutter.dev/get-started/install/windows
```

Or direct link:
```
https://storage.googleapis.com/flutter_infra_release/releases/stable/windows/flutter_windows_3.27.1-stable.zip
```

**Extract to**:
```
C:\flutter
```

**IMPORTANT**: Extract to `C:\flutter` (not in Program Files, not in a folder with spaces)

---

### Step 2: Add Flutter to PATH

**PowerShell (as Administrator)**:
```powershell
[System.Environment]::SetEnvironmentVariable("Path", [System.Environment]::GetEnvironmentVariable("Path", "User") + ";C:\flutter\bin", "User")
```

**Or via GUI**:
1. Press `Win + X` → System
2. Advanced system settings
3. Environment Variables
4. Edit "Path" in User variables
5. Add: `C:\flutter\bin`
6. Click OK

**Verify** (in NEW terminal):
```bash
flutter --version
```

---

### Step 3: Run Flutter Doctor

Open a new terminal and run:

```bash
flutter doctor
```

**Expected output**:
```
Doctor summary:
[✓] Flutter (Channel stable, 3.27.1)
[✗] Android toolchain - develop for Android devices
    ✗ cmdline-tools component is missing
[!] Android Studio (version 2024.2)
    ✗ Flutter plugin not installed
    ✗ Dart plugin not installed
[✓] VS Code (optional)
[✓] Connected device
```

Don't worry about the ✗ marks - we'll fix them!

---

### Step 4: Install Flutter & Dart Plugins in Android Studio

1. **Open Android Studio**

2. **Go to Plugins**:
   - File → Settings (or Ctrl+Alt+S)
   - Plugins (left sidebar)

3. **Search and Install**:
   - Type "Flutter" in search
   - Click "Install" on Flutter plugin
   - It will auto-install Dart plugin too
   - Click "OK"

4. **Restart Android Studio**

5. **Verify**:
   - After restart, you should see "New Flutter Project" option

---

### Step 5: Configure Android SDK

Android Studio should have already downloaded Android SDK during installation.

**Verify Android SDK location**:

1. Open Android Studio
2. File → Settings → Appearance & Behavior → System Settings → Android SDK
3. Note the SDK path (usually: `C:\Users\YourName\AppData\Local\Android\Sdk`)

**Tell Flutter about it** (if needed):

```bash
flutter config --android-sdk "C:\Users\Mostafa\AppData\Local\Android\Sdk"
```

**Accept Android licenses**:

```bash
flutter doctor --android-licenses
```

Type `y` for all prompts.

---

### Step 6: Install Android Command Line Tools

If `flutter doctor` shows "cmdline-tools component is missing":

1. Open Android Studio
2. File → Settings → Appearance & Behavior → System Settings → Android SDK
3. Go to "SDK Tools" tab
4. Check these:
   - ✅ Android SDK Command-line Tools (latest)
   - ✅ Android SDK Build-Tools
   - ✅ Android SDK Platform-Tools
   - ✅ Android Emulator
5. Click "Apply" → "OK"
6. Wait for download to complete

---

### Step 7: Create Android Emulator

1. **Open Android Studio**

2. **Open Device Manager**:
   - Click "Device Manager" icon (phone icon on right side)
   - Or: Tools → Device Manager

3. **Create Virtual Device**:
   - Click "Create Device"
   - Select **Pixel 7** (or any phone) → Next
   - Select **System Image**: Choose "Tiramisu" (API 33) or latest
   - Click "Download" if needed
   - Click "Next"
   - Name it (e.g., "Pixel_7_API_33")
   - Click "Finish"

4. **Start Emulator**:
   - Click green ▶ play button next to your emulator
   - Wait for emulator to boot (1-2 minutes first time)

---

### Step 8: Verify Setup

Run Flutter Doctor again:

```bash
flutter doctor -v
```

**Expected (all green)**:
```
[✓] Flutter (Channel stable, 3.27.1)
[✓] Android toolchain - develop for Android devices (Android SDK 33.0.0)
[✓] Android Studio (version 2024.2)
    • Flutter plugin version 81.1.2
    • Dart plugin version 241.19072
[✓] Connected device (2 available)
[✓] Network resources
```

---

### Step 9: Install Project Dependencies

```bash
cd c:\Repos\Booking\booksy-customer-app

# Install packages
flutter pub get

# Generate code
flutter pub run build_runner build --delete-conflicting-outputs
```

---

### Step 10: Run the App!

**Make sure emulator is running**, then:

```bash
flutter run
```

Or in Android Studio:
1. Open project folder: `c:\Repos\Booking\booksy-customer-app`
2. Wait for indexing to complete
3. Select your emulator from device dropdown (top toolbar)
4. Click green ▶ Run button
5. Or press `Shift+F10`

---

## Option: Use VS Code Instead

If you prefer VS Code over Android Studio for coding:

1. **Keep Android Studio** (for SDK and emulator)
2. **Install VS Code**
3. **Install Flutter extension** in VS Code
4. **Code in VS Code**, use Android Studio only for emulator

See [VSCODE_SETUP.md](VSCODE_SETUP.md) for details.

---

## Troubleshooting

### "Flutter not found"
→ Add `C:\flutter\bin` to PATH, restart terminal

### "Android SDK not found"
→ Run: `flutter config --android-sdk "C:\Users\Mostafa\AppData\Local\Android\Sdk"`

### "No devices found"
→ Start emulator first: Android Studio → Device Manager → Click ▶

### "cmdline-tools missing"
→ Install via Android Studio SDK Manager (Step 6)

### "License error"
→ Run: `flutter doctor --android-licenses` and accept all

### Build fails
→ Clean project: `flutter clean && flutter pub get`

---

## Quick Commands Reference

```bash
# Check setup
flutter doctor -v

# List devices
flutter devices

# Run app
flutter run

# Hot reload (while running)
Press 'r' in terminal

# Hot restart
Press 'R' in terminal

# Quit
Press 'q' in terminal

# Clean project
flutter clean

# Get packages
flutter pub get

# Generate code
flutter pub run build_runner build --delete-conflicting-outputs
```

---

## Summary

**Setup Order**:
1. ✅ Download Flutter SDK
2. ✅ Extract to `C:\flutter`
3. ✅ Add to PATH
4. ✅ Install Flutter plugin in Android Studio
5. ✅ Accept Android licenses
6. ✅ Install command-line tools
7. ✅ Create emulator
8. ✅ Run `flutter pub get`
9. ✅ Run `flutter pub run build_runner build --delete-conflicting-outputs`
10. ✅ Run `flutter run`

**Estimated Time**: 30-40 minutes (including downloads)

---

## What You'll See

Once the app runs:

1. **Build Process** (first time: 2-3 minutes)
   - Gradle sync
   - Compile Dart code
   - Build APK
   - Install on emulator

2. **App Launch**
   - Splash screen
   - Login screen (Persian UI)
   - Phone number input
   - OTP verification

3. **Hot Reload**
   - Change code
   - Press `r`
   - See instant changes!

---

## Next Steps

After successful setup:
1. ✅ Test authentication flow
2. ✅ Explore the codebase
3. ✅ Read [README.md](README.md) for architecture
4. ✅ Start developing features!

---

**Ready?** Start with Step 1: Download Flutter SDK! 🚀
