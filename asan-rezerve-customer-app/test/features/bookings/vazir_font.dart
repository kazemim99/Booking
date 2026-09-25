import 'dart:io';

import 'package:flutter/services.dart';

/// Loads the app's real Persian font, so layout checks measure the text a phone draws: the test font draws every
/// glyph as a 1em square, which is neither the width nor the height of Vazir. Call from `setUpAll`.
Future<void> loadVazir() async {
  final vazir = FontLoader('Vazir');
  for (final file in ['Vazir.ttf', 'Vazir-Medium.ttf', 'Vazir-Bold.ttf']) {
    final bytes = File('assets/fonts/vazir/$file').readAsBytesSync();
    vazir.addFont(Future.value(ByteData.sublistView(bytes)));
  }
  await vazir.load();
}
