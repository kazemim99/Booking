/*
 * Web push service worker (identical in booksy-customer-app and booksy-provider-app).
 *
 * The app registers it (lib/core/push/web_push_config.dart › serviceWorkerPath) as
 *   push/firebase-messaging-sw.js?apiKey=…&appId=…&messagingSenderId=…&projectId=…
 * so its scope is <app>/push/, never the app's own: Flutter's loader re-registers its own worker at the app's scope
 * whenever a registration covers the page. The Firebase Web app's config rides in the query because a service worker
 * cannot read the app's --dart-define values. Without the config this worker does nothing — and the app never
 * registers it then.
 *
 * Two jobs:
 *  1. Show a push while no tab of the app is visible, and hand it to the visible tab otherwise — Firebase's compat SDK
 *     does both (the page's FirebaseMessaging.onMessage receives the second).
 *  2. Route a tap. Firebase's own click handler opens only a webpush link, which the backend cannot set: it does not
 *     know which of the two sites registered a token. This handler, registered FIRST, routes by the push's data
 *     instead: it focuses an open tab of the app and posts the data to it (lib/core/push/push_click_bridge_web.dart),
 *     or opens the app at #/push-open?<data>, which the app's router resolves with the same mapping a tap uses on
 *     Android (lib/core/push/push_open_route.dart).
 */
'use strict';

// Keep in step with firebase_core_web's supportedFirebaseJsSdkVersion (12.19.0 in firebase_core_web 3.12.0).
const FIREBASE_JS_SDK = '12.19.0';

// lib/core/push/push_click_message.dart › pushClickMessageType. Renaming one side alone drops every tap.
const TAP_MESSAGE = 'booksy-push-open';

self.addEventListener('notificationclick', (event) => {
  const notification = event.notification;
  const payload = notification.data && notification.data.FCM_MSG;
  const data = (payload && payload.data) || {};

  // Firebase's handler (added below, so called after this one) would close the notification and open nothing.
  event.stopImmediatePropagation();
  notification.close();
  event.waitUntil(openApp(data));
});

async function openApp(data) {
  // This file lives at <app>/push/.
  const appRoot = new URL('../', self.location.href).href;
  const tabs = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
  const tab = tabs.find((client) => client.url.startsWith(appRoot));

  if (tab) {
    try {
      await tab.focus();
    } catch (_) {
      // Focusing is best-effort; the tab still routes.
    }
    tab.postMessage({ type: TAP_MESSAGE, data });
    return;
  }

  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(data)) {
    if (value !== undefined && value !== null) query.set(key, String(value));
  }
  const suffix = query.toString();
  await self.clients.openWindow(appRoot + '#/push-open' + (suffix ? '?' + suffix : ''));
}

const params = new URL(self.location.href).searchParams;
const config = {
  apiKey: params.get('apiKey'),
  appId: params.get('appId'),
  messagingSenderId: params.get('messagingSenderId'),
  projectId: params.get('projectId'),
};

if (config.apiKey && config.appId && config.messagingSenderId && config.projectId) {
  // Deliberately not caught. If the SDK cannot be fetched, installing this worker fails, the app's getToken fails
  // with it, and the app reports push as unavailable and tries again on the next sign-in. A worker installed
  // without Firebase would receive pushes it cannot show. Once installed, the imported scripts are stored with the
  // worker, so waking it for a push does not fetch them again.
  importScripts(
    `https://www.gstatic.com/firebasejs/${FIREBASE_JS_SDK}/firebase-app-compat.js`,
    `https://www.gstatic.com/firebasejs/${FIREBASE_JS_SDK}/firebase-messaging-compat.js`,
  );
  firebase.initializeApp(config);
  firebase.messaging();
}
