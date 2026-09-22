import '../../../config/routes/app_router.dart';
import '../domain/inbox_item.dart';

/// Where tapping a notification goes, or null for "mark it read and stay here".
///
/// A booking notice opens the calendar ON that booking — its day selected and its sheet open, so a new request
/// can be confirmed or declined from the notification (QA walkthrough 2026-09-22). Every other kind goes nowhere:
/// a plausible-looking wrong screen is worse than staying on the list. Add a kind when its screen exists.
String? inboxDestination(InboxItem item) {
  if (!item.isActionable || item.destinationId == null) return null;

  switch (item.destinationKind.toLowerCase()) {
    case 'booking':
      return Routes.calendarBooking(item.destinationId!);
    default:
      return null;
  }
}
