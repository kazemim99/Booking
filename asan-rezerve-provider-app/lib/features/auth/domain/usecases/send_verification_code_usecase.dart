import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/phone_number.dart';
import '../repositories/auth_repository.dart';

/// Sends the OTP verification code for provider login/registration.
///
/// Validates the phone number through the single-source [PhoneNumber] value
/// object before hitting the network.
class SendVerificationCodeUseCase {
  final AuthRepository _repository;

  const SendVerificationCodeUseCase(this._repository);

  Future<Either<Failure, String>> call({required String phoneNumber}) async {
    final phone = PhoneNumber.tryParse(phoneNumber);
    if (phone == null) {
      return const Left(ValidationFailure('شماره موبایل وارد شده معتبر نیست'));
    }
    return _repository.sendVerificationCode(phoneNumber: phone.value);
  }
}
