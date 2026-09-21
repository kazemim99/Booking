import 'package:dartz/dartz.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/auth/domain/entities/user.dart';
import 'package:booksy_customer_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:booksy_customer_app/features/auth/domain/usecases/complete_authentication_usecase.dart';
import 'package:booksy_customer_app/features/auth/domain/usecases/send_verification_code_usecase.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_state.dart';

/// A real [AuthBloc] over a repository that does nothing, for screens that only need to READ auth state.
///
/// It starts as a guest (the bloc's own initial state). Call [signIn] to put it in the state a successful OTP
/// produces. Shared so a screen test does not have to hand-roll the whole auth repository to render.
class FakeAuthBloc extends AuthBloc {
  FakeAuthBloc._(AuthRepository repo)
      : super(
          SendVerificationCodeUseCase(repo),
          CompleteAuthenticationUseCase(repo),
          repo,
        );

  factory FakeAuthBloc() => FakeAuthBloc._(_InertAuthRepository());

  void signIn() => emit(Authenticated(fakeSession));
}

final fakeSession = AuthSession(
  accessToken: 'token',
  refreshToken: 'refresh',
  user: User(id: 'user-1', phoneNumber: '+989121234567', createdAt: DateTime(2026, 1, 1)),
  expiresIn: 3600,
);

class _InertAuthRepository implements AuthRepository {
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
      Right(fakeSession);

  @override
  Future<Either<Failure, String>> resendOtp({required String phoneNumber}) async => const Right('resent');

  @override
  Future<Either<Failure, AuthSession>> refreshToken() async => Right(fakeSession);

  @override
  Future<Either<Failure, void>> logout() async => const Right(null);

  @override
  Future<bool> isLoggedIn() async => false;

  @override
  Future<Either<Failure, AuthSession?>> getCurrentSession() async => const Right(null);
}
