import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
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
    registerFallbackValue(<GalleryImageUpload>[]);
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

    // Regression: a resumed draft whose registration had already fully
    // completed (backend step 9 / status advanced past Drafted — reachable
    // when the router lands here on a stale cached JWT that still says
    // "Drafted") used to fall through OnboardingDraft.resumeStep's switch to
    // its `default: 1`, dumping the provider back on the business-info screen
    // despite being fully registered and pending admin verification.
    test(
      'a draft already past registration (status PendingVerification) jumps '
      'straight to the completion screen instead of business-info step 1',
      () async {
        when(() => repo.getDraft()).thenAnswer(
          (_) async => const Right(
            OnboardingDraft(
              providerId: 'prov-done',
              registrationStep: 9,
              status: ProviderStatus.pendingVerification,
              data: OnboardingData(
                businessInfo: BusinessInfo(businessName: 'آرایشگاه نهال'),
              ),
            ),
          ),
        );

        final cubit = build();
        await cubit.init(phoneNumber: '09123135143');

        expect(cubit.state.draftProviderId, 'prov-done');
        expect(cubit.state.step, OnboardingState.totalSteps);
        expect(cubit.state.phase, OnboardingPhase.completed);
        expect(cubit.state.isCompleted, isTrue);
      },
    );

    // Pins the case that actually reproduced the bug report: the backend can
    // omit `status` on some payload shapes, so registrationStep alone (>= 9,
    // one past this wizard's own 8-step numbering) must be sufficient.
    test(
      'registrationStep 9 alone (no status field) is still treated as complete',
      () async {
        when(() => repo.getDraft()).thenAnswer(
          (_) async => const Right(
            OnboardingDraft(
              providerId: 'prov-done-2',
              registrationStep: 9,
              data: OnboardingData(),
            ),
          ),
        );

        final cubit = build();
        await cubit.init(phoneNumber: '09123135143');

        expect(cubit.state.step, OnboardingState.totalSteps);
        expect(cubit.state.phase, OnboardingPhase.completed);
      },
    );

    test(
      'a mid-wizard draft (Drafted status, step 6) still resumes normally',
      () async {
        when(() => repo.getDraft()).thenAnswer(
          (_) async => const Right(
            OnboardingDraft(
              providerId: 'prov-mid',
              registrationStep: 6,
              status: ProviderStatus.drafted,
              data: OnboardingData(
                businessInfo: BusinessInfo(businessName: 'سالن ب'),
              ),
            ),
          ),
        );

        final cubit = build();
        await cubit.init(phoneNumber: '09121234567');

        expect(cubit.state.step, 6);
        expect(cubit.state.phase, OnboardingPhase.editing);
        expect(cubit.state.data.businessInfo.businessName, 'سالن ب');
      },
    );
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

    blocTest<OnboardingCubit, OnboardingState>(
      'step 5 with all days closed → error',
      build: build,
      seed: () => OnboardingState(
        step: 5,
        draftProviderId: 'p1',
        data: OnboardingData(
          businessHours:
              List.generate(7, (i) => DayHours(dayOfWeek: i, isOpen: false)),
        ),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 5);
        verifyNever(() => repo.saveWorkingHours(any(), any()));
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 5 with close before open → error',
      build: build,
      seed: () => const OnboardingState(
        step: 5,
        draftProviderId: 'p1',
        data: OnboardingData(
          businessHours: [
            DayHours(
              dayOfWeek: 0,
              isOpen: true,
              openTime: ClockTime(18, 0),
              closeTime: ClockTime(9, 0),
            ),
          ],
        ),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 5);
        verifyNever(() => repo.saveWorkingHours(any(), any()));
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 5 with a break outside business hours → error',
      build: build,
      seed: () => const OnboardingState(
        step: 5,
        draftProviderId: 'p1',
        data: OnboardingData(
          businessHours: [
            DayHours(
              dayOfWeek: 0,
              isOpen: true,
              openTime: ClockTime(9, 0),
              closeTime: ClockTime(18, 0),
              // 19:00–20:00 is entirely after closing.
              breaks: [BreakTime(ClockTime(19, 0), ClockTime(20, 0))],
            ),
          ],
        ),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 5);
        verifyNever(() => repo.saveWorkingHours(any(), any()));
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 5 with a valid in-hours break → saves and advances',
      setUp: () => when(() => repo.saveWorkingHours(any(), any()))
          .thenAnswer((_) async => const Right(null)),
      build: build,
      seed: () => const OnboardingState(
        step: 5,
        draftProviderId: 'p1',
        data: OnboardingData(
          businessHours: [
            DayHours(
              dayOfWeek: 0,
              isOpen: true,
              openTime: ClockTime(9, 0),
              closeTime: ClockTime(18, 0),
              breaks: [BreakTime(ClockTime(13, 0), ClockTime(14, 0))],
            ),
          ],
        ),
      ),
      act: (c) => c.next(),
      verify: (c) {
        expect(c.state.step, 6);
        verify(() => repo.saveWorkingHours('p1', any())).called(1);
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
          businessHours: [
            DayHours(
              dayOfWeek: 0,
              isOpen: true,
              openTime: ClockTime(9, 0),
              closeTime: ClockTime(18, 0),
            ),
          ],
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
      'step 6 gallery: no images → advances without uploading',
      build: build,
      seed: () => const OnboardingState(step: 6, draftProviderId: 'p1'),
      act: (c) => c.uploadGalleryAndAdvance(const []),
      verify: (c) {
        expect(c.state.step, 7);
        verifyNever(() => repo.uploadGallery(any(), any()));
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 6 gallery: with images → uploads then advances',
      setUp: () => when(() => repo.uploadGallery(any(), any()))
          .thenAnswer((_) async => const Right(null)),
      build: build,
      seed: () => const OnboardingState(step: 6, draftProviderId: 'p1'),
      act: (c) => c.uploadGalleryAndAdvance(
        const [GalleryImageUpload(name: 'a.jpg', bytes: [1, 2, 3])],
      ),
      verify: (c) {
        expect(c.state.step, 7);
        verify(() => repo.uploadGallery('p1', any())).called(1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'step 6 gallery: upload failure → error, stays on step 6',
      setUp: () => when(() => repo.uploadGallery(any(), any())).thenAnswer(
          (_) async => const Left(ServerFailure('boom'))),
      build: build,
      seed: () => const OnboardingState(step: 6, draftProviderId: 'p1'),
      act: (c) => c.uploadGalleryAndAdvance(
        const [GalleryImageUpload(name: 'a.jpg', bytes: [1])],
      ),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 6);
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
    void stubComplete() {
      when(() => repo.complete(any()))
          .thenAnswer((_) async => const Right(null));
      when(() => repo.setOwnerProvidesServices(any()))
          .thenAnswer((_) async => const Right(null));
    }

    blocTest<OnboardingCubit, OnboardingState>(
      'completes registration, submits the provides-services answer, lands on step 8',
      setUp: stubComplete,
      build: build,
      seed: () => const OnboardingState(step: 7, draftProviderId: 'p1'),
      act: (c) => c.complete(),
      verify: (c) {
        expect(c.state.step, 8);
        expect(c.state.isCompleted, isTrue);
        verify(() => repo.complete('p1')).called(1);
        // Defaults to yes → owner becomes the first active staff member (S1).
        verify(() => repo.setOwnerProvidesServices(true)).called(1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'submits provides-services = false when the owner only manages (S2)',
      setUp: stubComplete,
      build: build,
      seed: () => const OnboardingState(step: 7, draftProviderId: 'p1'),
      act: (c) async {
        c.setOwnerProvidesServices(false);
        await c.complete();
      },
      verify: (c) {
        expect(c.state.step, 8);
        verify(() => repo.setOwnerProvidesServices(false)).called(1);
      },
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'owner-provides-services defaults to true and toggles',
      build: build,
      act: (c) {
        c.setOwnerProvidesServices(false);
        c.setOwnerProvidesServices(true);
      },
      verify: (c) => expect(c.state.data.ownerProvidesServices, isTrue),
    );

    blocTest<OnboardingCubit, OnboardingState>(
      'a completion failure never blocks on the provides-services call',
      setUp: () => when(() => repo.complete(any()))
          .thenAnswer((_) async => const Left(ServerFailure('boom'))),
      build: build,
      seed: () => const OnboardingState(step: 7, draftProviderId: 'p1'),
      act: (c) => c.complete(),
      verify: (c) {
        expect(c.state.phase, OnboardingPhase.error);
        expect(c.state.step, 7);
        verifyNever(() => repo.setOwnerProvidesServices(any()));
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
