import '../../config/routes/app_router.dart';

/// Where tapping a push notification takes the salon.
///
/// A booking push opens the calendar ON that booking — its day selected and its sheet open — the same choice the
/// inbox makes (see `inbox_destination.dart`), so a new request can be confirmed from the notification. Anything else
/// opens the inbox, which always has the full notification and knows whether its target still exists; guessing a
/// narrower screen from a push payload is how a tap ends up somewhere wrong.
String pushOpenRoute(Map<String, dynamic> data) {
  final bookingId = data['bookingId']?.toString();
  if (bookingId != null && bookingId.isNotEmpty) return Routes.calendarBooking(bookingId);
  return Routes.notifications;
}
