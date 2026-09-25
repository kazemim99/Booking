import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/core/push/push_registration.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/entities/user.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/usecases/complete_authentication_usecase.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/usecases/send_verification_code_usecase.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_state.dart';

/// Push registration rides the real auth flow. A registration service that nothing calls is the exact failure
/// add-notification-clients exists to fix, so these go through the bloc rather than the service.

final _session = AuthSession(
  accessToken: 'token',
  refreshToken: 'refresh',
  user: User(id: 'user-1', phoneNumber: '+989121234567', createdAt: DateTime(2026, 1, 1)),
  expiresIn: 3600,
);

/// One log for both collaborators, so the ORDER of push and session calls can be asserted.
class _Log {
  final calls = <String>[];
}

class _Repo implements AuthRepository {
  final _Log log;
  bool verifySucceeds = true;
  bool hasStoredSession = false;

  _Repo(this.log);

  @override
  Future<Either<Failure, String>> sendVerificationCode({
    required String phoneNumber,
    String countryCode = '+98',
  }) async =>
      const Right('sent');

  @override
  Future<Either<Failure, AuthSession>> completeAuthentication({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  }) async =>
      verifySucceeds ? Right(_session) : const Left(ValidationFailure('bad code'));

  @override
  Future<Either<Failure, String>> resendOtp({required String phoneNumber}) async => const Right('resent');

  @override
  Future<Either<Failure, AuthSession>> refreshToken() async => Right(_session);

  @override
  Future<Either<Failure, void>> logout() async {
    log.calls.add('sessionCleared');
    return const Right(null);
  }

  @override
  Future<bool> isLoggedIn() async => hasStoredSession;

  @override
  Future<Either<Failure, AuthSession?>> getCurrentSession() async => Right(hasStoredSession ? _session : null);

  @override
  Future<void> rememberUserName({required String firstName, required String lastName}) async {}
}

class _Push implements PushLifecycle {
  final _Log log;
  _Push(this.log);

  @override
  Future<void> onSignedIn() async => log.calls.add('signedIn');

  @override
  Future<void> onSigningOut() async => log.calls.add('signingOut');
}

void main() {
  late _Log log;
  late _Repo repo;
  late AuthBloc bloc;

  setUp(() {
    log = _Log();
    repo = _Repo(log);
    bloc = AuthBloc(
      SendVerificationCodeUseCase(repo),
      CompleteAuthenticationUseCase(repo),
      repo,
      push: _Push(log),
    );
  });

  tearDown(() => bloc.close());

  Future<void> settle() => Future<void>.delayed(const Duration(milliseconds: 10));

  test('signing in with an OTP registers this device', () async {
    bloc.add(const VerifyCodeEvent(phoneNumber: '09121234567', code: '123456'));
    await settle();

    expect(bloc.state, isA<Authenticated>());
    expect(log.calls, ['signedIn']);
  });

  test('a failed OTP registers nothing', () async {
    repo.verifySucceeds = false;

    bloc.add(const VerifyCodeEvent(phoneNumber: '09121234567', code: '000000'));
    await settle();

    expect(log.calls, isEmpty);
  });

  test('a restored session at app start re-registers, which is how a failed registration is retried', () async {
    repo.hasStoredSession = true;

    bloc.add(const CheckAuthStatusEvent());
    await settle();

    expect(log.calls, ['signedIn']);
  });

  test('a guest at app start registers nothing', () async {
    bloc.add(const CheckAuthStatusEvent());
    await settle();

    expect(log.calls, isEmpty);
  });

  test('signing out revokes the device BEFORE the session is cleared', () async {
    // Revoking is an authenticated call; after logout it would be answered 401 and the token would stay
    // attached to the account on a handset that may be handed to someone else.
    bloc.add(const LogoutEvent());
    await settle();

    expect(log.calls, ['signingOut', 'sessionCleared']);
  });
}
