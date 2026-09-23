import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:go_router/go_router.dart';

import '../../config/routes/app_router.dart';
import '../../features/notifications/presentation/inbox_cubit.dart';
import 'firebase_push_token_source.dart';
import 'push_open_route.dart';

/// Connects incoming push messages to the app. Deliberately thin: the decision of where a tap goes is
/// [pushOpenRoute], which is unit-tested; this only forwards Firebase's three entry points to it.
class PushMessageRouter {
  static bool _attached = false;

  /// Idempotent — the root widget can rebuild. A build without Firebase configuration attaches nothing.
  static Future<void> attach({
    required FirebasePushTokenSource source,
    required GoRouter router,
    required InboxCubit inbox,
  }) async {
    if (_attached || !await source.isAvailable()) return;
    _attached = true;

    void open(RemoteMessage message) {
      inbox.refreshCount();
      // Not a plain push: the tap can come while booking or checkout covers the tabs, where a push would crash.
      AppRouter.open(router, pushOpenRoute(message.data));
    }

    // Tapped while the app was in the background.
    FirebaseMessaging.onMessageOpenedApp.listen(open);

    // Tapped while the app was closed: the tap launched it.
    final initial = await FirebaseMessaging.instance.getInitialMessage();
    if (initial != null) open(initial);

    // Arrived while the salon is using the app. The system shows no banner in the foreground, and hijacking
    // the screen they are on would be worse, so only the badge moves.
    FirebaseMessaging.onMessage.listen((_) => inbox.refreshCount());
  }
}
