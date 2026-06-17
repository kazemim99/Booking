# Start Android Emulator - Simple Guide

## The Problem

When clicking Play in Android Studio, you're getting a Visual Studio error. This is strange - starting an emulator shouldn't need Visual Studio!

---

## ✅ Solution: Start Emulator Directly

### Method 1: Using Command Line (Recommended)

Open a terminal and run:

```bash
cd C:\Users\Mostafa\AppData\Local\Android\Sdk\emulator
emulator -avd Pixel_7
```

**Or find your SDK path**:
```bash
where emulator
```

Then use that path with:
```bash
emulator -avd Pixel_7
```

This will open the emulator window directly!

---

### Method 2: Start Without Opening Project

1. **Close any Flutter/project in Android Studio**

2. **From Android Studio Welcome Screen**:
   - Click "More Actions" → "Virtual Device Manager"
   - Click ▶ Play button next to Pixel 7

3. **Emulator should start** without the Visual Studio error

---

### Method 3: Use adb (Android Debug Bridge)

```bash
# List available emulators
emulator -list-avds

# Start Pixel_7
emulator -avd Pixel_7
```

---

## ✅ Verify Emulator Started

After starting the emulator, wait 1-2 minutes, then check:

```bash
flutter devices
```

You should see:
```
sdk gphone64 x86 64 (mobile) • emulator-5554 • android-x64 • Android XX (emulator)
```

---

## 🚀 Then Run Your App

Once the emulator appears in `flutter devices`:

```bash
cd c:\Repos\Booking\booksy-customer-app
flutter run -d emulator-5554
```

---

## 🐛 Why the Visual Studio Error?

The error appears because:
- Android Studio might be trying to run Flutter on Windows (not the emulator)
- You have a Flutter project open, and it defaults to Windows
- The emulator launch is being interpreted as a Flutter run command

**Solution**: Start the emulator separately (Method 1 or 2 above), THEN run `flutter run`

---

## 📝 Step-by-Step (Complete)

### Step 1: Start Emulator

**Option A**: From terminal:
```bash
cd C:\Users\Mostafa\AppData\Local\Android\Sdk\emulator
emulator -avd Pixel_7
```

**Option B**: From Android Studio (NO project open):
- Welcome Screen → Virtual Device Manager → Click ▶ Play

### Step 2: Wait for Boot (1-2 minutes)

Watch the emulator window:
- Android logo appears
- Boot animation plays
- Home screen loads

### Step 3: Verify Emulator

```bash
flutter devices
```

Should show `emulator-5554`

### Step 4: Run App

```bash
cd c:\Repos\Booking\booksy-customer-app
flutter run -d emulator-5554
```

---

## ⚡ Quick Command

Open two terminals:

**Terminal 1** (Start emulator):
```bash
cd C:\Users\Mostafa\AppData\Local\Android\Sdk\emulator
emulator -avd Pixel_7
```

**Terminal 2** (Wait for boot, then run app):
```bash
# Wait 1-2 minutes, then:
cd c:\Repos\Booking\booksy-customer-app
flutter devices
flutter run -d emulator-5554
```

---

## 🎯 Summary

**Problem**: Android Studio Play button gives Visual Studio error

**Solution**: Start emulator from command line instead

**Commands**:
```bash
# Terminal 1: Start emulator
cd C:\Users\Mostafa\AppData\Local\Android\Sdk\emulator
emulator -avd Pixel_7

# Terminal 2: Run app (after emulator boots)
flutter run -d emulator-5554
```

**Time**: 5 minutes total

---

**Try this now**: Open terminal and run `cd C:\Users\Mostafa\AppData\Local\Android\Sdk\emulator && emulator -avd Pixel_7`
