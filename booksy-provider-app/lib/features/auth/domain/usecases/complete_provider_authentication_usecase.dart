import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/phone_number.dart';
import '../entities/provider_session.dart';
import '../repositories/auth_repository.dart';

/// Verifies the OTP and completes provider authentication (login/registration).
class CompleteProviderAuthenticationUseCase {
  final AuthRepository _repository;

  const CompleteProviderAuthenticationUseCase(this._repository);

  Future<Either<Failure, ProviderSession>> call({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  }) async {
    final phone = PhoneNumber.tryParse(phoneNumber);
    if (phone == null) {
      return const Left(ValidationFailure('شماره موبایل وارد شده معتبر نیست'));
    }
    if (code.length != 6) {
      return const Left(ValidationFailure('لطفاً کد ۶ رقمی را وارد کنید'));
    }
    return _repository.completeProviderAuthentication(
      phoneNumber: phone.value,
      code: code,
      firstName: firstName,
      lastName: lastName,
      email: email,
    );
  }
}
