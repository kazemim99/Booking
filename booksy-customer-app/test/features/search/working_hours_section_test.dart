import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/features/booking/data/repositories/booking_repository_impl.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/working_hours_section.dart';

Widget _wrap(Widget child, {double textScale = 1.0}) => MaterialApp(
      theme: AppTheme.light,
      builder: (context, appChild) => MediaQuery(
        data: MediaQuery.of(context)
            .copyWith(textScaler: TextScaler.linear(textScale)),
        child: Directionality(
          textDirection: TextDirection.rtl,
          child: appChild!,
        ),
      ),
      home: Scaffold(body: SingleChildScrollView(child: child)),
    );

/// Day names/times are taken from the real parser rather than hand-written, so
/// this test fails if the two ever disagree about spelling (the Tuesday name
/// carries a zero-width non-joiner) or about the `HH:mm` format.
List<BusinessHour> _parsed() => BookingRepositoryImpl.parseBusinessHours([
      // .NET DayOfWeek: 0 = Sunday … 6 = Saturday.
      {
        'dayOfWeek': 2, // Tuesday — the ZWNJ case
        'isOpen': true,
        'openTimeHours': 9,
        'openTimeMinutes': 0,
        'closeTimeHours': 18,
        'closeTimeMinutes': 30,
      },
      {'dayOfWeek': 5, 'isOpen': false}, // Friday closed
      {
        'dayOfWeek': 6, // Saturday
        'isOpen': true,
        'openTimeHours': 10,
        'openTimeMinutes': 0,
        'closeTimeHours': 20,
        'closeTimeMinutes': 0,
      },
    ]);

/// 2026-08-11 is a Tuesday; 2026-08-14 a Friday; 2026-08-15 a Saturday.
DateTime _tuesdayAt(int hour, [int minute = 0]) =>
    DateTime(2026, 8, 11, hour, minute);

void main() {
  group('WorkingHoursSection.isOpenNow', () {
    test('is open inside the parsed hours for today', () {
      expect(WorkingHoursSection.isOpenNow(_parsed(), _tuesdayAt(12)), isTrue);
      expect(
        WorkingHoursSection.isOpenNow(_parsed(), _tuesdayAt(9)),
        isTrue,
        reason: 'the opening minute counts as open',
      );
    });

    test('is closed before opening and after closing', () {
      expect(WorkingHoursSection.isOpenNow(_parsed(), _tuesdayAt(8, 59)),
          isFalse);
      expect(WorkingHoursSection.isOpenNow(_parsed(), _tuesdayAt(18, 30)),
          isFalse);
      expect(WorkingHoursSection.isOpenNow(_parsed(), _tuesdayAt(23)), isFalse);
    });

    test('is closed on a day the provider marked closed', () {
      // Friday.
      expect(
        WorkingHoursSection.isOpenNow(_parsed(), DateTime(2026, 8, 14, 12)),
        isFalse,
      );
    });

    test('is unknown when today has no row at all', () {
      // Sunday is absent from the payload — unknown, not "closed".
      expect(
        WorkingHoursSection.isOpenNow(_parsed(), DateTime(2026, 8, 16, 12)),
        isNull,
      );
      expect(WorkingHoursSection.isOpenNow(const [], _tuesdayAt(12)), isNull);
    });

    test('is unknown when an open day carries no times', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        {'dayOfWeek': 2, 'isOpen': true},
      ]);
      expect(WorkingHoursSection.isOpenNow(hours, _tuesdayAt(12)), isNull);
    });

    test('handles a shift that runs past midnight', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        {
          'dayOfWeek': 2,
          'isOpen': true,
          'openTimeHours': 20,
          'openTimeMinutes': 0,
          'closeTimeHours': 2,
          'closeTimeMinutes': 0,
        },
      ]);
      expect(WorkingHoursSection.isOpenNow(hours, _tuesdayAt(23)), isTrue);
      expect(WorkingHoursSection.isOpenNow(hours, _tuesdayAt(1)), isTrue);
      expect(WorkingHoursSection.isOpenNow(hours, _tuesdayAt(12)), isFalse);
    });
  });

  group('WorkingHoursSection rendering', () {
    testWidgets('lists every parsed day and marks closed days', (tester) async {
      await tester.pumpWidget(
        _wrap(WorkingHoursSection(hours: _parsed(), now: _tuesdayAt(12))),
      );

      expect(find.text(AppStrings.workingHoursTitle), findsOneWidget);
      // Saturday-first ordering comes from the parser; all three rows render.
      for (final hour in _parsed()) {
        expect(find.text(hour.dayOfWeek), findsOneWidget);
      }
      expect(find.text(AppStrings.closedDay), findsOneWidget);
      expect(find.text('۰۹:۰۰ – ۱۸:۳۰'), findsOneWidget);
    });

    testWidgets('shows the open-now badge only while open', (tester) async {
      await tester.pumpWidget(
        _wrap(WorkingHoursSection(hours: _parsed(), now: _tuesdayAt(12))),
      );
      expect(find.byKey(const Key('provider-open-now-badge')), findsOneWidget);
      expect(find.text(AppStrings.openNow), findsOneWidget);

      await tester.pumpWidget(
        _wrap(WorkingHoursSection(hours: _parsed(), now: _tuesdayAt(23))),
      );
      expect(find.byKey(const Key('provider-open-now-badge')), findsNothing);
    });

    testWidgets('shows no badge when openness cannot be determined',
        (tester) async {
      // Sunday: no row for today.
      await tester.pumpWidget(
        _wrap(WorkingHoursSection(
          hours: _parsed(),
          now: DateTime(2026, 8, 16, 12),
        )),
      );
      expect(find.byKey(const Key('provider-open-now-badge')), findsNothing);
    });

    testWidgets('renders right-to-left without overflow at 1.3x text scale',
        (tester) async {
      await tester.pumpWidget(
        _wrap(
          WorkingHoursSection(hours: _parsed(), now: _tuesdayAt(12)),
          textScale: 1.3,
        ),
      );

      expect(tester.takeException(), isNull);
      final direction = Directionality.of(
        tester.element(find.text(AppStrings.workingHoursTitle)),
      );
      expect(direction, TextDirection.rtl);
    });
  });
}
