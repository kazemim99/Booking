import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:booksy_provider_app/features/auth/domain/usecases/complete_provider_authentication_usecase.dart';
import 'package:booksy_provider_app/features/auth/domain/usecases/send_verification_code_usecase.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockSend extends Mock implements SendVerificationCodeUseCase {}

class _MockComplete extends Mock
    implements CompleteProviderAuthenticationUseCase {}

class _MockRepo extends Mock implements AuthRepository {}

ProviderSession _session({
  String? providerId,
  ProviderStatus? status,
  bool isNew = false,
}) {
  return ProviderSession(
    accessToken: 'a',
    refreshToken: 'r',
    expiresIn: 86400,
    user: const ProviderUser(id: 'u', phoneNumber: '09121234567', fullName: 'X'),
    providerId: providerId,
    providerStatus: status,
    isNewProvider: isNew,
    requiresOnboarding: false,
  );
}

void main() {
  late _MockSend send;
  late _MockComplete complete;
  late _MockRepo repo;

  AuthBloc build() => AuthBloc(send, complete, repo);

  setUp(() {
    send = _MockSend();
    complete = _MockComplete();
    repo = _MockRepo();
  });

  group('SendVerificationCodeRequested', () {
    blocTest<AuthBloc, AuthState>(
      'emits [Loading, OtpSent] on success',
      setUp: () => when(() => send(phoneNumber: any(named: 'phoneNumber')))
          .thenAnswer((_) async => const Right('sent')),
      build: build,
      act: (b) => b.add(const SendVerificationCodeRequested('09121234567')),
      expect: () => [
        const AuthLoading(),
        const OtpSent(phoneNumber: '09121234567', message: 'sent'),
      ],
    );

    blocTest<AuthBloc, AuthState>(
      'emits [Loading, AuthError] on failure',
      setUp: () => when(() => send(phoneNumber: any(named: 'phoneNumber')))
          .thenAnswer((_) async => const Left(NetworkFailure('offline'))),
      build: build,
      act: (b) => b.add(const SendVerificationCodeRequested('09121234567')),
      expect: () => [const AuthLoading(), const AuthError('offline')],
    );
  });

  group('VerifyCodeRequested → status-based resolution', () {
    blocTest<AuthBloc, AuthState>(
      'active provider → Authenticated',
      setUp: () => when(() => complete(
            phoneNumber: any(named: 'phoneNumber'),
            code: any(named: 'code'),
            firstName: any(named: 'firstName'),
            lastName: any(named: 'lastName'),
            email: any(named: 'email'),
          )).thenAnswer((_) async =>
          Right(_session(providerId: 'p', status: ProviderStatus.active))),
      build: build,
      act: (b) => b.add(
          const VerifyCodeRequested(phoneNumber: '09121234567', code: '123456')),
      expect: () => [const AuthLoading(), isA<Authenticated>()],
    );

    blocTest<AuthBloc, AuthState>(
      'new provider → NeedsOnboarding',
      setUp: () => when(() => complete(
            phoneNumber: any(named: 'phoneNumber'),
            code: any(named: 'code'),
            firstName: any(named: 'firstName'),
            lastName: any(named: 'lastName'),
            email: any(named: 'email'),
          )).thenAnswer((_) async => Right(_session(isNew: true))),
      build: build,
      act: (b) => b.add(
          const VerifyCodeRequested(phoneNumber: '09121234567', code: '123456')),
      expect: () => [const AuthLoading(), isA<NeedsOnboarding>()],
    );

    blocTest<AuthBloc, AuthState>(
      'suspended provider → AccountBlocked (E-14)',
      setUp: () => when(() => complete(
            phoneNumber: any(named: 'phoneNumber'),
            code: any(named: 'code'),
            firstName: any(named: 'firstName'),
            lastName: any(named: 'lastName'),
            email: any(named: 'email'),
          )).thenAnswer((_) async =>
          Right(_session(providerId: 'p', status: ProviderStatus.suspended))),
      build: build,
      act: (b) => b.add(
          const VerifyCodeRequested(phoneNumber: '09121234567', code: '123456')),
      expect: () => [const AuthLoading(), isA<AccountBlocked>()],
    );

    blocTest<AuthBloc, AuthState>(
      'wrong code → AuthError',
      setUp: () => when(() => complete(
            phoneNumber: any(named: 'phoneNumber'),
            code: any(named: 'code'),
            firstName: any(named: 'firstName'),
            lastName: any(named: 'lastName'),
            email: any(named: 'email'),
          )).thenAnswer(
          (_) async => const Left(AuthFailure('کد وارد شده صحیح نیست'))),
      build: build,
      act: (b) => b.add(
          const VerifyCodeRequested(phoneNumber: '09121234567', code: '000000')),
      expect: () =>
          [const AuthLoading(), const AuthError('کد وارد شده صحیح نیست')],
    );
  });

  group('ResendCodeRequested', () {
    blocTest<AuthBloc, AuthState>(
      'emits [OtpResent] on success (no Loading)',
      setUp: () => when(() => send(phoneNumber: any(named: 'phoneNumber')))
          .thenAnswer((_) async => const Right('sent')),
      build: build,
      act: (b) => b.add(const ResendCodeRequested('09121234567')),
      expect: () => [isA<OtpResent>()],
    );
  });

  group('AuthStatusChecked (session restore)', () {
    blocTest<AuthBloc, AuthState>(
      'not logged in → Unauthenticated',
      setUp: () => when(() => repo.isLoggedIn()).thenAnswer((_) async => false),
      build: build,
      act: (b) => b.add(const AuthStatusChecked()),
      expect: () => [const Unauthenticated()],
    );

    blocTest<AuthBloc, AuthState>(
      'logged-in active provider → Authenticated',
      setUp: () {
        when(() => repo.isLoggedIn()).thenAnswer((_) async => true);
        when(() => repo.getCurrentSession()).thenAnswer((_) async =>
            Right(_session(providerId: 'p', status: ProviderStatus.active)));
      },
      build: build,
      act: (b) => b.add(const AuthStatusChecked()),
      expect: () => [isA<Authenticated>()],
    );

    blocTest<AuthBloc, AuthState>(
      'logged-in drafted provider → NeedsOnboarding',
      setUp: () {
        when(() => repo.isLoggedIn()).thenAnswer((_) async => true);
        when(() => repo.getCurrentSession()).thenAnswer((_) async =>
            Right(_session(providerId: 'p', status: ProviderStatus.drafted)));
      },
      build: build,
      act: (b) => b.add(const AuthStatusChecked()),
      expect: () => [isA<NeedsOnboarding>()],
    );
  });

  group('ProviderStatusRefreshRequested (post-onboarding)', () {
    blocTest<AuthBloc, AuthState>(
      'drafted → active after onboarding completes → Authenticated',
      setUp: () => when(() => repo.refreshProviderStatus()).thenAnswer((_) async =>
          Right(_session(providerId: 'p', status: ProviderStatus.active))),
      build: build,
      act: (b) => b.add(const ProviderStatusRefreshRequested()),
      expect: () => [const AuthLoading(), isA<Authenticated>()],
    );

    blocTest<AuthBloc, AuthState>(
      'still drafted → stays in onboarding',
      setUp: () => when(() => repo.refreshProviderStatus()).thenAnswer((_) async =>
          Right(_session(providerId: 'p', status: ProviderStatus.drafted))),
      build: build,
      act: (b) => b.add(const ProviderStatusRefreshRequested()),
      expect: () => [const AuthLoading(), isA<NeedsOnboarding>()],
    );
  });

  group('LogoutRequested', () {
    blocTest<AuthBloc, AuthState>(
      'emits [Loading, LoggedOut]',
      setUp: () =>
          when(() => repo.logout()).thenAnswer((_) async => const Right(null)),
      build: build,
      act: (b) => b.add(const LogoutRequested()),
      expect: () => [const AuthLoading(), const LoggedOut()],
    );
  });
}
