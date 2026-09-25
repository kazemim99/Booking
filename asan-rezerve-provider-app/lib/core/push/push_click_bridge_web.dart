import 'dart:async';
import 'dart:js_interop';
import 'dart:js_interop_unsafe';

import 'push_click_message.dart';

/// Listens to messages from the site's service workers and passes on the taps posted by ours.
Stream<Map<String, dynamic>> serviceWorkerPushClicks() {
  final navigator = globalContext.getProperty<JSObject?>('navigator'.toJS);
  final container = navigator?.getProperty<JSObject?>('serviceWorker'.toJS);
  if (container == null) return const Stream.empty();

  final taps = StreamController<Map<String, dynamic>>.broadcast();
  void onMessage(JSObject event) {
    final data = pushClickData(event.getProperty<JSAny?>('data'.toJS).dartify());
    if (data != null) taps.add(data);
  }

  container.callMethod<JSAny?>('addEventListener'.toJS, 'message'.toJS, onMessage.toJS);
  return taps.stream;
}
