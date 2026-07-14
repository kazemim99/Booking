import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_draft.dart';
import 'package:booksy_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_state.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

const _businessInfo = BusinessInfo(
  businessName: 'سالن الف',
  ownerFirstName: 'رضا',
  ownerLastName: 'محمدی',
  phone: '09121234567',
  // Required by the backend validator.
  description: 'توضیح کسب‌و‌کار',
);

const _address = OnboardingAddress(
  addressLine1: 'خیابان ولیعصر',
  city: 'تهران',
  province: 'تهران', // required by the backend validator
);

const _service = ServiceDraft(
  name: 'کوتاهی مو',
  durationHours: 0,
  durationMinutes: 30,
  price: 200000,
);

void main() {
  late _MockRepo repo;

  setUpAll(() {
    registerFallbackValue(const OnboardingData());
    registerFallbackValue(<ServiceDraft>[]);
    registerFallbackValue(<DayHours>[]);
  });

  setUp(() {
    repo = _MockRepo();
    when(() => repo.getDraft()).thenAnswer((_) async => const Right(null));
  });

  OnboardingCubit build() => OnboardingCubit(repo);

  group('init', () {
    test('seeds default working hours and pre-fills the phone', () async {
      final cubit = build();
      await cubit.init(phoneNumber: '09121234567');

      expect(cubit.state.step, 1);
      expect(cubit.state.data.businessInfo.phone, '09121234567');
      expect(cubit.state.data.businessHours.length, 7);
      expect(cubit.state.data.businessHours.every((d) => d.isOpen), isTrue);
    });

    test('rehydrates every saved field and resumes at the right step', () async {
      when(() => repo.getDraft()).thenAnswer(
        (_) async => Right(
          OnboardingDraft(
            providerId: 'draft-1',
            registrationStep: 6, // hours saved → resume on gallery (step 6)
            data: const OnboardingData(
              businessInfo: BusinessInfo(
                businessName: 'سالن بازیابی',
                ownerFirstName: 'علی',
                ownerLastName: 'رضایی',
                email: 'draft@b.com',
                phone: '09125550011',
                description: 'توضیح تست',
              ),
              categoryId: 'barbershop',
              address: OnboardingAddress(
                addressLine1: 'خیابان آزادی, پلاک ۵',
                city: 'تهران',
                province: 'تهران',
                postalCode: '1112223334',
                latitude: 35.7,
                longitude: 51.4,
              ),
              services: [
                ServiceDraft(
                  name: 'اصلاح',
                  durationHours: 0,
                  durationMinutes: 45,
                  price: 180000,
                ),
              ],
              businessHours: [
                DayHours(
                  dayOfWeek: 0,
                  isOpen: true,
                  openTime: ClockTime(10, 30),
                  closeTime: ClockTime(19, 0),
                ),
              ],
            ),
          ),
        ),
      );

      final cubit = build();
      await cubit.init(phoneNumber: '09125550011');

      expect(cubit.state.draftProviderId, 'draft-1');
      expect(cubit.state.step, 6);

      final data = cubit.state.data;
      expect(data.businessInfo.businessName, 'سالن بازیابی');
      expect(data.businessInfo.ownerFirstName, 'علی');
      expect(data.businessInfo.email, 'draft@b.com');
      expect(data.categoryId, 'barbershop');
      expect(data.address.city, 'تهران');
      expect(data.address.latitude, 35.7);
      expect(data.services.single.name, 'اصلاح');
      expect(data.businessHours.single.openTime, const ClockTime(10, 30));
    });

    test('keeps default hours when the draft was saved before the hours step',
        () async {
      when(() => repo.getDraft()).thenAnswer(
        (_) async => const Right(
          OnboardingDraft(
            providerId: 'draft-2',
            registrationStep: 3, // only location saved
            data: OnboardingData(
              businessInfo: BusinessInfo(businessName: 'س'),
            ),
          ),
        ),
      );

      final cubit = build();
      await cubit.init(phoneNumber: '09121234567');

      expect(cubit.state.step, 4); // location saved → resume on services
      expect(cubit.state.data.businessHours, hasLength(7));
      // Phone falls back to the authenticated number when the draft has none.
      expect(cubit.state.data.businessInfo.phone, '09121234567');
    });

    test('a failed progress lookup does not block a fresh registration',
        () async {
      when(() => repo.getDraft())
          .thenAnswer((_) async => const Left(ServerFailure('boom')));
      final cubit = build();
      await cubit.init(phoneNumber: '09121234567');

      expect(cubit.state.step, 1);
      expect(cubit.state.draftProviderId, isNull);
      expect(cubit.state.data.businessHours, hasLength(7));
    });
  });

  group('step validation', () {
    blocTest<OnboardingCubit, OnboardingState>(
      'step 1 incomplete → error, stays on step 1',
      build: build,
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 1 complete → advances to step 2',
      build: build,
      act: (c) async {
        c.updateBusinessInfo(_businessInfo);
        await c.next();
      },
      verify: (c) => expect(c.state.step, 2),
    );

    // The backend validator requires BusinessDescription and Province
    // (.NotEmpty). Gate on them client-side rather than letting the draft
    // creation fail with a server 400 three steps later.
    blocTest<OnboardingCubit, OnboardingState>(
      'step 1 without a description → error (backend requires it)',
      build: build,
      act: (c) async {
        c.updateBusinessInfo(const BusinessInfo(
          businessName: 'س',
          ownerFirstName: 'ر',
          ownerLastName: 'م',
          phone: '09121234567',
          description: '',
        ));
        await c.next();
      },
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 3 without a province → error (backend requires it)',
      build: build,
      seed: () => const OnboardingState(step: 3),
      act: (c) async {
        c.updateAddress(const OnboardingAddress(
          addressLine1: 'خیابان ولیعصر',
          city: 'تهران',
          province: '',
        ));
        await c.next();
      },
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 3);
        verifyNever(() => repo.createDraft(any()));
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 2 without a category → error',
      build: build,
      seed: () => const OnboardingState(step: 2),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 2);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 4 with no services → error',
      build: build,
      seed: () => const OnboardingState(step: 4, draftProviderId: 'p1'),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 4);
      },
    );
  });

  group('step 3 → creates the draft', () {
    blocTest<OnboardingCubit, OnboardingState>(
      'stores providerId and advances to step 4',
      setUp: () => when(() => repo.createDraft(any()))
          .thenAnswer((_) async => const Right('prov-9')),
      build: build,
      seed: () => const OnboardingState(
        step: 3,
        data: OnboardingData(
          businessInfo: _businessInfo,
          categoryId: 'hair_salon',
          address: _address,
        ),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.draftProviderId, 'prov-9');
        expect(c.state.step, 4);
        verify(() => repo.createDraft(any())).called(1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'server failure → error, stays on step 3',
      setUp: () => when(() => repo.createDraft(any()))
          .thenAnswer((_) async => const Left(ServerFailure('boom'))),
      build: build,
      seed: () => const OnboardingState(
        step: 3,
        data: OnboardingData(
          businessInfo: _businessInfo,
          categoryId: 'hair_salon',
          address: _address,
        ),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.errorMessage, 'boom');
        expect(c.state.step, 3);
      },
    );
  });

  group('incremental saves', () {
    blocTest<OnboardingCubit, OnboardingState>(
      'step 4 saves services and advances',
      setUp: () => when(() => repo.saveServices(any(), any()))
          .thenAnswer((_) async => const Right(null)),
      build: build,
      seed: () => const OnboardingState(
        step: 4,
        draftProviderId: 'p1',
        data: OnboardingData(services: [_service]),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.step, 5);
        verify(() => repo.saveServices('p1', [_service])).called(1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 5 saves working hours and advances',
      setUp: () => when(() => repo.saveWorkingHours(any(), any()))
          .thenAnswer((_) async => const Right(null)),
      build: build,
      seed: () => const OnboardingState(
        step: 5,
        draftProviderId: 'p1',
        data: OnboardingData(
          businessHours: [DayHours(dayOfWeek: 0, isOpen: true)],
        ),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.step, 6);
        verify(() => repo.saveWorkingHours('p1', any())).called(1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 6 (gallery) is skippable with no backend call',
      build: build,
      seed: () => const OnboardingState(step: 6, draftProviderId: 'p1'),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.step, 7);
        verifyNever(() => repo.saveServices(any(), any()));
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'a save without a draft id errors instead of calling the API',
      build: build,
      seed: () => const OnboardingState(
        step: 4,
        data: OnboardingData(services: [_service]),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        verifyNever(() => repo.saveServices(any(), any()));
      },
    );
  });

  group('complete', () {
    blocTest<OnboardingCubit, OnboardingState>(
      'completes registration and lands on step 8',
      setUp: () => when(() => repo.complete(any()))
          .thenAnswer((_) async => const Right(null)),
      build: build,
      seed: () => const OnboardingState(step: 7, draftProviderId: 'p1'),
      act: (c) => c.complete(),
      verify: (c) {
        expect(c.state.step, 8);
        expect(c.state.isCompleted, isTrue);
        verify(() => repo.complete('p1')).called(1);
      },
    );
  });

  group('navigation', () {
    blocTest<OnboardingCubit, OnboardingState>(
      'back moves to the previous step',
      build: build,
      seed: () => const OnboardingState(step: 3),
      act: (c) => c.back(),
      verify: (c) => expect(c.state.step, 2),
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'goToStep jumps (preview edit)',
      build: build,
      seed: () => const OnboardingState(step: 7),
      act: (c) => c.goToStep(2),
      verify: (c) => expect(c.state.step, 2),
    );
  });
}
