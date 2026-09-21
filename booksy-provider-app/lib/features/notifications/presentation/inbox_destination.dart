import '../../../config/routes/app_router.dart';
import '../domain/inbox_item.dart';

/// Where tapping a notification goes, or null for "mark it read and stay here".
///
/// This app has no single-booking screen — a salon sees its bookings on the calendar — so a booking notice
/// opens the calendar rather than that one booking. Every other kind goes nowhere: a plausible-looking wrong
/// screen is worse than staying on the list. Add a kind when its screen exists.
String? inboxDestination(InboxItem item) {
  if (!item.isActionable || item.destinationId == null) return null;

  switch (item.destinationKind.toLowerCase()) {
    case 'booking':
      return Routes.calendar;
    default:
      return null;
  }
}
