import 'dart:io';

import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/utils/jalali_formatter.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:asan_rezerve_customer_app/features/booking/presentation/widgets/slot_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

/// The day strip measured with the font the app ships. The test font draws every glyph as a 1em square with its own
/// line height, so a strip sized for it says nothing about Vazir's taller lines and wider Persian words; these checks
/// load the real Vazir, as the bottom bar's layout tests do.
void main() {
  setUpAll(() async {
    final vazir = FontLoader('Vazir');
    for (final file in ['Vazir.ttf', 'Vazir-Medium.ttf', 'Vazir-Bold.ttf']) {
      final bytes = File('assets/fonts/vazir/$file').readAsBytesSync();
      vazir.addFont(Future.value(ByteData.sublistView(bytes)));
    }
    await vazir.load();
  });

  // A Wednesday; Thursday the 24th is closed.
  final today = DateTime(2026, 9, 23);
  Finder chip(int d) => find.byKey(ValueKey('slot-picker-day-2026-9-$d'));

  Future<void> pumpPicker(WidgetTester tester, {required DateTime selected}) async {
    tester.view.physicalSize = const Size(360, 640) * 3;
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: Scaffold(
          body: SlotPicker(
            today: today,
            selectedDate: selected,
            onDateSelected: (_) {},
            status: SlotPickerStatus.loaded,
            slots: [
              for (final hour in [9, 10, 11, 16, 17])
                TimeSlot(
                  startTime: DateTime(2026, 9, selected.day, hour),
                  endTime: DateTime(2026, 9, selected.day, hour, 45),
                  durationMinutes: 45,
                  isAvailable: true,
                ),
            ],
            selectedSlot: null,
            onSlotSelected: (_) {},
            onRetry: () {},
            closedWeekdays: const {DateTime.thursday},
            notice: AppStrings.bookingMovedFromToday,
          ),
        ),
      ),
    ));
    await tester.pump();
  }

  /// Every line of a day chip lies inside the chip: nothing is clipped at the top or bottom.
  void expectLinesInside(WidgetTester tester, int d) {
    final box = tester.getRect(chip(d));
    final lines = find.descendant(of: chip(d), matching: find.byType(Text));
    expect(lines, findsNWidgets(2));
    for (final line in tester.elementList(lines)) {
      final rect = tester.getRect(find.byElementPredicate((e) => e == line));
      expect(rect.top, greaterThanOrEqualTo(box.top - 0.01), reason: 'day ${d}th, line starts inside the chip');
      expect(rect.bottom, lessThanOrEqualTo(box.bottom + 0.01), reason: 'day ${d}th, line ends inside the chip');
    }
  }

  testWidgets('day chips fit a 360x640 screen at 1.3x text in Vazir', (tester) async {
    await pumpPicker(tester, selected: today);

    expect(tester.takeException(), isNull);
    expectLinesInside(tester, 23); // selected
    expectLinesInside(tester, 24); // closed
    expectLinesInside(tester, 25); // open
    expect(find.text(AppStrings.bookingDayClosed), findsOneWidget);
    expect(find.text(JalaliFormatter.formatShortDate(DateTime(2026, 9, 25))), findsOneWidget);

    // Nothing in the chips was cut short with an ellipsis: each line is as wide as its text.
    for (final element in tester.elementList(find.descendant(of: chip(25), matching: find.byType(Text)))) {
      final paragraph = element.renderObject! as RenderParagraph;
      expect(paragraph.didExceedMaxLines, isFalse);
    }
  });

  testWidgets('a selected closed day fits too', (tester) async {
    // Today is always shown first, closed or not; a selected closed day shows its date instead of «تعطیل».
    await pumpPicker(tester, selected: DateTime(2026, 9, 24));

    expect(tester.takeException(), isNull);
    expectLinesInside(tester, 24);
  });
}
