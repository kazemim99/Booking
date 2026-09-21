import '../../../config/routes/app_router.dart';
import '../domain/inbox_item.dart';

/// Where tapping a notification goes, or null for "mark it read and stay here".
///
/// A booking opens that appointment. Every other kind goes nowhere: a plausible-looking wrong screen is worse
/// than staying on the list, so a kind is added here only when its screen exists.
String? inboxDestination(InboxItem item) {
  final id = item.destinationId;
  if (!item.isActionable || id == null) return null;

  switch (item.destinationKind.toLowerCase()) {
    case 'booking':
      return Routes.appointmentDetail(id);
    default:
      return null;
  }
}
