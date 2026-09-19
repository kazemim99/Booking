import 'package:equatable/equatable.dart';

import 'saved_customer.dart';

/// One row of the provider's client book: a customer the salon saved
/// ([saved] set), someone who booked online (derived from bookings; identity
/// resolved by the backend's cross-schema seam), or both, matched by phone.
class ProviderClient extends Equatable {
  final String customerId;
  final String name;
  final String phone;
  final int totalBookings;
  final int completedBookings;
  final int upcomingBookings;
  final DateTime? lastVisitAt;

  /// The customer-book entry behind this row; null for online-only clients.
  final SavedCustomer? saved;

  const ProviderClient({
    required this.customerId,
    required this.name,
    required this.phone,
    this.totalBookings = 0,
    this.completedBookings = 0,
    this.upcomingBookings = 0,
    this.lastVisitAt,
    this.saved,
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
        saved,
      ];
}
