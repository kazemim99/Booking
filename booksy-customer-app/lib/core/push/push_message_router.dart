import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../config/routes/app_router.dart';
import '../../features/notifications/presentation/inbox_cubit.dart';
import 'firebase_push_token_source.dart';
import 'push_click_bridge.dart';
import 'push_notice.dart';
import 'push_open_route.dart';

/// Connects incoming push messages to the app. Deliberately thin: the decision of where a tap goes is
/// [pushOpenRoute], which is unit-tested; this only forwards the entry points to it.
///
/// Where a tap comes from depends on the platform:
/// * Android: Firebase's `onMessageOpenedApp` (app in the background) and `getInitialMessage` (the tap launched it).
/// * Browser, a tab already open: the push service worker posts the tap to the tab ([serviceWorkerPushClicks]).
/// * Browser, no tab open: the worker opens the app at [Routes.pushOpen], which the router itself resolves.
class PushMessageRouter {
  static bool _attached = false;

  /// Idempotent — the root widget can rebuild. A build without Firebase configuration attaches nothing.
  static Future<void> attach({
    required FirebasePushTokenSource source,
    required GoRouter router,
    required InboxCubit inbox,
    GlobalKey<ScaffoldMessengerState>? messenger,
  }) async {
    if (_attached) return;
    _attached = true;

    void open(Map<String, dynamic> data) {
      inbox.refreshCount();
      // Not a plain push: the tap can come while booking or checkout covers the tabs, where a push would crash.
      AppRouter.open(router, pushOpenRoute(data));
    }

    // Independent of Firebase in the page: the worker posts these itself.
    serviceWorkerPushClicks().listen(open);

    if (!await source.isAvailable()) return;

    // Tapped while the app was in the background.
    FirebaseMessaging.onMessageOpenedApp.listen((message) => open(message.data));

    // Tapped while the app was closed: the tap launched it.
    final initial = await FirebaseMessaging.instance.getInitialMessage();
    if (initial != null) open(initial.data);

    // Arrived while the app is on screen. Neither Android nor the browser shows a banner for the app in front, so the
    // app shows a snackbar — never taking the screen the person is using — with a way to open what it is about.
    FirebaseMessaging.onMessage.listen((message) {
      inbox.refreshCount();
      final notice = pushNoticeFrom(
        title: message.notification?.title,
        body: message.notification?.body,
        data: message.data,
      );
      if (notice != null) showPushNotice(messenger?.currentState, notice, onOpen: open);
    });
  }
}
