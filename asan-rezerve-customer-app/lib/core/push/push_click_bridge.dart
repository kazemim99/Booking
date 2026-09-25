import 'push_click_bridge_stub.dart' if (dart.library.js_interop) 'push_click_bridge_web.dart' as impl;

/// Notifications tapped while the app is open in a browser tab, as push data.
///
/// On Android, Firebase reports a tap itself (`onMessageOpenedApp`); in a browser it never does, so the push service
/// worker posts the tap to the open tab and this hands it over. Empty everywhere but the web.
Stream<Map<String, dynamic>> serviceWorkerPushClicks() => impl.serviceWorkerPushClicks();
