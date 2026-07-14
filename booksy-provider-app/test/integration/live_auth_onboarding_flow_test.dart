@Tags(['live'])
library;

import 'package:booksy_provider_app/core/storage/secure_storage_service.dart';
import 'package:booksy_provider_app/features/auth/data/datasources/auth_api_service.dart';
import 'package:booksy_provider_app/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/auth/domain/usecases/complete_provider_authentication_usecase.dart';
import 'package:booksy_provider_app/features/auth/domain/usecases/send_verification_code_usecase.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:booksy_provider_app/features/onboarding/data/datasources/onboarding_api_service.dart';
import 'package:booksy_provider_app/features/onboarding/data/repositories/onboarding_repository_impl.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_state.dart';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

/// LIVE end-to-end test: drives the real data sources / repositories / Bloc /
/// Cubit against a RUNNING backend, verifying the full provider journey:
///
///   login (OTP) → NeedsOnboarding → wizard (draft → services → hours →
///   complete) → provider-status refresh → Authenticated (dashboard)
///
/// Requires the backend on http://localhost:5000 started with
/// OTP_SANDBOX_CODE=123456. Excluded from the default run:
///
///   flutter test --tags live          # run it
///   flutter test --exclude-tags live  # default CI (no backend needed)
const String _baseUrl = 'http://localhost:5000/api';
const String _sandboxOtp = '123456';

/// In-memory FlutterSecureStorage (no platform channels under `flutter test`).
class _FakeSecureStorage extends Mock implements FlutterSecureStorage {
  final Map<String, String?> store = {};
}

void main() {
  late Dio dio;
  late SecureStorageService storage;
  late AuthRepositoryImpl authRepo;
  late OnboardingRepositoryImpl onboardingRepo;

  // A fresh phone per run keeps the provider new (isNewProvider == true).
  final phone = '0912${DateTime.now().millisecondsSinceEpoch % 10000000}'
      .padRight(11, '0')
      .substring(0, 11);

  setUpAll(() {
    dio = Dio(BaseOptions(
      baseUrl: _baseUrl,
      headers: const {'Content-Type': 'application/json'},
      // Don't throw on 4xx — repositories inspect the envelope/status.
      validateStatus: (code) => code != null && code < 500,
    ));

    final fake = _FakeSecureStorage();
    when(() => fake.write(key: any(named: 'key'), value: any(named: 'value')))
        .thenAnswer((i) async {
      fake.store[i.namedArguments[#key] as String] =
          i.namedArguments[#value] as String?;
    });
    when(() => fake.read(key: any(named: 'key')))
        .thenAnswer((i) async => fake.store[i.namedArguments[#key] as String]);
    when(() => fake.delete(key: any(named: 'key'))).thenAnswer((i) async {
      fake.store.remove(i.namedArguments[#key] as String);
    });

    storage = SecureStorageService(fake);
    authRepo = AuthRepositoryImpl(AuthApiService(dio, dio), storage);
    onboardingRepo = OnboardingRepositoryImpl(OnboardingApiService(dio));
  });

  test('backend is reachable', () async {
    final res = await Dio().get(
      'http://localhost:5000/health',
      options: Options(validateStatus: (_) => true),
    );
    expect(res.statusCode, 200,
        reason: 'Start the backend with OTP_SANDBOX_CODE=123456');
  });

  test('full journey: OTP login → onboarding → dashboard', () async {
    // ---------- 1. Authenticate (send + verify) ----------
    final authBloc = AuthBloc(
      SendVerificationCodeUseCase(authRepo),
      CompleteProviderAuthenticationUseCase(authRepo),
      authRepo,
    );

    authBloc.add(SendVerificationCodeRequested(phone));
    await expectLater(
      authBloc.stream,
      emitsInOrder([isA<AuthLoading>(), isA<OtpSent>()]),
    );

    authBloc.add(VerifyCodeRequested(phoneNumber: phone, code: _sandboxOtp));
    await expectLater(
      authBloc.stream,
      emitsInOrder([
        isA<AuthLoading>(),
        // Brand-new provider has no ServiceCatalog profile → onboarding.
        isA<NeedsOnboarding>(),
      ]),
    );

    // Tokens were persisted, so subsequent calls are authenticated.
    final token = await storage.getAccessToken();
    expect(token, isNotNull);
    expect(token, isNotEmpty);
    dio.options.headers['Authorization'] = 'Bearer $token';

    // ---------- 2. Onboarding wizard ----------
    final cubit = OnboardingCubit(onboardingRepo);
    await cubit.init(phoneNumber: phone);
    expect(cubit.state.step, 1);
    expect(cubit.state.data.businessHours, hasLength(7));

    // Step 1 — business info
    cubit.updateBusinessInfo(BusinessInfo(
      businessName: 'سالن یکپارچه',
      ownerFirstName: 'رضا',
      ownerLastName: 'محمدی',
      phone: phone,
      email: 'e2e@booksy.test',
      description: 'تست یکپارچه',
    ));
    await cubit.next();
    expect(cubit.state.step, 2);

    // Step 2 — category
    cubit.selectCategory('hair_salon');
    await cubit.next();
    expect(cubit.state.step, 3);

    // Step 3 — location → creates the draft on the server
    cubit.updateAddress(const OnboardingAddress(
      addressLine1: 'خیابان ولیعصر',
      city: 'تهران',
      province: 'تهران',
      postalCode: '1234567890',
    ));
    await cubit.next();
    expect(cubit.state.phase, OnboardingPhase.editing,
        reason: 'draft creation failed: ${cubit.state.errorMessage}');
    expect(cubit.state.draftProviderId, isNotNull);
    expect(cubit.state.step, 4);

    // Step 4 — services
    cubit.setServices(const [
      ServiceDraft(
        name: 'کوتاهی مو',
        durationHours: 0,
        durationMinutes: 30,
        price: 250000,
      ),
    ]);
    await cubit.next();
    expect(cubit.state.phase, OnboardingPhase.editing,
        reason: 'services save failed: ${cubit.state.errorMessage}');
    expect(cubit.state.step, 5);

    // Step 5 — working hours (defaults are all open 09:00–18:00)
    await cubit.next();
    expect(cubit.state.phase, OnboardingPhase.editing,
        reason: 'hours save failed: ${cubit.state.errorMessage}');
    expect(cubit.state.step, 6);

    // Step 6 — gallery (optional, skipped)
    await cubit.next();
    expect(cubit.state.step, 7);

    // Step 7 — confirm → complete registration
    await cubit.complete();
    expect(cubit.state.phase, OnboardingPhase.completed,
        reason: 'complete failed: ${cubit.state.errorMessage}');
    expect(cubit.state.step, 8);

    // ---------- 3. Post-onboarding: status refresh → dashboard ----------
    // The cached JWT still carries the stale (empty/Drafted) status, so the app
    // re-fetches it from the server before routing.
    authBloc.add(const ProviderStatusRefreshRequested());
    await expectLater(
      authBloc.stream,
      emitsInOrder([isA<AuthLoading>(), isA<Authenticated>()]),
    );

    final session = (authBloc.state as Authenticated).session;
    expect(session.providerId, isNotNull);
    expect(session.providerStatus, ProviderStatus.pendingVerification);
    // The whole point: the provider is now cleared for the dashboard.
    expect(session.needsOnboarding, isFalse);
    expect(session.isBlocked, isFalse);

    await authBloc.close();
    await cubit.close();
  }, timeout: const Timeout(Duration(minutes: 2)));
}
