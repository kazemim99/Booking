import 'package:dartz/dartz.dart';

import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/entities/user.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/usecases/complete_authentication_usecase.dart';
import 'package:asan_rezerve_customer_app/features/auth/domain/usecases/send_verification_code_usecase.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_state.dart';

/// A real [AuthBloc] over a repository that does nothing, for screens that only need to READ auth state.
///
/// It starts as a guest (the bloc's own initial state). Call [signIn] to put it in the state a successful OTP
/// produces. Shared so a screen test does not have to hand-roll the whole auth repository to render.
class FakeAuthBloc extends AuthBloc {
  final _InertAuthRepository _repo;

  FakeAuthBloc._(this._repo)
      : super(
          SendVerificationCodeUseCase(_repo),
          CompleteAuthenticationUseCase(_repo),
          _repo,
        );

  factory FakeAuthBloc() => FakeAuthBloc._(_InertAuthRepository());

  /// Signed in as [session], [fakeSession] by default.
  void signIn([AuthSession? session]) => emit(Authenticated(session ?? fakeSession));

  /// The names the bloc asked the repository to keep on the device, oldest first.
  List<(String, String)> get rememberedNames => List.unmodifiable(_repo.remembered);
}

/// A session for someone called [firstName] [lastName] — «مشتری 9384444636» is how the server stores a customer
/// who signed up by OTP and never gave a name.
AuthSession sessionNamed(String? firstName, String? lastName) => AuthSession(
      accessToken: 'token',
      refreshToken: 'refresh',
      user: User(
        id: 'user-1',
        phoneNumber: '+989121234567',
        firstName: firstName,
        lastName: lastName,
        createdAt: DateTime(2026, 1, 1),
      ),
      expiresIn: 3600,
    );

final fakeSession = AuthSession(
  accessToken: 'token',
  refreshToken: 'refresh',
  user: User(id: 'user-1', phoneNumber: '+989121234567', createdAt: DateTime(2026, 1, 1)),
  expiresIn: 3600,
);

class _InertAuthRepository implements AuthRepository {
  final remembered = <(String, String)>[];

  @override
  Future<void> rememberUserName({required String firstName, required String lastName}) async =>
      remembered.add((firstName, lastName));

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
