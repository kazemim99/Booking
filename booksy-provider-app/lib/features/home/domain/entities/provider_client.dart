import 'package:equatable/equatable.dart';

/// One row of the provider's client book (derived from bookings; identity
/// resolved by the backend's cross-schema seam).
class ProviderClient extends Equatable {
  final String customerId;
  final String name;
  final String phone;
  final int totalBookings;
  final int completedBookings;
  final int upcomingBookings;
  final DateTime? lastVisitAt;

  const ProviderClient({
    required this.customerId,
    required this.name,
    required this.phone,
    this.totalBookings = 0,
    this.completedBookings = 0,
    this.upcomingBookings = 0,
    this.lastVisitAt,
  });

  @override
  List<Object?> get props => [
        customerId,
        name,
        phone,
        totalBookings,
        completedBookings,
        upcomingBookings,
        lastVisitAt,
      ];
}
