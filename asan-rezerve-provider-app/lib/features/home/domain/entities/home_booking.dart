import 'package:equatable/equatable.dart';

/// Canonical booking lifecycle states as the Home consumes them.
enum HomeBookingStatus { pending, confirmed, completed, noShow, cancelled }

/// A lightweight view of one booking, carried inside `HomeContext` so the
/// agenda / now-next / action-queue widgets render rows without their own
/// fetch cycle (the design mandates widgets read only from the context).
class HomeBooking extends Equatable {
  final String id;
  final DateTime? start;

  /// Slot end, when the backend provides it (drives the time-range row).
  final DateTime? end;
  final String clientName;
  final String clientPhone;
  final String serviceName;

  /// Total price + ISO/display currency, when provided.
  final double? price;
  final String currency;
  final HomeBookingStatus status;

  const HomeBooking({
    required this.id,
    required this.start,
    this.end,
    required this.clientName,
    this.clientPhone = '',
    required this.serviceName,
    this.price,
    this.currency = '',
    required this.status,
  });

  HomeBooking copyWith({String? serviceName}) => HomeBooking(
        id: id,
        start: start,
        end: end,
        clientName: clientName,
        clientPhone: clientPhone,
        serviceName: serviceName ?? this.serviceName,
        price: price,
        currency: currency,
        status: status,
      );

  bool get isDone =>
      status == HomeBookingStatus.completed ||
      status == HomeBookingStatus.noShow;

  @override
  List<Object?> get props =>
      [id, start, end, clientName, clientPhone, serviceName, price, currency, status];
}
