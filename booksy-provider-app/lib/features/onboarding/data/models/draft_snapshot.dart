import '../../domain/entities/onboarding_data.dart';
import '../../domain/entities/onboarding_draft.dart';

/// Parses GET /v1/Registration/progress into an [OnboardingDraft].
///
/// Shape verified against the live backend. Gotchas encoded here:
/// - `businessInfo.category` echoes the ServiceCategory ENUM NAME (e.g.
///   "HairSalon"), not the id we sent.
/// - `location.addressLine1` is the CONCATENATION of line1 + ", " + line2 (the
///   backend stores a single Street), so line2 comes back folded in.
/// - `services[].priceType` echoes a backend enum ("Standard"), not our
///   "fixed"/"variable".
/// - `businessHours[]` are FLATTENED (openTimeHours/openTimeMinutes/…), arrive
///   UNORDERED, and closed days omit the time fields entirely.
class DraftSnapshot {
  DraftSnapshot._();

  /// Parses the `data` object of the /progress envelope. Null when there is no
  /// draft to resume.
  static OnboardingDraft? fromProgressJson(Map<String, dynamic> json) {
    if (json['hasDraft'] != true) return null;
    final draft = json['draftData'];
    if (draft is! Map) return null;

    final providerId = draft['providerId']?.toString();
    if (providerId == null || providerId.isEmpty) return null;

    final info = _asMap(draft['businessInfo']);
    final location = _asMap(draft['location']);

    return OnboardingDraft(
      providerId: providerId,
      registrationStep:
          _int(draft['registrationStep']) ?? _int(json['currentStep']) ?? 3,
      data: OnboardingData(
        businessInfo: BusinessInfo(
          businessName: _str(info['businessName']),
          ownerFirstName: _str(info['ownerFirstName']),
          ownerLastName: _str(info['ownerLastName']),
          email: _str(info['email']),
          phone: _str(info['phoneNumber']),
          description: _str(info['businessDescription']),
          logoUrl: info['logoUrl']?.toString(),
        ),
        categoryId: BusinessCategory.idFromWireName(info['category']?.toString()),
        address: OnboardingAddress(
          // line2 was folded into the stored street by the backend.
          addressLine1: _str(location['addressLine1']),
          city: _str(location['city']),
          province: _str(location['province']),
          postalCode: _str(location['postalCode']),
          latitude: _double(location['latitude']),
          longitude: _double(location['longitude']),
        ),
        services: _services(draft['services']),
        businessHours: _hours(draft['businessHours']),
      ),
    );
  }

  static List<ServiceDraft> _services(Object? raw) {
    if (raw is! List) return const [];
    return raw.map((e) {
      final m = _asMap(e);
      return ServiceDraft(
        name: _str(m['name']),
        durationHours: _int(m['durationHours']) ?? 0,
        durationMinutes: _int(m['durationMinutes']) ?? 0,
        price: _double(m['price']) ?? 0,
        // The backend echoes its own enum ("Standard"); anything not explicitly
        // variable is treated as a fixed price.
        priceType: _str(m['priceType']).toLowerCase() == 'variable'
            ? ServicePriceType.variable
            : ServicePriceType.fixed,
      );
    }).toList();
  }

  /// Rebuilds all 7 days in order; days absent from the payload are closed.
  static List<DayHours> _hours(Object? raw) {
    final byDay = <int, Map<String, dynamic>>{};
    if (raw is List) {
      for (final e in raw) {
        final m = _asMap(e);
        final day = _int(m['dayOfWeek']);
        if (day != null) byDay[day] = m;
      }
    }
    if (byDay.isEmpty) return const [];

    return List.generate(7, (day) {
      final m = byDay[day];
      if (m == null) return DayHours(dayOfWeek: day, isOpen: false);

      return DayHours(
        dayOfWeek: day,
        isOpen: m['isOpen'] == true,
        openTime: _clock(m['openTimeHours'], m['openTimeMinutes']),
        closeTime: _clock(m['closeTimeHours'], m['closeTimeMinutes']),
        breaks: _breaks(m['breaks']),
      );
    });
  }

  static List<BreakTime> _breaks(Object? raw) {
    if (raw is! List) return const [];
    final result = <BreakTime>[];
    for (final e in raw) {
      final m = _asMap(e);
      final start = _clock(m['startTimeHours'], m['startTimeMinutes']);
      final end = _clock(m['endTimeHours'], m['endTimeMinutes']);
      if (start != null && end != null) result.add(BreakTime(start, end));
    }
    return result;
  }

  static ClockTime? _clock(Object? h, Object? m) {
    final hours = _int(h);
    final minutes = _int(m);
    if (hours == null || minutes == null) return null;
    return ClockTime(hours, minutes);
  }

  static Map<String, dynamic> _asMap(Object? v) {
    if (v is Map<String, dynamic>) return v;
    if (v is Map) return Map<String, dynamic>.from(v);
    return <String, dynamic>{};
  }

  static String _str(Object? v) => v?.toString() ?? '';
  static int? _int(Object? v) => (v as num?)?.toInt();
  static double? _double(Object? v) => (v as num?)?.toDouble();
}
