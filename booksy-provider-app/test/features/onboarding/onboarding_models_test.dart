import 'package:booksy_provider_app/features/onboarding/data/models/onboarding_models.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('RegisterOrganizationRequest (draft creation)', () {
    test('sends the raw category id and defaults coordinates to 0', () {
      const data = OnboardingData(
        businessInfo: BusinessInfo(
          businessName: 'سالن الف',
          ownerFirstName: 'رضا',
          ownerLastName: 'محمدی',
          phone: '09121234567',
          email: 'a@b.com',
          description: 'توضیح',
        ),
        categoryId: 'hair_salon',
        address: OnboardingAddress(
          addressLine1: 'خیابان ولیعصر',
          city: 'تهران',
          province: 'تهران',
          postalCode: '1234567890',
        ),
      );

      final json = const RegisterOrganizationRequest(data).toJson();

      expect(json['businessName'], 'سالن الف');
      expect(json['ownerFirstName'], 'رضا');
      expect(json['ownerLastName'], 'محمدی');
      expect(json['phoneNumber'], '09121234567');
      // Category is sent as the raw id string (parity with Vue).
      expect(json['category'], 'hair_salon');
      expect(json['addressLine1'], 'خیابان ولیعصر');
      expect(json['city'], 'تهران');
      // Map is optional — backend defaults lat/lng to 0.
      expect(json['latitude'], 0);
      expect(json['longitude'], 0);
      // Empty optional not sent.
      expect(json.containsKey('addressLine2'), isFalse);
      expect(json.containsKey('logoUrl'), isFalse);
    });

    test('includes coordinates when a map pin is set', () {
      const data = OnboardingData(
        address: OnboardingAddress(
          addressLine1: 'x',
          city: 'y',
          latitude: 35.7,
          longitude: 51.4,
        ),
      );
      final json = const RegisterOrganizationRequest(data).toJson();
      expect(json['latitude'], 35.7);
      expect(json['longitude'], 51.4);
    });
  });

  group('SaveServicesRequest', () {
    test('maps services to the backend shape', () {
      final json = const SaveServicesRequest('p1', [
        ServiceDraft(
          name: 'کوتاهی مو',
          durationHours: 1,
          durationMinutes: 30,
          price: 250000,
        ),
      ]).toJson();

      expect(json['providerId'], 'p1');
      final services = json['services'] as List;
      expect(services.single, {
        'name': 'کوتاهی مو',
        'durationHours': 1,
        'durationMinutes': 30,
        'price': 250000.0,
        'priceType': 'fixed',
      });
    });
  });

  group('SaveWorkingHoursRequest', () {
    test('maps open and closed days', () {
      final json = const SaveWorkingHoursRequest('p1', [
        DayHours(
          dayOfWeek: 0,
          isOpen: true,
          openTime: ClockTime(9, 0),
          closeTime: ClockTime(18, 30),
          breaks: [BreakTime(ClockTime(14, 0), ClockTime(15, 0))],
        ),
        DayHours(dayOfWeek: 5, isOpen: false),
      ]).toJson();

      final hours = json['businessHours'] as List;
      final open = hours.first as Map;
      expect(open['dayOfWeek'], 0);
      expect(open['isOpen'], isTrue);
      expect(open['openTime'], {'hours': 9, 'minutes': 0});
      expect(open['closeTime'], {'hours': 18, 'minutes': 30});
      expect((open['breaks'] as List).single, {
        'start': {'hours': 14, 'minutes': 0},
        'end': {'hours': 15, 'minutes': 0},
      });

      final closed = hours.last as Map;
      expect(closed['isOpen'], isFalse);
      expect(closed['openTime'], isNull);
      expect(closed['closeTime'], isNull);
    });
  });
}
