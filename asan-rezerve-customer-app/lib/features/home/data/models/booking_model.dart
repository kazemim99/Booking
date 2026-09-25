import '../../../../core/api/models/booking_models.dart';
import '../../domain/entities/upcoming_booking.dart';

/// Extension to convert BookingDto to UpcomingBooking entity
extension BookingMapper on BookingDto {
  UpcomingBooking toUpcomingBooking() {
    return UpcomingBooking(
      id: id,
      providerName: 'Provider', // TODO: Add provider name to BookingDto
      serviceName: 'Service', // TODO: Add service name to BookingDto
      dateTime: startTime,
      durationMinutes: endTime != null
          ? endTime!.difference(startTime).inMinutes
          : 60, // Default 60 minutes
      price: totalAmount?.toInt() ?? 0,
      status: status.name,
      providerLat: null, // TODO: Add provider location to BookingDto
      providerLng: null,
    );
  }
}
