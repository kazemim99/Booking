# Create Android Emulator

## Current Status

✅ Flutter setup complete
✅ Android SDK installed
❌ No Android emulator created yet

---

## Quick Create Emulator

You have 2 options:

### Option 1: Using Android Studio GUI (Recommended - Easier)

1. **Open Android Studio**

2. **Open Device Manager**:
   - Click **Device Manager** icon on the right toolbar
   - Or: Tools → Device Manager

3. **Create Virtual Device**:
   - Click **Create Device** button
   - Select **Phone** category
   - Choose **Pixel 7** (recommended) or any phone
   - Click **Next**

4. **Select System Image**:
   - Choose **Tiramisu** (API 33) or **UpsideDownCake** (API 34)
   - Click **Download** if not already downloaded
   - Wait for download (500MB-1GB, 5-10 minutes)
   - Click **Next**

5. **Configure Emulator**:
   - Name: `Pixel_7_API_34` (or leave default)
   - Leave other settings as default
   - Click **Finish**

6. **Start Emulator**:
   - Click the green ▶ **Play** button next to your emulator
   - Wait for emulator to boot (1-2 minutes first time)

---

### Option 2: Using Command Line (Faster)

Create an emulator from terminal:

```bash
# Create a Pixel 7 emulator with default settings
flutter emulators --create --name pixel7

# Or specify more details
avdmanager create avd -n Pixel_7 -k "system-images;android-34;google_apis;x86_64" -d pixel_7
```

**Note**: You may need to download the system image first using Android Studio SDK Manager.

---

## After Creating Emulator

### Start the Emulator

**Option A: From Android Studio**
- Device Manager → Click ▶ Play button

**Option B: From Command Line**
```bash
flutter emulators --launch pixel7
```

**Option C: List and Launch**
```bash
# List available emulators
flutter emulators

# Launch specific emulator
flutter emulators --launch <emulator-id>
```

---

## Verify Emulator is Running

```bash
flutter devices
```

You should now see:
```
Found 4 connected devices:
  sdk gphone64 x86 64 (mobile) • emulator-5554 • android-x64 • Android 14 (API 34) (emulator)
  Windows (desktop)             • windows       • windows-x64 • Microsoft Windows
  Chrome (web)                  • chrome        • web-javascript • Google Chrome
  Edge (web)                    • edge          • web-javascript • Microsoft Edge
```

---

## Run the App on Emulator

Once emulator is running:

```bash
cd c:\Repos\Booking\booksy-customer-app

# Install dependencies (if not done yet)
flutter pub get

# Generate code (if not done yet)
flutter pub run build_runner build --delete-conflicting-outputs

# Run on emulator
flutter run

# Or specify device
flutter run -d emulator-5554
```

---

## Troubleshooting

### "No space left on device"
→ Emulator needs ~2GB free space. Free up disk space and try again.

### "Intel HAXM is required"
→ Your CPU may need virtualization enabled in BIOS
→ Or use ARM-based system image instead of x86_64

### "Emulator won't start"
→ Check if Hyper-V is enabled: Windows Features → Hyper-V
→ Or disable Hyper-V and use Intel HAXM

### "Download fails"
→ Check internet connection
→ Try again later
→ Or download manually via SDK Manager

---

## Emulator Performance Tips

### Make Emulator Faster

1. **Enable Hardware Acceleration**:
   - Already enabled if Hyper-V is on
   - Or install Intel HAXM

2. **Increase RAM**:
   - Edit emulator settings
   - Increase RAM to 2048MB or 4096MB

3. **Use x86_64 Images**:
   - Faster than ARM images on x86 PCs

4. **Close Other Apps**:
   - Emulator uses significant RAM
   - Close browsers and other heavy apps

---

## Alternative: Use Physical Android Phone

If emulator is slow or has issues:

1. **Enable Developer Options** on your Android phone
2. **Enable USB Debugging**
3. **Connect phone via USB**
4. **Run**: `flutter devices` (phone should appear)
5. **Run**: `flutter run`

No emulator needed!

---

## Summary

**Recommended**: Use Android Studio GUI to create emulator

**Steps**:
1. Open Android Studio
2. Device Manager → Create Device
3. Select Pixel 7 → Download API 34
4. Create and start emulator
5. Run `flutter run`

**Time**: 10-15 minutes (first time, including download)

---

**Next**: Create an emulator using Android Studio Device Manager!
