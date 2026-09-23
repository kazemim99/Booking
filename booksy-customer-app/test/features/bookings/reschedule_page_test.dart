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
import 'day_strip_overflow.dart';

/// The reschedule screen (UX review 2026-09-23, E.5): it says when the visit is now, offers the salon's booking
/// window rather than two weeks, and says why a day has no times.
void main() {
  late FakeSlots slots;
  final booking = fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30));

  setUp(() {
    slots = FakeSlots(maxAdvanceBookingDays: 5, day: const DaySlots(reason: 'مجموعه در این روز تعطیل است.'));
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
      home: ReschedulePage(booking: booking),
    ));
    for (var i = 0; i < 10; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }
  }

  // Every test draws the day strip, so each runs under [ignoringDayStripOverflow].
  testWidgets(
      'the header says when the visit is now',
      (tester) => ignoringDayStripOverflow(() async {
            await open(tester);

            expect(find.textContaining(JalaliFormatter.formatDateTime(booking.startTime)), findsOneWidget);
          }));

  testWidgets(
      "the day strip is today plus the salon's window",
      (tester) => ignoringDayStripOverflow(() async {
            await open(tester);

            expect(tester.widget<SlotPicker>(find.byType(SlotPicker)).daysToShow, 6);
          }));

  testWidgets(
      "an empty day shows the salon's reason",
      (tester) => ignoringDayStripOverflow(() async {
            await open(tester);

            expect(find.text('مجموعه در این روز تعطیل است.'), findsOneWidget);
          }));

  testWidgets(
      'fits a 360x640 phone at 1.3x text',
      (tester) => ignoringDayStripOverflow(() async {
            await open(tester, textScale: 1.3);

            expect(tester.takeException(), isNull);
          }));
}
