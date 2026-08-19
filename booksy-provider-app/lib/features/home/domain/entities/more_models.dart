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

/// A member of the organization (membership model) shown on More → تیم.
/// Sourced from `/hierarchy/members`; a member appears here once they accept an
/// invitation (an owner appears from onboarding).
class OrgMember extends Equatable {
  final String membershipId;
  final String? personId;
  final String name;
  final String? phone;
  final List<String> roles;
  final String status;
  final bool isOwner;
  final bool providesServices;

  const OrgMember({
    required this.membershipId,
    this.personId,
    this.name = '',
    this.phone,
    this.roles = const [],
    this.status = '',
    this.isOwner = false,
    this.providesServices = false,
  });

  bool get isActive => status == 'Active';

  @override
  List<Object?> get props => [
        membershipId,
        personId,
        name,
        phone,
        roles,
        status,
        isOwner,
        providesServices,
      ];
}

/// One of the person's organization memberships (More → سالن‌های من).
/// A person may hold several — the model that makes multi-salon (S6) visible.
class ProviderMembership extends Equatable {
  final String membershipId;
  final String organizationId;
  final String organizationName;
  final String? organizationLogo;
  final List<String> roles;
  final String status;
  final bool providesServices;

  const ProviderMembership({
    required this.membershipId,
    required this.organizationId,
    required this.organizationName,
    this.organizationLogo,
    this.roles = const [],
    this.status = '',
    this.providesServices = false,
  });

  bool get isOwner => roles.contains('Owner');
  bool get isActive => status == 'Active';

  @override
  List<Object?> get props => [
        membershipId,
        organizationId,
        organizationName,
        organizationLogo,
        roles,
        status,
        providesServices,
      ];
}

/// The editable public business profile (More → مشخصات کسب‌وکار).
class BusinessProfile extends Equatable {
  final String businessName;
  final String description;

  const BusinessProfile({
    required this.businessName,
    this.description = '',
  });

  @override
  List<Object?> get props => [businessName, description];
}

/// A provider day off (More → تعطیلات و مرخصی).
class ProviderHoliday extends Equatable {
  final String id;

  /// Date-only (local).
  final DateTime date;
  final String reason;
  final bool isRecurring;

  const ProviderHoliday({
    required this.id,
    required this.date,
    required this.reason,
    this.isRecurring = false,
  });

  /// Whether this holiday applies to [day] (exact date, or month/day match
  /// for yearly recurring ones).
  bool appliesTo(DateTime day) => isRecurring
      ? date.month == day.month && date.day == day.day
      : date.year == day.year &&
          date.month == day.month &&
          date.day == day.day;

  @override
  List<Object?> get props => [id, date, reason, isRecurring];
}

/// A gallery photo (More → گالری).
class GalleryImage extends Equatable {
  final String id;
  final String thumbnailUrl;
  final String originalUrl;
  final bool isPrimary;
  final int displayOrder;

  const GalleryImage({
    required this.id,
    required this.thumbnailUrl,
    this.originalUrl = '',
    this.isPrimary = false,
    this.displayOrder = 0,
  });

  @override
  List<Object?> get props =>
      [id, thumbnailUrl, originalUrl, isPrimary, displayOrder];
}

/// A per-date availability exception (block-time: all-day closed, or
/// modified hours for that date).
class AvailabilityException extends Equatable {
  final String id;

  /// Date-only (local).
  final DateTime date;

  /// "HH:mm" when the date has modified hours; null when closed all day.
  final String? openTime;
  final String? closeTime;
  final String reason;
  final bool isClosed;

  const AvailabilityException({
    required this.id,
    required this.date,
    this.openTime,
    this.closeTime,
    required this.reason,
    this.isClosed = false,
  });

  bool appliesTo(DateTime day) =>
      date.year == day.year && date.month == day.month && date.day == day.day;

  @override
  List<Object?> get props => [id, date, openTime, closeTime, reason, isClosed];
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
