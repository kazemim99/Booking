import 'package:equatable/equatable.dart';

/// Canonical booking lifecycle states as the Home consumes them.
enum HomeBookingStatus { pending, confirmed, completed, noShow, cancelled }

/// A lightweight view of one booking, carried inside `HomeContext` so the
/// agenda / now-next / action-queue widgets render rows without their own
/// fetch cycle (the design mandates widgets read only from the context).
class HomeBooking extends Equatable {
  final String id;
  final DateTime? start;
  final String clientName;
  final String clientPhone;
  final String serviceName;
  final HomeBookingStatus status;

  const HomeBooking({
    required this.id,
    required this.start,
    required this.clientName,
    this.clientPhone = '',
    required this.serviceName,
    required this.status,
  });

  bool get isDone =>
      status == HomeBookingStatus.completed ||
      status == HomeBookingStatus.noShow;

  @override
  List<Object?> get props =>
      [id, start, clientName, clientPhone, serviceName, status];
}
