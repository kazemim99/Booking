import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/auth/domain/entities/user.dart';
import 'package:booksy_customer_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:booksy_customer_app/features/auth/domain/usecases/complete_authentication_usecase.dart';
import 'package:booksy_customer_app/features/auth/domain/usecases/send_verification_code_usecase.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_state.dart';

/// Regression cover for a cold-start hang: the splash screen only leaves once AuthBloc reaches a
/// TERMINAL state (Authenticated / Unauthenticated / LoggedOut) — see AppRouter's AuthNotifier,
/// which flips `sessionResolved` only on those three states. CheckAuthStatusEvent is the one thing
/// dispatched at app start, and its handler used to have no exception handling: if reading the
/// stored session threw (which flutter_secure_storage on web can do when decrypting a token written
/// by an earlier build/session), the handler died mid-flight, no terminal state was ever emitted, and
/// the app was stuck on the splash screen forever with no way to recover except a hard reload.
class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository({this.isLoggedInResult = false, this.throwOnIsLoggedIn = false, this.throwOnGetSession = false, this.sessionResult});

  final bool isLoggedInResult;
  final bool throwOnIsLoggedIn;
  final bool throwOnGetSession;
  final Either<Failure, AuthSession?>? sessionResult;

  @override
  Future<bool> isLoggedIn() async {
    if (throwOnIsLoggedIn) {
      throw Exception('secure storage decrypt failed');
    }
    return isLoggedInResult;
  }

  @override
  Future<Either<Failure, AuthSession?>> getCurrentSession() async {
    if (throwOnGetSession) {
      throw Exception('secure storage decrypt failed');
    }
    return sessionResult ?? const Right(null);
  }

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
      const Left(ServerFailure('not used in this test'));

  @override
  Future<Either<Failure, String>> resendOtp({required String phoneNumber}) async =>
      const Right('resent');

  @override
  Future<Either<Failure, AuthSession>> refreshToken() async =>
      const Left(ServerFailure('not used in this test'));

  @override
  Future<Either<Failure, void>> logout() async => const Right(null);
}

AuthSession _session() => AuthSession(
      accessToken: 'tok',
      refreshToken: 'refresh',
      user: User(
        id: 'u1',
        phoneNumber: '+989121234567',
        createdAt: DateTime(2026, 1, 1),
      ),
      expiresIn: 3600,
    );

AuthBloc _bloc(_FakeAuthRepository repo) => AuthBloc(
      SendVerificationCodeUseCase(repo),
      CompleteAuthenticationUseCase(repo),
      repo,
    );

void main() {
  group('CheckAuthStatusEvent always reaches a terminal state', () {
    test('emits Authenticated when a valid session is stored', () async {
      final bloc = _bloc(_FakeAuthRepository(
        isLoggedInResult: true,
        sessionResult: Right(_session()),
      ));
      addTearDown(bloc.close);

      bloc.add(const CheckAuthStatusEvent());
      await expectLater(
        bloc.stream,
        emitsInOrder([isA<AuthLoading>(), isA<Authenticated>()]),
      );
    });

    test('emits Unauthenticated when no token is stored', () async {
      final bloc = _bloc(_FakeAuthRepository(isLoggedInResult: false));
      addTearDown(bloc.close);

      bloc.add(const CheckAuthStatusEvent());
      await expectLater(
        bloc.stream,
        emitsInOrder([isA<AuthLoading>(), isA<Unauthenticated>()]),
      );
    });

    test('emits Unauthenticated when a token exists but the session cannot be read', () async {
      final bloc = _bloc(_FakeAuthRepository(
        isLoggedInResult: true,
        sessionResult: const Left(CacheFailure('corrupt')),
      ));
      addTearDown(bloc.close);

      bloc.add(const CheckAuthStatusEvent());
      await expectLater(
        bloc.stream,
        emitsInOrder([isA<AuthLoading>(), isA<Unauthenticated>()]),
      );
    });

    test(
      'emits Unauthenticated rather than hanging when isLoggedIn() throws '
      '(the exact defect: a stale/corrupt secure-storage read stranded the splash screen)',
      () async {
        final bloc = _bloc(_FakeAuthRepository(throwOnIsLoggedIn: true));
        addTearDown(bloc.close);

        bloc.add(const CheckAuthStatusEvent());
        await expectLater(
          bloc.stream,
          emitsInOrder([isA<AuthLoading>(), isA<Unauthenticated>()]),
        );
      },
    );

    test(
      'emits Unauthenticated rather than hanging when getCurrentSession() throws',
      () async {
        final bloc = _bloc(_FakeAuthRepository(
          isLoggedInResult: true,
          throwOnGetSession: true,
        ));
        addTearDown(bloc.close);

        bloc.add(const CheckAuthStatusEvent());
        await expectLater(
          bloc.stream,
          emitsInOrder([isA<AuthLoading>(), isA<Unauthenticated>()]),
        );
      },
    );
  });
}
