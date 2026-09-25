import 'package:equatable/equatable.dart';

/// One notification in the signed-in customer's inbox, as the backend rendered it.
///
/// [eventCode] is what to choose an icon from — never parse the Persian copy. [isActionable] is false when the
/// target is gone or no longer this person's: the row still shows, because its text is history, but tapping it
/// must not lead anywhere.
class InboxItem extends Equatable {
  final String id;
  final String? eventCode;
  final String subject;
  final String body;
  final DateTime createdAt;
  final DateTime? readAt;
  final String destinationKind;
  final String? destinationId;
  final bool isActionable;

  const InboxItem({
    required this.id,
    required this.subject,
    required this.body,
    required this.createdAt,
    this.eventCode,
    this.readAt,
    this.destinationKind = 'None',
    this.destinationId,
    this.isActionable = false,
  });

  bool get isUnread => readAt == null;

  InboxItem markedRead(DateTime at) => InboxItem(
        id: id,
        eventCode: eventCode,
        subject: subject,
        body: body,
        createdAt: createdAt,
        readAt: at,
        destinationKind: destinationKind,
        destinationId: destinationId,
        isActionable: isActionable,
      );

  factory InboxItem.fromJson(Map<String, dynamic> json) => InboxItem(
        id: json['id']?.toString() ?? '',
        eventCode: json['eventCode']?.toString(),
        subject: json['subject']?.toString() ?? '',
        body: json['body']?.toString() ?? '',
        createdAt: DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
            DateTime.fromMillisecondsSinceEpoch(0),
        readAt: DateTime.tryParse(json['readAt']?.toString() ?? ''),
        destinationKind: json['destinationKind']?.toString() ?? 'None',
        destinationId: json['destinationId']?.toString(),
        isActionable: json['isActionable'] == true,
      );

  @override
  List<Object?> get props => [id, readAt];
}

/// A page of the inbox, newest first, with the counts the server computed for it.
class InboxResult extends Equatable {
  final List<InboxItem> items;
  final int totalCount;
  final int unreadCount;

  const InboxResult({required this.items, required this.totalCount, required this.unreadCount});

  @override
  List<Object?> get props => [items, totalCount, unreadCount];
}
