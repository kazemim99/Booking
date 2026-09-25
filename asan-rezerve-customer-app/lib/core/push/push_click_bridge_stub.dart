/// Not a browser: taps arrive through Firebase's own callbacks.
Stream<Map<String, dynamic>> serviceWorkerPushClicks() => const Stream.empty();
