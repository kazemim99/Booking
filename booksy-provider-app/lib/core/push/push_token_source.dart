/// What the platform says about notification permission, read without asking.
enum PushPermission { granted, denied, notDetermined }

/// Where this device's push token comes from.
///
/// An interface so the registration logic can be tested without Firebase, and so an app build that has no
/// Firebase configuration degrades to "no push" instead of crashing at startup — which is what a bare
/// `Firebase.initializeApp()` does when `google-services.json` is missing.
abstract class PushTokenSource {
  /// False when push cannot work on this build (no Firebase configuration, or an unsupported platform/browser).
  Future<bool> isAvailable();

  /// True where the permission prompt may only follow a tap on something that asks for notifications — a browser.
  /// Firefox refuses the prompt otherwise and Chrome hides it behind a quiet icon.
  bool get promptNeedsUserAction;

  /// The current permission, without prompting.
  Future<PushPermission> permissionStatus();

  /// Asks the person. False when they declined — which is a normal outcome, not an error.
  Future<bool> requestPermission();

  /// The current token, or null when the platform has none to give.
  Future<String?> getToken();

  /// Fires when the messaging service replaces the token for this install.
  Stream<String> get onTokenRefresh;

  /// Forgets the token locally, so the next sign-in on this device gets a fresh one.
  Future<void> deleteToken();

  /// The value the backend's `DevicePlatform` enum expects: `Android`, `Ios` or `Web`.
  String get platform;
}

/// What the backend needs to know about a device. Kept to the two calls the device-token endpoint offers.
abstract class DeviceTokenApi {
  Future<void> register(String token, String platform);
  Future<void> revoke(String token);
}
