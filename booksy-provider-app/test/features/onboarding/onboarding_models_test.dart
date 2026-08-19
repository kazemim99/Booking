import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/onboarding/data/models/draft_snapshot.dart';
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

  group('DraftSnapshot.fromProgressJson', () {
    // Trimmed from a live GET /v1/Registration/progress response for a
    // provider whose registration had fully completed (backend step 9,
    // status PendingVerification). Reported bug: the provider app showed the
    // step-1 business-info screen instead of routing to the dashboard.
    Map<String, dynamic> completedResponse({Object? status = 'PendingVerification'}) => {
          'hasDraft': true,
          'currentStep': 9,
          'providerId': 'a8346c06-b292-46cb-8951-d1f8bf711ed2',
          'draftData': {
            'providerId': 'a8346c06-b292-46cb-8951-d1f8bf711ed2',
            'registrationStep': 9,
            'status': ?status,
            'businessInfo': {
              'businessName': 'آرایشگاه نهال',
              'businessDescription': 'آرایشگاه با بیش از 20 سابقه',
              'category': 'Barbershop',
              'phoneNumber': '+989123135143',
              'email': '',
              'ownerFirstName': 'مصطفی',
              'ownerLastName': 'کاظمی',
            },
            'location': {
              'addressLine1': 'کرامت ۳۴',
              'city': 'اسلامشهر',
              'province': 'تهران',
              'postalCode': '',
              'latitude': 35.567001737131,
              'longitude': 51.2468278972986,
            },
            'services': <Object?>[],
            'businessHours': <Object?>[],
          },
        };

    test('parses status alongside registrationStep', () {
      final draft = DraftSnapshot.fromProgressJson(completedResponse());

      expect(draft, isNotNull);
      expect(draft!.registrationStep, 9);
      expect(draft.status, ProviderStatus.pendingVerification);
    });

    test('a fully-completed draft (step 9, PendingVerification) is reported complete', () {
      final draft = DraftSnapshot.fromProgressJson(completedResponse());

      expect(draft!.isFullyComplete, isTrue,
          reason: 'this is the exact payload from the bug report — the provider app must not '
              'resume the wizard on it');
    });

    test('registrationStep 9 alone is complete even if status is missing', () {
      final draft =
          DraftSnapshot.fromProgressJson(completedResponse(status: null));

      expect(draft!.status, isNull);
      expect(draft.isFullyComplete, isTrue);
    });

    test('an unrecognized status string parses to null rather than throwing', () {
      final draft =
          DraftSnapshot.fromProgressJson(completedResponse(status: 'SomeFutureStatus'));

      expect(draft!.status, isNull);
      // registrationStep 9 alone is still enough to know it's done.
      expect(draft.isFullyComplete, isTrue);
    });

    test('a genuinely in-progress draft (step 6, Drafted) is not complete', () {
      final json = {
        'hasDraft': true,
        'draftData': {
          'providerId': 'prov-mid',
          'registrationStep': 6,
          'status': 'Drafted',
          'businessInfo': <String, Object?>{},
          'location': <String, Object?>{},
          'services': <Object?>[],
          'businessHours': <Object?>[],
        },
      };

      final draft = DraftSnapshot.fromProgressJson(json);

      expect(draft!.isFullyComplete, isFalse);
      expect(draft.resumeStep, 6); // gallery saved → preview... (existing switch)
    });

    test('no draft on the server parses to null', () {
      expect(DraftSnapshot.fromProgressJson({'hasDraft': false}), isNull);
    });
  });
}
