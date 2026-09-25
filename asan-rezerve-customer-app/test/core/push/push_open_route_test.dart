import 'package:asan_rezerve_customer_app/config/routes/app_router.dart';
import 'package:asan_rezerve_customer_app/core/push/push_open_route.dart';
import 'package:flutter_test/flutter_test.dart';

/// Where a tapped push lands. The payload names the notification and, when there is one, the booking.
void main() {
  test('a push about a booking opens that appointment', () {
    expect(pushOpenRoute({'notificationId': 'n1', 'bookingId': 'b1'}), Routes.appointmentDetail('b1'));
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
