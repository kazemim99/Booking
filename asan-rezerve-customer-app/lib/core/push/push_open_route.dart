import '../../config/routes/app_router.dart';

/// Where tapping a push notification takes the customer: the appointment it is about, or else the inbox, which
/// always has the full notification and knows whether its target still exists.
String pushOpenRoute(Map<String, dynamic> data) {
  final bookingId = data['bookingId']?.toString();
  if (bookingId != null && bookingId.isNotEmpty) return Routes.appointmentDetail(bookingId);
  return Routes.notifications;
}
