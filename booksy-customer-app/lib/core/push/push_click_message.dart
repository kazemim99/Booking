/// The message web/push/firebase-messaging-sw.js posts to an open tab when one of its notifications is tapped.
///
/// The worker posts `{type: 'booksy-push-open', data: {...the push's data...}}`. Keep the literal in step with it.
const String pushClickMessageType = 'booksy-push-open';

/// The push data a tap message carries, or null when the message is not one of ours. The page hears messages from
/// every service worker of the site — Firebase's own "push-received" among them — and only our tap may move the
/// screen.
Map<String, dynamic>? pushClickData(Object? message) {
  if (message is! Map || message['type'] != pushClickMessageType) return null;

  final data = message['data'];
  if (data is! Map) return <String, dynamic>{};

  return {
    for (final entry in data.entries)
      if (entry.value != null) entry.key.toString(): entry.value.toString(),
  };
}
