import 'package:equatable/equatable.dart';

/// Time of day (hours + minutes) — matches the backend working-hours shape.
class ClockTime extends Equatable {
  final int hours;
  final int minutes;
  const ClockTime(this.hours, this.minutes);

  String get label =>
      '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}';

  @override
  List<Object?> get props => [hours, minutes];
}

/// A break within a working day.
class BreakTime extends Equatable {
  final ClockTime start;
  final ClockTime end;
  const BreakTime(this.start, this.end);

  @override
  List<Object?> get props => [start, end];
}

/// Working hours for a single day of week (0=Sunday … 6=Saturday, backend order).
class DayHours extends Equatable {
  final int dayOfWeek;
  final bool isOpen;
  final ClockTime? openTime;
  final ClockTime? closeTime;
  final List<BreakTime> breaks;

  const DayHours({
    required this.dayOfWeek,
    required this.isOpen,
    this.openTime,
    this.closeTime,
    this.breaks = const [],
  });

  DayHours copyWith({
    bool? isOpen,
    ClockTime? openTime,
    ClockTime? closeTime,
    List<BreakTime>? breaks,
  }) {
    return DayHours(
      dayOfWeek: dayOfWeek,
      isOpen: isOpen ?? this.isOpen,
      openTime: openTime ?? this.openTime,
      closeTime: closeTime ?? this.closeTime,
      breaks: breaks ?? this.breaks,
    );
  }

  @override
  List<Object?> get props => [dayOfWeek, isOpen, openTime, closeTime, breaks];
}

enum ServicePriceType {
  fixed('fixed'),
  variable('variable');

  const ServicePriceType(this.wireName);
  final String wireName;
}

/// A service offered by the provider (onboarding draft).
class ServiceDraft extends Equatable {
  final String name;
  final int durationHours;
  final int durationMinutes;
  final double price;
  final ServicePriceType priceType;

  const ServiceDraft({
    required this.name,
    required this.durationHours,
    required this.durationMinutes,
    required this.price,
    this.priceType = ServicePriceType.fixed,
  });

  @override
  List<Object?> get props =>
      [name, durationHours, durationMinutes, price, priceType];
}

/// Business identity captured in step 1.
class BusinessInfo extends Equatable {
  final String businessName;
  final String ownerFirstName;
  final String ownerLastName;
  final String email;
  final String phone;
  final String description;
  final String? logoUrl;

  const BusinessInfo({
    this.businessName = '',
    this.ownerFirstName = '',
    this.ownerLastName = '',
    this.email = '',
    this.phone = '',
    this.description = '',
    this.logoUrl,
  });

  bool get isComplete =>
      businessName.trim().isNotEmpty &&
      ownerFirstName.trim().isNotEmpty &&
      ownerLastName.trim().isNotEmpty &&
      phone.trim().isNotEmpty;

  BusinessInfo copyWith({
    String? businessName,
    String? ownerFirstName,
    String? ownerLastName,
    String? email,
    String? phone,
    String? description,
    String? logoUrl,
  }) {
    return BusinessInfo(
      businessName: businessName ?? this.businessName,
      ownerFirstName: ownerFirstName ?? this.ownerFirstName,
      ownerLastName: ownerLastName ?? this.ownerLastName,
      email: email ?? this.email,
      phone: phone ?? this.phone,
      description: description ?? this.description,
      logoUrl: logoUrl ?? this.logoUrl,
    );
  }

  @override
  List<Object?> get props =>
      [businessName, ownerFirstName, ownerLastName, email, phone, description, logoUrl];
}

/// Address + optional map coordinates captured in step 3.
class OnboardingAddress extends Equatable {
  final String addressLine1;
  final String addressLine2;
  final String city;
  final String province;
  final String postalCode;
  final double? latitude;
  final double? longitude;

  const OnboardingAddress({
    this.addressLine1 = '',
    this.addressLine2 = '',
    this.city = '',
    this.province = '',
    this.postalCode = '',
    this.latitude,
    this.longitude,
  });

  /// City + address required; coordinates optional (backend defaults to 0).
  bool get isComplete =>
      addressLine1.trim().isNotEmpty && city.trim().isNotEmpty;

  OnboardingAddress copyWith({
    String? addressLine1,
    String? addressLine2,
    String? city,
    String? province,
    String? postalCode,
    double? latitude,
    double? longitude,
  }) {
    return OnboardingAddress(
      addressLine1: addressLine1 ?? this.addressLine1,
      addressLine2: addressLine2 ?? this.addressLine2,
      city: city ?? this.city,
      province: province ?? this.province,
      postalCode: postalCode ?? this.postalCode,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
    );
  }

  @override
  List<Object?> get props =>
      [addressLine1, addressLine2, city, province, postalCode, latitude, longitude];
}

/// Aggregate onboarding form state.
class OnboardingData extends Equatable {
  final BusinessInfo businessInfo;
  final String? categoryId;
  final OnboardingAddress address;
  final List<ServiceDraft> services;
  final List<DayHours> businessHours;

  const OnboardingData({
    this.businessInfo = const BusinessInfo(),
    this.categoryId,
    this.address = const OnboardingAddress(),
    this.services = const [],
    this.businessHours = const [],
  });

  OnboardingData copyWith({
    BusinessInfo? businessInfo,
    String? categoryId,
    OnboardingAddress? address,
    List<ServiceDraft>? services,
    List<DayHours>? businessHours,
  }) {
    return OnboardingData(
      businessInfo: businessInfo ?? this.businessInfo,
      categoryId: categoryId ?? this.categoryId,
      address: address ?? this.address,
      services: services ?? this.services,
      businessHours: businessHours ?? this.businessHours,
    );
  }

  @override
  List<Object?> get props =>
      [businessInfo, categoryId, address, services, businessHours];
}

/// Business categories (matches the active Vue CategorySelectionStep set).
class BusinessCategory {
  final String id;
  final String label;
  final String emoji;
  const BusinessCategory(this.id, this.label, this.emoji);

  static const all = <BusinessCategory>[
    BusinessCategory('hair_salon', 'آرایشگاه زنانه', '💇‍♀️'),
    BusinessCategory('barber', 'آرایشگاه مردانه', '💇‍♂️'),
  ];
}
