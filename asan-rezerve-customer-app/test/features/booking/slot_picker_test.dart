import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/utils/jalali_formatter.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:asan_rezerve_customer_app/features/booking/presentation/widgets/slot_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/semantics.dart';
import 'package:flutter_test/flutter_test.dart';

/// The day strip and the empty day (UX review 2026-09-23, #4): the salon's closed weekdays cannot be picked, and a
/// day with no free time offers a way forward instead of a dead end.
void main() {
  // A Wednesday; Thursday the 24th is the closed day in these tests.
  final today = DateTime(2026, 9, 23);
  DateTime day(int d) => DateTime(2026, 9, d);
  Finder chip(int d) => find.byKey(ValueKey('slot-picker-day-2026-9-$d'));

  Future<List<DateTime>> pumpPicker(
    WidgetTester tester, {
    Set<int> closedWeekdays = const {},
    List<TimeSlot> slots = const [],
    VoidCallback? onFindNextFreeDay,
    String? notice,
    String? emptyReason,
    Size size = const Size(390, 844),
  }) async {
    tester.view.physicalSize = size * 3;
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    final picked = <DateTime>[];
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: Scaffold(
          body: SlotPicker(
            today: today,
            selectedDate: today,
            onDateSelected: picked.add,
            status: SlotPickerStatus.loaded,
            slots: slots,
            selectedSlot: null,
            onSlotSelected: (_) {},
            onRetry: () {},
            daysToShow: 8,
            closedWeekdays: closedWeekdays,
            onFindNextFreeDay: onFindNextFreeDay,
            notice: notice,
            emptyReason: emptyReason,
          ),
        ),
      ),
    ));
    await tester.pump();
    return picked;
  }

  group('closed weekdays', () {
    testWidgets('a closed day cannot be picked; an open one can', (tester) async {
      final picked = await pumpPicker(tester, closedWeekdays: {DateTime.thursday});

      await tester.tap(chip(24));
      await tester.pump();
      expect(picked, isEmpty, reason: 'the salon is closed on Thursdays');

      await tester.tap(chip(25));
      await tester.pump();
      expect(picked, [day(25)]);
    });

    testWidgets('a screen reader hears that the day is closed', (tester) async {
      final handle = tester.ensureSemantics();
      await pumpPicker(tester, closedWeekdays: {DateTime.thursday});

      final closed = tester.getSemantics(chip(24));
      expect(closed.label, contains(AppStrings.bookingDayClosed));
      expect(closed.label, contains(JalaliFormatter.formatDate(day(24))));
      expect(closed.getSemanticsData().hasAction(SemanticsAction.tap), isFalse);

      final open = tester.getSemantics(chip(25));
      expect(open.label, isNot(contains(AppStrings.bookingDayClosed)));
      expect(open.getSemanticsData().hasAction(SemanticsAction.tap), isTrue);
      handle.dispose();
    });

    testWidgets('a closed day is drawn muted, unlike an open one', (tester) async {
      await pumpPicker(tester, closedWeekdays: {DateTime.thursday});

      Opacity? opacityOf(int d) => tester
          .widgetList<Opacity>(find.descendant(of: chip(d), matching: find.byType(Opacity)))
          .firstOrNull;

      expect(opacityOf(24)?.opacity, lessThan(1));
      expect(opacityOf(25), isNull);
    });

    testWidgets('without closed days every day stays pickable (reschedule keeps its behaviour)', (tester) async {
      final picked = await pumpPicker(tester);

      await tester.tap(chip(24));
      await tester.pump();
      expect(picked, [day(24)]);
    });
  });

  group('an empty day', () {
    testWidgets('offers the nearest day with free times', (tester) async {
      var asked = 0;
      await pumpPicker(tester, onFindNextFreeDay: () => asked++, emptyReason: 'مجموعه در این روز تعطیل است.');

      expect(find.text('مجموعه در این روز تعطیل است.'), findsOneWidget);
      await tester.tap(find.text(AppStrings.bookingFindNextFreeDay));
      await tester.pump();
      expect(asked, 1);
    });

    testWidgets('has no such button where nobody handles it', (tester) async {
      await pumpPicker(tester);

      expect(find.text(AppStrings.bookingFindNextFreeDay), findsNothing);
    });
  });

  testWidgets('says why the app chose the day it shows', (tester) async {
    await pumpPicker(
      tester,
      notice: AppStrings.bookingMovedFromToday,
      slots: [
        TimeSlot(
          startTime: DateTime(2026, 9, 23, 10),
          endTime: DateTime(2026, 9, 23, 10, 45),
          durationMinutes: 45,
          isAvailable: true,
        ),
      ],
    );

    expect(find.text(AppStrings.bookingMovedFromToday), findsOneWidget);
  });

  // The strip measures its two lines with text painters on every build, and the picker rebuilds on every bloc change
  // in both the booking and the reschedule flow: each painter must be disposed, or every build leaks its layout.
  testWidgets('measuring the day strip leaves no text painter undisposed', (tester) async {
    final live = <Object>{};
    void track(ObjectEvent event) {
      if (event is ObjectCreated && event.object is TextPainter) live.add(event.object);
      if (event is ObjectDisposed) live.remove(event.object);
    }

    FlutterMemoryAllocations.instance.addListener(track);
    addTearDown(() => FlutterMemoryAllocations.instance.removeListener(track));

    await pumpPicker(tester, closedWeekdays: {DateTime.thursday});
    await tester.pumpWidget(const SizedBox());

    expect(live, isEmpty);
  });

  testWidgets('fits a 360x640 screen at 1.3x text with a notice and an empty day', (tester) async {
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

    await pumpPicker(
      tester,
      size: const Size(360, 640),
      closedWeekdays: {DateTime.thursday},
      notice: AppStrings.bookingNoFreeDayInWindow,
      emptyReason: 'مجموعه در این روز تعطیل است.',
      onFindNextFreeDay: () {},
    );

    expect(tester.takeException(), isNull);
  });
}
