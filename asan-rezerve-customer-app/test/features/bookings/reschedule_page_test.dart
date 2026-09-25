import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/di/injection.dart';
import 'package:booksy_customer_app/core/utils/jalali_formatter.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/booking/presentation/widgets/slot_picker.dart';
import 'package:booksy_customer_app/features/bookings/domain/repositories/bookings_repository.dart';
import 'package:booksy_customer_app/features/bookings/presentation/pages/reschedule_page.dart';

import 'bookings_fakes.dart';
import 'vazir_font.dart';

/// The reschedule screen (UX review 2026-09-23, E.5): it says when the visit is now, offers the salon's booking
/// window rather than two weeks, and says why a day has no times.
void main() {
  late FakeSlots slots;
  final booking = fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30));

  // The 1.3x check measures the day strip and times with the real font.
  setUpAll(loadVazir);

  setUp(() {
    slots = FakeSlots(
      maxAdvanceBookingDays: 5,
      businessHours: const [BusinessHour(dayOfWeek: 'جمعه', isClosed: true)],
      day: const DaySlots(reason: 'مجموعه در این روز تعطیل است.'),
    );
    getIt
      ..registerSingleton<BookingsRepository>(FakeBookings(upcoming: [booking]))
      ..registerSingleton<BookingRepository>(slots);
  });

  tearDown(() => getIt.reset());

  Future<void> open(WidgetTester tester, {double textScale = 1.0}) async {
    tester.view.physicalSize = const Size(360 * 3, 640 * 3);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(textScaler: TextScaler.linear(textScale)),
        child: Directionality(textDirection: TextDirection.rtl, child: child!),
      ),
      home: ReschedulePage(booking: booking, now: () => DateTime(2026, 9, 23, 10, 15)),
    ));
    for (var i = 0; i < 10; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }
  }

  testWidgets('the header says when the visit is now', (tester) async {
    await open(tester);

    expect(find.textContaining(JalaliFormatter.formatDateTime(booking.startTime)), findsOneWidget);
  });

  testWidgets("the day strip is today plus the salon's window", (tester) async {
    await open(tester);

    expect(tester.widget<SlotPicker>(find.byType(SlotPicker)).daysToShow, 6);
  });

  // Review of the merged branch: the same rules as booking — the strip starts on the screen's own today, and the
  // salon's closed weekdays cannot be picked.
  testWidgets("the strip starts on the screen's today and marks the salon's closed weekdays", (tester) async {
    await open(tester);

    final picker = tester.widget<SlotPicker>(find.byType(SlotPicker));
    expect(picker.today, DateTime(2026, 9, 23));
    expect(picker.closedWeekdays, {DateTime.friday});
  });

  testWidgets("an empty day shows the salon's reason", (tester) async {
    await open(tester);

    expect(find.text('مجموعه در این روز تعطیل است.'), findsOneWidget);
  });

  testWidgets('fits a 360x640 phone at 1.3x text', (tester) async {
    await open(tester, textScale: 1.3);

    expect(tester.takeException(), isNull);
  });
}
