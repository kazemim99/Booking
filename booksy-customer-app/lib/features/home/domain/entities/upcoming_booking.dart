import 'package:equatable/equatable.dart';

/// Upcoming booking summary for home screen
class UpcomingBooking extends Equatable {
  final String id;
  final String providerName;
  final String serviceName;
  final DateTime dateTime;
  final int durationMinutes;
  final int price;
  final String status;
  final double? providerLat;
  final double? providerLng;

  const UpcomingBooking({
    required this.id,
    required this.providerName,
    required this.serviceName,
    required this.dateTime,
    required this.durationMinutes,
    required this.price,
    required this.status,
    this.providerLat,
    this.providerLng,
  });

  @override
  List<Object?> get props => [
        id,
        providerName,
        serviceName,
        dateTime,
        durationMinutes,
        price,
        status,
        providerLat,
        providerLng,
      ];
}
