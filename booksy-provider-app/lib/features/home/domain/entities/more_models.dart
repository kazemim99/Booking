import 'package:equatable/equatable.dart';

/// A team member row on the More → Staff screen.
class ProviderStaffMember extends Equatable {
  final String id;

  /// Display name (backend fullName, falling back to first+last).
  final String name;
  final String firstName;
  final String lastName;
  final String phone;
  final String role;
  final bool isActive;

  const ProviderStaffMember({
    required this.id,
    required this.name,
    this.firstName = '',
    this.lastName = '',
    this.phone = '',
    this.role = '',
    this.isActive = true,
  });

  @override
  List<Object?> get props =>
      [id, name, firstName, lastName, phone, role, isActive];
}

/// Booking statistics for the Insights screen (all-time + trailing 30 days).
class InsightsSummary extends Equatable {
  final int totalBookings;
  final int completedBookings;
  final int cancelledBookings;
  final int noShowBookings;

  /// Turnover across all bookings (includes pending amounts).
  final double totalRevenue;

  /// Revenue from completed bookings only.
  final double completedRevenue;
  final String currency;
  final int bookingsTrailing30d;

  const InsightsSummary({
    this.totalBookings = 0,
    this.completedBookings = 0,
    this.cancelledBookings = 0,
    this.noShowBookings = 0,
    this.totalRevenue = 0,
    this.completedRevenue = 0,
    this.currency = '',
    this.bookingsTrailing30d = 0,
  });

  @override
  List<Object?> get props => [
        totalBookings,
        completedBookings,
        cancelledBookings,
        noShowBookings,
        totalRevenue,
        completedRevenue,
        currency,
        bookingsTrailing30d,
      ];
}
