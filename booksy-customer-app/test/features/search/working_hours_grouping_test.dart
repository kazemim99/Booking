import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/working_hours_section.dart';
import 'package:flutter_test/flutter_test.dart';

/// Seven rows that say the same thing are seven rows nobody reads
/// (openspec/changes/customer-app-discovery-pass): days that keep the same
/// hours share one row, and only the day that differs stands on its own.
void main() {
  BusinessHour day(String name, String? open, String? close,
          {bool closed = false, List<BusinessBreak> breaks = const []}) =>
      BusinessHour(
        dayOfWeek: name,
        openTime: open,
        closeTime: close,
        isClosed: closed,
        breaks: breaks,
      );

  test('the six identical days become one row, the odd one keeps its own', () {
    final grouped = WorkingHoursSection.group([
      day('شنبه', '09:00', '18:00'),
      day('یکشنبه', '11:00', '12:00'),
      day('دوشنبه', '09:00', '18:00'),
      day('سه‌شنبه', '09:00', '18:00'),
      day('چهارشنبه', '09:00', '18:00'),
      day('پنجشنبه', '09:00', '18:00'),
      day('جمعه', '09:00', '18:00'),
    ]);

    expect(grouped, hasLength(2), reason: 'only Sunday differs');
    expect(grouped[0].label, 'شنبه، دوشنبه تا جمعه',
        reason: 'neighbouring days read as a range, the rest are listed');
    expect(grouped[0].hours.openTime, '09:00');
    expect(grouped[1].label, 'یکشنبه');
    expect(grouped[1].hours.openTime, '11:00');
  });

  test('days that are not next to each other are listed, not ranged', () {
    final grouped = WorkingHoursSection.group([
      day('شنبه', '09:00', '18:00'),
      day('یکشنبه', '10:00', '20:00'),
      day('دوشنبه', '09:00', '18:00'),
    ]);

    expect(grouped, hasLength(2));
    expect(grouped.first.label, 'شنبه، دوشنبه');
  });

  test('closed days group together too', () {
    final grouped = WorkingHoursSection.group([
      day('پنجشنبه', null, null, closed: true),
      day('جمعه', null, null, closed: true),
    ]);

    expect(grouped, hasLength(1));
    expect(grouped.single.label, 'پنجشنبه تا جمعه');
    expect(grouped.single.hours.isClosed, isTrue);
  });

  test('a different break makes a different row', () {
    final grouped = WorkingHoursSection.group([
      day('شنبه', '09:00', '18:00',
          breaks: const [BusinessBreak(startTime: '13:00', endTime: '14:00')]),
      day('یکشنبه', '09:00', '18:00'),
    ]);

    expect(grouped, hasLength(2), reason: 'one of them shuts at lunchtime');
  });
}
