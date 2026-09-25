import 'package:booksy_provider_app/config/routes/app_router.dart';
import 'package:booksy_provider_app/core/push/push_open_route.dart';
import 'package:flutter_test/flutter_test.dart';

/// Where a tapped push lands. The payload names the notification and, when there is one, the booking — the
/// backend started sending those keys in this same change; before that every push arrived with no data at all.
void main() {
  test('a push about a booking opens the calendar ON that booking, where the salon can confirm it', () {
    // The inbox's choice since the QA walkthrough of 2026-09-22 (inbox_destination.dart); a tapped push used to stop
    // at the plain calendar, one step short of the booking it was about.
    expect(pushOpenRoute({'notificationId': 'n1', 'bookingId': 'b1'}), Routes.calendarBooking('b1'));
  });

  test('a push about nothing in particular opens the inbox, where it can be read in full', () {
    expect(pushOpenRoute({'notificationId': 'n1'}), Routes.notifications);
  });

  test('a push with no data at all still lands somewhere useful', () {
    expect(pushOpenRoute(const {}), Routes.notifications);
  });

  test('an empty booking id is not a booking', () {
    expect(pushOpenRoute({'bookingId': ''}), Routes.notifications);
  });
}
