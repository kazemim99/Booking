import '../../config/routes/app_router.dart';

/// Where tapping a push notification takes the salon.
///
/// This app has no single-booking screen, so a booking push opens the calendar (the same choice the inbox makes
/// — see `inbox_destination.dart`). Anything else opens the inbox, which always has the full notification and
/// knows whether its target still exists; guessing a narrower screen from a push payload is how a tap ends up
/// somewhere wrong.
String pushOpenRoute(Map<String, dynamic> data) {
  final bookingId = data['bookingId']?.toString();
  if (bookingId != null && bookingId.isNotEmpty) return Routes.calendar;
  return Routes.notifications;
}
