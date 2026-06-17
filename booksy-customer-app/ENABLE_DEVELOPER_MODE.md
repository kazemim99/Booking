# Enable Windows Developer Mode

## Error Message

```
Error: Building with plugins requires symlink support.

Please enable Developer Mode in your system settings.
```

## Why This is Needed

Flutter on Windows uses **symbolic links** (symlinks) for managing plugin dependencies. Windows requires Developer Mode to be enabled to create symlinks.

---

## Quick Fix (2 Minutes)

### Step 1: Open Developer Settings

**Option A: Using the command**
```bash
start ms-settings:developers
```

**Option B: Manual navigation**
1. Press `Win + I` (opens Settings)
2. Go to **Privacy & Security** → **For developers**

### Step 2: Enable Developer Mode

1. Find **Developer Mode** toggle
2. **Turn it ON**
3. A confirmation dialog will appear
4. Click **Yes** to confirm

### Step 3: Restart Terminal

1. Close your current terminal/PowerShell
2. Open a new one
3. Navigate back to the project:
   ```bash
   cd c:\Repos\Booking\booksy-customer-app
   ```

### Step 4: Run the App Again

```bash
flutter run -d windows
```

**Or use the script**:
```bash
quick-run.bat
```

---

## Visual Guide

### Windows 11

```
Settings → Privacy & Security → For developers → Developer Mode (ON)
```

### Windows 10

```
Settings → Update & Security → For developers → Developer Mode (ON)
```

---

## What Developer Mode Does

**Enables**:
- ✅ Symbolic link creation (needed for Flutter)
- ✅ App sideloading
- ✅ Device Portal
- ✅ PowerShell script execution

**Does NOT**:
- ❌ Disable security features
- ❌ Make your PC vulnerable
- ❌ Require administrator rights for apps

**It's safe to enable!** It's required for many development tools.

---

## Alternative: Run Without Symlinks (Not Recommended)

If you can't enable Developer Mode, you can try:

```bash
flutter run -d windows --no-pub
```

But this may cause plugin issues.

---

## After Enabling Developer Mode

Once enabled, run:

```bash
cd c:\Repos\Booking\booksy-customer-app
flutter run -d windows
```

The app should build and launch successfully! 🎉

---

## Troubleshooting

### "Developer Mode option is greyed out"

**Solution**: Your Windows edition may not support it.
- Try: `Windows Settings → Apps → Optional Features → Add Feature → Windows Developer Mode`
- Or: Use an Android phone/emulator instead of Windows desktop

### "Access Denied" error

**Solution**: Run PowerShell/Command Prompt as Administrator, then try again.

### Still not working?

Try restarting your computer after enabling Developer Mode.

---

## Summary

1. ✅ Run `start ms-settings:developers`
2. ✅ Enable Developer Mode toggle
3. ✅ Click "Yes" to confirm
4. ✅ Restart terminal
5. ✅ Run `flutter run -d windows`

**Estimated time**: 2 minutes

---

**Next**: After enabling Developer Mode, the app should build and launch! 🚀
