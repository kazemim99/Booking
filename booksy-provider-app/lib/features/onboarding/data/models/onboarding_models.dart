import '../../domain/entities/onboarding_data.dart';

/// Draft-creation request → POST /v1/Providers/organizations.
/// Mirrors Vue RegisterOrganizationRequest. `category` is the raw category id
/// string (e.g. "hair_salon"); coordinates default to 0 when the map is unused.
class RegisterOrganizationRequest {
  final OnboardingData data;
  const RegisterOrganizationRequest(this.data);

  Map<String, dynamic> toJson() {
    final b = data.businessInfo;
    final a = data.address;
    return {
      'businessName': b.businessName,
      'businessDescription': b.description,
      'category': data.categoryId ?? '',
      'phoneNumber': b.phone,
      'email': b.email,
      'addressLine1': a.addressLine1,
      if (a.addressLine2.isNotEmpty) 'addressLine2': a.addressLine2,
      'city': a.city,
      'province': a.province,
      'postalCode': a.postalCode,
      'latitude': a.latitude ?? 0,
      'longitude': a.longitude ?? 0,
      'ownerFirstName': b.ownerFirstName,
      'ownerLastName': b.ownerLastName,
      if (b.logoUrl != null) 'logoUrl': b.logoUrl,
    };
  }
}

/// Step-4 services request.
class SaveServicesRequest {
  final String providerId;
  final List<ServiceDraft> services;
  const SaveServicesRequest(this.providerId, this.services);

  Map<String, dynamic> toJson() => {
        'providerId': providerId,
        'services': services
            .map((s) => {
                  'name': s.name,
                  'durationHours': s.durationHours,
                  'durationMinutes': s.durationMinutes,
                  'price': s.price,
                  'priceType': s.priceType.wireName,
                })
            .toList(),
      };
}

/// Step-6 working-hours request.
class SaveWorkingHoursRequest {
  final String providerId;
  final List<DayHours> businessHours;
  const SaveWorkingHoursRequest(this.providerId, this.businessHours);

  Map<String, dynamic> _time(ClockTime? t) =>
      t == null ? {} : {'hours': t.hours, 'minutes': t.minutes};

  Map<String, dynamic> toJson() => {
        'providerId': providerId,
        'businessHours': businessHours
            .map((d) => {
                  'dayOfWeek': d.dayOfWeek,
                  'isOpen': d.isOpen,
                  'openTime': d.openTime == null ? null : _time(d.openTime),
                  'closeTime': d.closeTime == null ? null : _time(d.closeTime),
                  'breaks': d.breaks
                      .map((br) => {
                            'start': _time(br.start),
                            'end': _time(br.end),
                          })
                      .toList(),
                })
            .toList(),
      };
}
