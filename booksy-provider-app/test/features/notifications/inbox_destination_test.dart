import 'package:booksy_provider_app/config/routes/app_router.dart';
import 'package:booksy_provider_app/features/notifications/domain/inbox_item.dart';
import 'package:booksy_provider_app/features/notifications/presentation/inbox_destination.dart';
import 'package:flutter_test/flutter_test.dart';

/// Where a tapped notification goes (QA walkthrough 2026-09-22: a booking notice must open THAT booking).
InboxItem _item({String kind = 'Booking', String? id = 'b1', bool actionable = true}) => InboxItem(
      id: 'n1',
      subject: 's',
      body: 'b',
      createdAt: DateTime.utc(2026, 9, 22),
      destinationKind: kind,
      destinationId: id,
      isActionable: actionable,
    );

void main() {
  test('a booking notice opens that booking on the calendar', () {
    expect(inboxDestination(_item()), Routes.calendarBooking('b1'));
    expect(Routes.calendarBooking('b1'), '/calendar?booking=b1');
  });

  test('a notice the server did not mark actionable goes nowhere', () {
    expect(inboxDestination(_item(actionable: false)), isNull);
  });

  test('a kind with no screen goes nowhere', () {
    expect(inboxDestination(_item(kind: 'Payout')), isNull);
  });
}
