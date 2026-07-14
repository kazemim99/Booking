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
  final List<BusinessHour> businessHours;
  final List<ServiceItem> services;
  final List<StaffMember> staff;

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
    required this.businessHours,
    required this.services,
    required this.staff,
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
        businessHours,
        services,
        staff,
      ];
}

class BusinessHour extends Equatable {
  final String dayOfWeek;
  final String? openTime;
  final String? closeTime;
  final bool isClosed;

  const BusinessHour({
    required this.dayOfWeek,
    this.openTime,
    this.closeTime,
    required this.isClosed,
  });

  @override
  List<Object?> get props => [dayOfWeek, openTime, closeTime, isClosed];
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
