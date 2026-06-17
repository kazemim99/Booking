# Run Flutter App on Windows Desktop (No Emulator Needed!)

## Good News!

You can run and test the Flutter app directly on **Windows desktop** without any Android emulator or SDK!

This is perfect for:
- ✅ Quick testing
- ✅ No Android SDK needed
- ✅ No emulator needed
- ✅ Fastest development
- ✅ See UI and test features

---

## Requirements

You already have:
- ✅ Flutter SDK at `C:\flutter`

You need:
- ❓ Flutter in PATH
- ❓ Windows Desktop support enabled

---

## Quick Start

### Step 1: Verify Flutter Installation

Open a **new** terminal and run:

```bash
flutter --version
```

**Expected**: Should show Flutter version
**If error**: Flutter not in PATH (see below to fix)

### Step 2: Enable Windows Desktop

```bash
flutter config --enable-windows-desktop
```

### Step 3: Check Devices

```bash
flutter devices
```

**Expected output**:
```
2 connected devices:

Windows (desktop) • windows • windows-x64    • Microsoft Windows [Version 10.0.19045.3803]
Chrome (web)      • chrome  • web-javascript • Google Chrome 120.0.6099.109
```

### Step 4: Install Dependencies

```bash
cd c:\Repos\Booking\booksy-customer-app
flutter pub get
```

### Step 5: Generate Code

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### Step 6: Run on Windows

```bash
flutter run -d windows
```

The app will launch as a **Windows desktop application**!

---

## If Flutter Command Not Found

### Fix: Add Flutter to PATH

**Option A: PowerShell (Admin)**

```powershell
[System.Environment]::SetEnvironmentVariable(
    "Path",
    [System.Environment]::GetEnvironmentVariable("Path", "User") + ";C:\flutter\bin",
    "User"
)
```

**Option B: GUI**

1. Search "Environment Variables" in Windows
2. Edit "Path" in User variables
3. Add: `C:\flutter\bin`
4. Click OK
5. **Restart terminal**

### Verify

```bash
flutter --version
```

---

## Running the App

### Using Terminal

```bash
# From project directory
cd c:\Repos\Booking\booksy-customer-app

# Run on Windows
flutter run -d windows

# Or just (auto-selects Windows)
flutter run
```

### Using VS Code

1. Open project in VS Code
2. Press `Ctrl+Shift+P`
3. Type "Flutter: Select Device"
4. Choose "Windows (windows)"
5. Press `F5` to run

---

## What You'll See

The app will launch as a Windows desktop app:

1. **Splash Screen** - Shows briefly
2. **Login Screen** - Persian UI for phone number
3. **OTP Screen** - Enter verification code
4. **Home Screen** - Welcome message

---

## Benefits of Windows Desktop

✅ **No Android SDK needed**
✅ **No emulator needed**
✅ **Faster build times**
✅ **Easier debugging**
✅ **Hot reload works**
✅ **Perfect for UI testing**

---

## Limitations

⚠️ **Camera**: Uses webcam (not phone camera)
⚠️ **Location**: Uses PC location (not GPS)
⚠️ **Some plugins**: May not work (phone-specific features)

But for testing **authentication, UI, and API calls** - it's perfect!

---

## Next Steps

After testing on Windows:

### Option 1: Continue with Windows (Recommended for now)
- Keep developing and testing on Windows
- Fast iteration
- No additional setup needed

### Option 2: Add Android Emulator (Later)
- Install Android Studio (for Android SDK)
- Create emulator
- Test Android-specific features

### Option 3: Use Physical Phone
- Enable USB debugging on Android phone
- Connect via USB
- Run: `flutter run` (will detect phone)

---

## Troubleshooting

### "Windows desktop not available"

**Solution**: Enable it
```bash
flutter config --enable-windows-desktop
flutter doctor
```

### "Build failed - Visual Studio not found"

**Solution**: You need Visual Studio for Windows builds

**Download**: https://visualstudio.microsoft.com/downloads/

**Install**:
- Choose "Desktop development with C++"
- Or install Build Tools only

**Then run**:
```bash
flutter doctor
flutter run -d windows
```

### "No devices found"

**Solution**:
```bash
# Enable Windows desktop
flutter config --enable-windows-desktop

# Check devices
flutter devices
```

---

## Quick Commands

```bash
# Check Flutter setup
flutter doctor

# List devices
flutter devices

# Run on Windows
flutter run -d windows

# Hot reload (while running)
# Press 'r' in terminal or Ctrl+S in VS Code

# Hot restart
# Press 'R' in terminal

# Quit
# Press 'q' in terminal
```

---

## Summary

You can **skip Android SDK and emulator** for now and test the app on Windows desktop!

**Steps**:
1. Add Flutter to PATH (if needed)
2. Enable Windows desktop
3. Run `flutter pub get`
4. Run `flutter pub run build_runner build --delete-conflicting-outputs`
5. Run `flutter run -d windows`

**Done!** 🎉

---

## When Do You Need Android SDK?

You only need Android SDK + Emulator if:
- You want to test on Android specifically
- You need Android-specific features (camera, GPS, etc.)
- You're ready to build APK for distribution

For **learning, testing, and development** - Windows desktop is perfect!
