import 'package:equatable/equatable.dart';

/// Full provider profile for the detail screen and booking flow.
class ProviderDetail extends Equatable {
  final String id;
  final String businessName;
  final String? description;
  final String? city;
  final String? addressLine;
  final String? logoUrl;
  final String? profileImageUrl;
  final double averageRating;
  final int totalReviews;

  /// How far ahead this salon takes bookings, in days. The date strip offers today plus these; the server
  /// refuses anything later (QA walkthrough 2026-09-22 — "nobody should be able to book more than a week out").
  final int maxAdvanceBookingDays;
  final List<BusinessHour> businessHours;
  final List<ServiceItem> services;
  final List<StaffMember> staff;

  /// Where the salon is, when the catalogue knows: used for the map on the
  /// profile and for handing the point to a navigation app.
  final double? latitude;
  final double? longitude;

  /// The salon's photos, the one it chose first (medium size, for the slider).
  final List<String> images;

  const ProviderDetail({
    required this.id,
    required this.businessName,
    this.description,
    this.city,
    this.addressLine,
    this.logoUrl,
    this.profileImageUrl,
    required this.averageRating,
    required this.totalReviews,
    this.maxAdvanceBookingDays = 7,
    required this.businessHours,
    required this.services,
    required this.staff,
      this.latitude,
    this.longitude,
    this.images = const [],
  });

  /// Staff eligible for booking (active only).
  List<StaffMember> get activeStaff =>
      staff.where((s) => s.isActive).toList();

  @override
  List<Object?> get props => [
        id,
        businessName,
        description,
        city,
        addressLine,
        logoUrl,
        profileImageUrl,
        averageRating,
        totalReviews,
        maxAdvanceBookingDays,
        businessHours,
        services,
        staff,
              latitude,
        longitude,
        images,
      ];
}

class BusinessHour extends Equatable {
  final String dayOfWeek;
  final String? openTime;
  final String? closeTime;
  final bool isClosed;

  /// When the salon shuts mid-day, as `HH:mm` pairs — a customer who turns up
  /// during one finds the door locked, so it belongs on the profile.
  final List<BusinessBreak> breaks;

  const BusinessHour({
    required this.dayOfWeek,
    this.openTime,
    this.closeTime,
    required this.isClosed,
    this.breaks = const [],
  });

  @override
  List<Object?> get props => [dayOfWeek, openTime, closeTime, isClosed, breaks];
}

class ServiceItem extends Equatable {
  final String id;
  final String name;
  final String? description;
  final double price;
  final String currency;
  final int durationMinutes;
  final String? imageUrl;

  const ServiceItem({
    required this.id,
    required this.name,
    this.description,
    required this.price,
    required this.currency,
    required this.durationMinutes,
    this.imageUrl,
  });

  @override
  List<Object?> get props =>
      [id, name, description, price, currency, durationMinutes, imageUrl];
}

class StaffMember extends Equatable {
  final String id;
  final String name;
  final String? role;
  final bool isActive;

  const StaffMember({
    required this.id,
    required this.name,
    this.role,
    required this.isActive,
  });

  @override
  List<Object?> get props => [id, name, role, isActive];
}

/// A bookable time slot for a service on a given day. When the user picks
/// "any staff", [staffId] identifies who the backend assigned to the slot.
class TimeSlot extends Equatable {
  final DateTime startTime;
  final DateTime endTime;
  final int durationMinutes;
  final bool isAvailable;
  final String? staffId;
  final String? staffName;

  const TimeSlot({
    required this.startTime,
    required this.endTime,
    required this.durationMinutes,
    required this.isAvailable,
    this.staffId,
    this.staffName,
  });

  @override
  List<Object?> get props =>
      [startTime, endTime, durationMinutes, isAvailable, staffId, staffName];
}

/// A mid-day closure, as the profile shows it.
class BusinessBreak extends Equatable {
  final String startTime;
  final String endTime;

  const BusinessBreak({required this.startTime, required this.endTime});

  @override
  List<Object?> get props => [startTime, endTime];
}

/// One day's bookable times, with the salon's reason when there are none.
///
/// The server explains an empty day (closed that weekday, the day is shorter than the visit, nobody qualified);
/// showing "no free time for this day" instead leaves the customer guessing (QA walkthrough 2026-09-22).
class DaySlots extends Equatable {
  final List<TimeSlot> slots;
  final String? reason;

  const DaySlots({this.slots = const [], this.reason});

  @override
  List<Object?> get props => [slots, reason];
}
