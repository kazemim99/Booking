# Flutter Development with VS Code (Recommended)

## TL;DR

✅ **Yes, you can use VS Code for Flutter development!**

You still need Android Studio installed, but **only for**:
- Android SDK
- Android Emulator
- Build tools

You'll **never need to open** Android Studio for daily development - just use VS Code!

---

## What You Need

### 1. Flutter SDK ✅
The Flutter framework itself.

### 2. VS Code ✅
Your main IDE for coding.

### 3. Android Studio ⚠️
**Only needed for Android SDK & Emulator** (you won't code in it).

---

## Setup Process

### Step 1: Install Flutter SDK

**Download**:
```
https://storage.googleapis.com/flutter_infra_release/releases/stable/windows/flutter_windows_3.27.1-stable.zip
```

**Extract to**: `C:\flutter`

**Add to PATH** (PowerShell as Admin):
```powershell
[System.Environment]::SetEnvironmentVariable("Path", [System.Environment]::GetEnvironmentVariable("Path", "User") + ";C:\flutter\bin", "User")
```

**Verify** (NEW terminal):
```bash
flutter --version
```

---

### Step 2: Install VS Code Extensions

Open VS Code and install these extensions:

#### Required Extensions:
1. **Flutter** (by Dart Code)
   - ID: `Dart-Code.flutter`
   - Includes Dart language support
   - Provides Flutter commands
   - Debug support

2. **Dart** (by Dart Code)
   - ID: `Dart-Code.dart-code`
   - Usually installed automatically with Flutter extension

#### Recommended Extensions:
3. **Bloc** (by Felix Angelov)
   - ID: `FelixAngelov.bloc`
   - BLoC code snippets
   - BLoC architecture helpers

4. **Error Lens** (by Alexander)
   - ID: `usernamehw.errorlens`
   - Inline error messages

5. **Better Comments** (by Aaron Bond)
   - ID: `aaron-bond.better-comments`
   - Color-coded comments

6. **GitLens** (by GitKraken)
   - ID: `eamodio.gitlens`
   - Git integration

**Install via command**:
```bash
code --install-extension Dart-Code.flutter
code --install-extension Dart-Code.dart-code
code --install-extension FelixAngelov.bloc
code --install-extension usernamehw.errorlens
code --install-extension aaron-bond.better-comments
code --install-extension eamodio.gitlens
```

---

### Step 3: Install Android SDK (via Android Studio)

You need Android Studio **ONLY** to install the Android SDK and emulator.

**Why?** Because manually installing Android SDK is complicated. Android Studio does it all automatically.

#### Installation Steps:

1. **Download Android Studio**:
   ```
   https://developer.android.com/studio
   ```

2. **Install with default settings**

3. **First launch**:
   - Android Studio will download Android SDK automatically
   - Wait for it to finish (5-10 minutes)
   - You can close Android Studio after this

4. **Accept Android licenses**:
   ```bash
   flutter doctor --android-licenses
   ```
   Type `y` to accept all.

---

### Step 4: Create Android Emulator

You need to create an emulator **once** using Android Studio:

1. **Open Android Studio**
2. Click **More Actions** → **Virtual Device Manager**
3. Click **Create Device**
4. Select **Pixel 7** → **Next**
5. Select **Android 14 (API 34)** → **Download** (if needed) → **Next**
6. Name it `Pixel_7_API_34` → **Finish**
7. **Close Android Studio** (you're done with it!)

---

### Step 5: Configure VS Code for Flutter

Create VS Code workspace settings:

**File**: `.vscode/settings.json`

```json
{
  "dart.lineLength": 100,
  "editor.formatOnSave": true,
  "editor.rulers": [100],
  "dart.debugExternalPackageLibraries": false,
  "dart.debugSdkLibraries": false,
  "[dart]": {
    "editor.formatOnSave": true,
    "editor.formatOnType": true,
    "editor.rulers": [100],
    "editor.selectionHighlight": false,
    "editor.suggest.snippetsPreventQuickSuggestions": false,
    "editor.suggestSelection": "first",
    "editor.tabCompletion": "onlySnippets",
    "editor.wordBasedSuggestions": false
  },
  "dart.flutterSdkPath": "C:\\flutter",
  "dart.checkForSdkUpdates": true,
  "dart.showInspectorNotificationsForWidgetErrors": true
}
```

Create VS Code launch configuration:

**File**: `.vscode/launch.json`

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Flutter: Debug",
      "request": "launch",
      "type": "dart",
      "program": "lib/main.dart"
    },
    {
      "name": "Flutter: Profile",
      "request": "launch",
      "type": "dart",
      "program": "lib/main.dart",
      "flutterMode": "profile"
    },
    {
      "name": "Flutter: Release",
      "request": "launch",
      "type": "dart",
      "program": "lib/main.dart",
      "flutterMode": "release"
    }
  ]
}
```

---

### Step 6: Install Project Dependencies

```bash
cd c:\Repos\Booking\booksy-customer-app

# Install packages
flutter pub get

# Generate code
flutter pub run build_runner build --delete-conflicting-outputs
```

---

## Using VS Code for Flutter Development

### Starting the Emulator

**Option A: From Terminal** (Recommended)
```bash
# List emulators
flutter emulators

# Start emulator
flutter emulators --launch Pixel_7_API_34
```

**Option B: From VS Code**
1. Press `Ctrl+Shift+P`
2. Type "Flutter: Launch Emulator"
3. Select your emulator

### Running the App

**Option A: Using VS Code Debug** (Recommended)
1. Open `lib/main.dart`
2. Press `F5` (or click Run → Start Debugging)
3. Select your device from the device selector (bottom right)
4. App will build and launch

**Option B: From Terminal**
```bash
flutter run
```

**Option C: Using VS Code Button**
1. Click the device selector in the bottom right
2. Select your emulator
3. Click "Run" → "Start Debugging"

### Hot Reload

**While app is running:**
- **Hot Reload**: Press `r` in terminal OR `Ctrl+F5` in VS Code
- **Hot Restart**: Press `R` in terminal OR `Shift+F5` in VS Code
- **Stop**: Press `q` in terminal OR stop debugging in VS Code

### VS Code Features

#### 1. Flutter Commands
Press `Ctrl+Shift+P` and type "Flutter:":
- Flutter: New Project
- Flutter: Doctor
- Flutter: Upgrade
- Flutter: Clean
- Flutter: Get Packages
- Flutter: Run Flutter Doctor
- Flutter: Launch Emulator
- Flutter: Hot Reload
- Flutter: Hot Restart

#### 2. Debug Console
- See print statements
- See error messages
- Interact with running app

#### 3. Widget Inspector
- Click "Open DevTools" in debug console
- Inspect widget tree
- View widget properties
- Debug layout issues

#### 4. IntelliSense
- Auto-completion for Flutter widgets
- Parameter hints
- Quick fixes
- Refactoring options

#### 5. Code Actions
- Wrap widget with Center, Padding, etc.
- Extract widget
- Extract method
- Remove widget

---

## Daily Development Workflow (VS Code Only)

```bash
# 1. Start emulator (one-time per work session)
flutter emulators --launch Pixel_7_API_34

# 2. Open project in VS Code
code c:\Repos\Booking\booksy-customer-app

# 3. Press F5 to run app

# 4. Make changes and hot reload (Ctrl+S or r)

# 5. If you change models/APIs:
flutter pub run build_runner build --delete-conflicting-outputs
```

**You never need to open Android Studio!**

---

## When Do You Need Android Studio?

### ✅ You NEED Android Studio for:
- Installing Android SDK (one-time)
- Creating emulators (one-time per emulator)
- Updating Android SDK (occasionally)
- Advanced Android-specific debugging (rare)

### ❌ You DON'T NEED Android Studio for:
- Daily coding (use VS Code)
- Running the app (use VS Code)
- Hot reload (use VS Code)
- Debugging (use VS Code)
- Git operations (use VS Code)

---

## VS Code Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Start Debugging | `F5` |
| Start Without Debugging | `Ctrl+F5` |
| Stop Debugging | `Shift+F5` |
| Hot Reload | Save file (`Ctrl+S`) |
| Command Palette | `Ctrl+Shift+P` |
| Quick Open File | `Ctrl+P` |
| Find in Files | `Ctrl+Shift+F` |
| Terminal | `` Ctrl+` `` |
| Format Document | `Shift+Alt+F` |
| Go to Definition | `F12` |
| Find References | `Shift+F12` |
| Rename Symbol | `F2` |

---

## VS Code Tips & Tricks

### 1. Flutter Outline
- View → Open View → Flutter Outline
- See widget tree structure
- Click to navigate to widget
- Reorganize widgets easily

### 2. Code Snippets
Type these and press Tab:
- `stless` → StatelessWidget
- `stful` → StatefulWidget
- `build` → build method
- `initState` → initState method
- `dispose` → dispose method

### 3. Multi-Cursor Editing
- `Alt+Click` → Add cursor
- `Ctrl+Alt+Up/Down` → Add cursor above/below
- `Ctrl+D` → Select next occurrence
- `Ctrl+Shift+L` → Select all occurrences

### 4. Code Folding
- `Ctrl+Shift+[` → Fold region
- `Ctrl+Shift+]` → Unfold region
- Click the arrow in the gutter

### 5. Problem Panel
- View → Problems
- See all errors and warnings
- Click to navigate to issue

---

## Debugging in VS Code

### Breakpoints
1. Click in the gutter (left of line numbers)
2. Red dot appears
3. Run with `F5`
4. App pauses at breakpoint

### Debug Controls
- **Continue** (`F5`) - Resume execution
- **Step Over** (`F10`) - Execute current line
- **Step Into** (`F11`) - Go into function
- **Step Out** (`Shift+F11`) - Exit function
- **Restart** (`Ctrl+Shift+F5`) - Restart app

### Variables Panel
- See current variable values
- Expand objects to inspect
- Modify values during debug

### Watch Expressions
- Add expressions to watch
- See values update in real-time

---

## Running on Physical Device

### Android Phone
1. Enable Developer Options on phone
2. Enable USB Debugging
3. Connect via USB
4. Phone will appear in VS Code device selector
5. Select phone and press `F5`

### Testing on Multiple Devices
VS Code shows all connected devices in the device selector (bottom right).
Click to switch between emulator and physical device.

---

## Useful VS Code Extensions for Flutter

### Must-Have
1. **Flutter** - Core Flutter support
2. **Dart** - Dart language support
3. **Bloc** - BLoC pattern snippets

### Highly Recommended
4. **Error Lens** - Inline errors
5. **GitLens** - Git integration
6. **Better Comments** - Colored comments
7. **Bracket Pair Colorizer** - Rainbow brackets
8. **Material Icon Theme** - Better file icons
9. **Path Intellisense** - Auto-complete paths
10. **TODO Highlight** - Highlight TODO comments

### Nice to Have
11. **Pubspec Assist** - Manage dependencies
12. **Flutter Widget Snippets** - More code snippets
13. **Awesome Flutter Snippets** - Even more snippets
14. **JSON to Dart Model** - Generate models from JSON

Install all recommended:
```bash
code --install-extension Dart-Code.flutter
code --install-extension Dart-Code.dart-code
code --install-extension FelixAngelov.bloc
code --install-extension usernamehw.errorlens
code --install-extension eamodio.gitlens
code --install-extension aaron-bond.better-comments
code --install-extension CoenraadS.bracket-pair-colorizer-2
code --install-extension PKief.material-icon-theme
code --install-extension christian-kohler.path-intellisense
code --install-extension wayou.vscode-todo-highlight
```

---

## Troubleshooting VS Code

### "Flutter SDK not found"
**Solution**: Set Flutter SDK path in VS Code settings:
```json
{
  "dart.flutterSdkPath": "C:\\flutter"
}
```

### "No device found"
**Solution**: Start emulator first:
```bash
flutter emulators --launch Pixel_7_API_34
```

### "Build failed"
**Solution**: Clean and rebuild:
```bash
flutter clean
flutter pub get
```

### Extensions not working
**Solution**: Reload VS Code:
- Press `Ctrl+Shift+P`
- Type "Reload Window"
- Press Enter

---

## Performance Tips

### VS Code Performance
1. Disable unused extensions
2. Increase memory limit:
   ```json
   {
     "dart.analysisServerMaxMemory": 4096
   }
   ```
3. Exclude folders from watching:
   ```json
   {
     "files.watcherExclude": {
       "**/build/**": true,
       "**/.dart_tool/**": true
     }
   }
   ```

### Flutter Performance
1. Use `--profile` mode for performance testing
2. Use `--release` mode for production builds
3. Enable DevTools for performance profiling

---

## Comparison: VS Code vs Android Studio

| Feature | VS Code | Android Studio |
|---------|---------|----------------|
| Lightweight | ✅ Fast | ❌ Heavy |
| Flutter Support | ✅ Excellent | ✅ Excellent |
| Dart Support | ✅ Excellent | ✅ Excellent |
| Extensions | ✅ Thousands | ⚠️ Limited |
| Startup Time | ✅ <5 sec | ❌ 20-30 sec |
| Memory Usage | ✅ ~200MB | ❌ ~1GB |
| Git Integration | ✅ Great | ⚠️ Basic |
| Terminal | ✅ Integrated | ✅ Integrated |
| Android SDK | ❌ Needs AS | ✅ Built-in |
| Layout Editor | ❌ No | ✅ Yes (rarely needed) |

**Recommendation**: Use VS Code for coding, keep Android Studio for SDK/emulator management.

---

## Summary

### What You Need

1. ✅ **Flutter SDK** - Install once
2. ✅ **VS Code** - Your main IDE
3. ✅ **Android Studio** - Install once for SDK, then rarely use
4. ✅ **VS Code Flutter Extension** - Install once

### Daily Workflow

```bash
# Morning routine (once per day)
flutter emulators --launch Pixel_7_API_34

# Open VS Code
code c:\Repos\Booking\booksy-customer-app

# Press F5 to run
# Code all day in VS Code
# Hot reload with Ctrl+S
# Never open Android Studio
```

### Next Steps

1. Install Flutter SDK
2. Install VS Code + Flutter extension
3. Install Android Studio (for SDK only)
4. Create emulator (one time)
5. Code in VS Code forever! 🎉

---

**Bottom Line**: You can do 99% of Flutter development in VS Code. Android Studio is just for the Android SDK and creating emulators.

Happy coding in VS Code! 🚀
