import 'package:booksy_provider_app/config/routes/app_router.dart';
import 'package:booksy_provider_app/core/push/push_open_route.dart';
import 'package:flutter_test/flutter_test.dart';

/// Where a tapped push lands. The payload names the notification and, when there is one, the booking — the
/// backend started sending those keys in this same change; before that every push arrived with no data at all.
void main() {
  test('a push about a booking opens the calendar, where this app shows bookings', () {
    expect(pushOpenRoute({'notificationId': 'n1', 'bookingId': 'b1'}), Routes.calendar);
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
