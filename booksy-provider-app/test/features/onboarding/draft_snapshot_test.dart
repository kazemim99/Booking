import 'dart:convert';

import 'package:booksy_provider_app/features/onboarding/data/models/draft_snapshot.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:flutter_test/flutter_test.dart';

/// The `data` object of a REAL GET /v1/Registration/progress response,
/// captured verbatim from the running backend. It encodes the lossy
/// round-trips we must tolerate:
///   - category echoes the ServiceCategory enum name ("Barbershop")
///   - addressLine1 has line2 folded into it ("…, پلاک ۵")
///   - priceType echoes a backend enum ("Standard"), not "fixed"
///   - businessHours are flattened, UNORDERED, and closed days omit times
const _liveProgress = '''
{
  "hasDraft": true,
  "currentStep": 6,
  "providerId": "e8dd5c60-e062-4ec6-9aae-42610a50587f",
  "draftData": {
    "providerId": "e8dd5c60-e062-4ec6-9aae-42610a50587f",
    "registrationStep": 6,
    "status": "Drafted",
    "businessInfo": {
      "businessName": "سالن بازیابی",
      "businessDescription": "توضیح تست",
      "category": "Barbershop",
      "phoneNumber": "09125550011",
      "email": "draft@b.com",
      "ownerFirstName": "علی",
      "ownerLastName": "رضایی"
    },
    "location": {
      "addressLine1": "خیابان آزادی, پلاک ۵",
      "city": "تهران",
      "province": "تهران",
      "postalCode": "1112223334",
      "latitude": 35.7,
      "longitude": 51.4
    },
    "services": [
      {
        "id": "754d9971-d631-4030-8146-691880a9592b",
        "name": "اصلاح",
        "durationHours": 0,
        "durationMinutes": 45,
        "price": 180000.0,
        "priceType": "Standard"
      }
    ],
    "businessHours": [
      { "dayOfWeek": 5, "isOpen": false, "breaks": [] },
      {
        "dayOfWeek": 0, "isOpen": true,
        "openTimeHours": 10, "openTimeMinutes": 30,
        "closeTimeHours": 19, "closeTimeMinutes": 0,
        "breaks": [
          { "startTimeHours": 14, "startTimeMinutes": 0,
            "endTimeHours": 15, "endTimeMinutes": 0 }
        ]
      },
      { "dayOfWeek": 4, "isOpen": false, "breaks": [] },
      { "dayOfWeek": 3, "isOpen": false, "breaks": [] },
      { "dayOfWeek": 1, "isOpen": false, "breaks": [] },
      { "dayOfWeek": 2, "isOpen": false, "breaks": [] },
      { "dayOfWeek": 6, "isOpen": false, "breaks": [] }
    ],
    "galleryImages": []
  }
}
''';

void main() {
  group('DraftSnapshot.fromProgressJson (live payload)', () {
    late final draft = DraftSnapshot.fromProgressJson(
      jsonDecode(_liveProgress) as Map<String, dynamic>,
    )!;

    test('restores identity and resume step', () {
      expect(draft.providerId, 'e8dd5c60-e062-4ec6-9aae-42610a50587f');
      expect(draft.registrationStep, 6);
      // hours saved (backend step 6) → resume on the gallery step
      expect(draft.resumeStep, 6);
    });

    test('restores business info', () {
      final info = draft.data.businessInfo;
      expect(info.businessName, 'سالن بازیابی');
      expect(info.description, 'توضیح تست');
      expect(info.ownerFirstName, 'علی');
      expect(info.ownerLastName, 'رضایی');
      expect(info.email, 'draft@b.com');
      expect(info.phone, '09125550011');
      expect(info.isComplete, isTrue);
    });

    test('reverse-maps the category enum name back to our id', () {
      expect(draft.data.categoryId, 'barbershop');
    });

    test('restores the address, keeping the folded addressLine1', () {
      final a = draft.data.address;
      expect(a.addressLine1, 'خیابان آزادی, پلاک ۵');
      expect(a.city, 'تهران');
      expect(a.province, 'تهران');
      expect(a.postalCode, '1112223334');
      expect(a.latitude, 35.7);
      expect(a.longitude, 51.4);
      expect(a.isComplete, isTrue);
    });

    test('restores services, coercing the backend priceType enum', () {
      final s = draft.data.services.single;
      expect(s.name, 'اصلاح');
      expect(s.durationMinutes, 45);
      expect(s.price, 180000);
      expect(s.priceType, ServicePriceType.fixed);
    });

    test('restores all 7 days in order, with times and breaks', () {
      final hours = draft.data.businessHours;
      expect(hours, hasLength(7));
      expect(hours.map((d) => d.dayOfWeek), [0, 1, 2, 3, 4, 5, 6]);

      final sunday = hours.first;
      expect(sunday.isOpen, isTrue);
      expect(sunday.openTime, const ClockTime(10, 30));
      expect(sunday.closeTime, const ClockTime(19, 0));
      expect(sunday.breaks.single,
          const BreakTime(ClockTime(14, 0), ClockTime(15, 0)));

      // Closed days omit the time fields entirely.
      final friday = hours[5];
      expect(friday.isOpen, isFalse);
      expect(friday.openTime, isNull);
      expect(friday.closeTime, isNull);
    });
  });

  group('DraftSnapshot edge cases', () {
    test('returns null when there is no draft', () {
      expect(
        DraftSnapshot.fromProgressJson({'hasDraft': false, 'draftData': null}),
        isNull,
      );
    });

    test('returns null when draftData is missing', () {
      expect(DraftSnapshot.fromProgressJson({'hasDraft': true}), isNull);
    });

    test('unknown category (legacy BeautySalon default) → unselected', () {
      final draft = DraftSnapshot.fromProgressJson({
        'hasDraft': true,
        'draftData': {
          'providerId': 'p',
          'registrationStep': 3,
          'businessInfo': {'category': 'BeautySalon'},
        },
      })!;
      expect(draft.data.categoryId, isNull);
      expect(draft.resumeStep, 4);
    });

    test('a draft with no hours yields no hours (cubit keeps its defaults)', () {
      final draft = DraftSnapshot.fromProgressJson({
        'hasDraft': true,
        'draftData': {
          'providerId': 'p',
          'registrationStep': 3,
          'businessHours': <dynamic>[],
        },
      })!;
      expect(draft.data.businessHours, isEmpty);
    });
  });
}
