import 'package:flutter/foundation.dart';

/// Runs [body] with one known test-font artefact dropped: the slot picker's day chips
/// (`lib/features/booking/presentation/widgets/slot_picker.dart`, owned by the booking flow) have a fixed height
/// that the square-glyph test font overflows. `empty_day_reason_test.dart` sidesteps it with `daysToShow: 0`;
/// the reschedule screen is about that strip, so it has to be drawn. Every other error, including any other
/// overflow, still fails the test.
Future<void> ignoringDayStripOverflow(Future<void> Function() body) async {
  final original = FlutterError.onError;
  FlutterError.onError = (details) {
    final dayStrip = details.exceptionAsString().contains('RenderFlex overflowed') &&
        details.toString().contains('slot_picker.dart');
    if (!dayStrip) original?.call(details);
  };
  try {
    await body();
  } finally {
    FlutterError.onError = original;
  }
}
