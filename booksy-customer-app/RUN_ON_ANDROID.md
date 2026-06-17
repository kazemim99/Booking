# Run Flutter App on Android Emulator

## ✅ You Created the Emulator!

Great job! Your Pixel_7 emulator is created. Now let's start it and run the app.

---

## 🚀 Quick Steps

### Step 1: Start the Emulator

The emulator is starting now. It takes **1-2 minutes** to boot the first time.

**Option A: Check if it's started** (run this command every 10-15 seconds):
```bash
flutter devices
```

**Wait for this to appear**:
```
sdk gphone64 x86 64 (mobile) • emulator-5554 • android-x64 • Android XX (API XX) (emulator)
```

**Option B: Start manually from Android Studio**:
1. Open Android Studio
2. Device Manager → Click green ▶ Play button next to Pixel 7
3. Wait for emulator window to appear and fully load

---

### Step 2: Verify Emulator is Ready

**Run**:
```bash
flutter devices
```

**Should show 4 devices now** (including emulator):
```
Found 4 connected devices:
  sdk gphone64 x86 64 (mobile) • emulator-5554 • android-x64    • Android XX (API XX) (emulator)
  Windows (desktop)             • windows       • windows-x64    • Microsoft Windows
  Chrome (web)                  • chrome        • web-javascript • Google Chrome
  Edge (web)                    • edge          • web-javascript • Microsoft Edge
```

---

### Step 3: Run the App on Android Emulator

Once emulator appears in `flutter devices`:

```bash
cd c:\Repos\Booking\booksy-customer-app
flutter run -d emulator-5554
```

**Or let Flutter auto-select** (it will ask you to choose):
```bash
flutter run
```

Then select the emulator from the list.

---

## 🎯 What Will Happen

### First Build (2-3 minutes)
```
✓ Gradle sync
✓ Downloading dependencies
✓ Compiling Dart code
✓ Building APK
✓ Installing on emulator
✓ Launching app
```

### App Launch
You'll see the emulator window with:

1. **Splash Screen** - Booksy logo (2-3 seconds)
2. **Login Screen** - Phone number input
   - Persian (RTL) layout
   - Beautiful UI
3. **Ready to test!**

---

## 🔥 Hot Reload

While the app is running:

- **Press `r`** in terminal → Hot reload (instant changes)
- **Press `R`** → Hot restart (full restart)
- **Press `q`** → Quit app
- **Save file** in VS Code/Android Studio → Auto hot reload

---

## 🐛 Troubleshooting

### Emulator not appearing in flutter devices

**Solution 1: Wait longer**
- First boot takes 1-2 minutes
- Run `flutter devices` again after 30 seconds

**Solution 2: Start from Android Studio**
1. Open Android Studio
2. Device Manager
3. Click ▶ Play button on Pixel 7
4. Wait for emulator to fully boot
5. Run `flutter devices` again

**Solution 3: Restart emulator**
```bash
# Close emulator window if open
# Then start again
flutter emulators --launch Pixel_7
```

---

### "Visual Studio toolchain" error

This error appears because Flutter tries to run on **Windows** by default.

**Solution**: Specify the Android emulator explicitly:
```bash
flutter run -d emulator-5554
```

Or start the emulator first, then run:
```bash
flutter run
```

Flutter will detect the emulator and ask you to choose it.

---

### Emulator is slow

**Solution 1: Increase RAM**
1. Android Studio → Device Manager
2. Edit (pencil icon) on your emulator
3. Show Advanced Settings
4. Increase RAM to 2048MB or 4096MB

**Solution 2: Enable hardware acceleration**
- Should be enabled by default (Hyper-V)
- Check Windows Features → Hyper-V is checked

**Solution 3: Use a physical phone**
- Connect Android phone via USB
- Enable USB Debugging
- Run `flutter run`

---

### Build fails

```bash
flutter clean
flutter pub get
flutter run -d emulator-5554
```

---

## ✅ Success Checklist

- [ ] Emulator created (Pixel_7) ✅
- [ ] Emulator started (running now)
- [ ] Emulator appears in `flutter devices`
- [ ] Run `flutter run -d emulator-5554`
- [ ] App builds successfully (2-3 min)
- [ ] App launches on emulator
- [ ] See splash screen → login screen

---

## 📱 Current Status

**You are here**:
```
✅ Emulator created
⏳ Emulator starting (wait 1-2 minutes)
⏳ Run flutter run -d emulator-5554
```

**Next**:
1. Wait for emulator to boot
2. Run `flutter devices` to verify
3. Run `flutter run -d emulator-5554`
4. See your app! 🎉

---

## 🎯 Quick Commands

```bash
# Check if emulator is ready
flutter devices

# Start emulator manually
flutter emulators --launch Pixel_7

# Run app on emulator (after it's started)
flutter run -d emulator-5554

# Run and let Flutter ask which device
flutter run
```

---

## ⏱️ Timeline

- **Now**: Emulator starting (1-2 min wait)
- **After boot**: Run `flutter run -d emulator-5554`
- **Build time**: 2-3 minutes (first time only)
- **Done**: App launches! 🚀

**Total time**: ~5 minutes from now

---

**Next Step**: Wait for emulator to boot, then run `flutter run -d emulator-5554`

Good luck! You're almost there! 🎊
