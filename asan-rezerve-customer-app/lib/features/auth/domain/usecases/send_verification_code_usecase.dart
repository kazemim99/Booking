import 'package:dartz/dartz.dart';
import 'package:injectable/injectable.dart';
import '../../../../core/errors/failures.dart';
import '../repositories/auth_repository.dart';

/// Send Verification Code Use Case
@injectable
class SendVerificationCodeUseCase {
  final AuthRepository _repository;

  SendVerificationCodeUseCase(this._repository);

  Future<Either<Failure, String>> call({
    required String phoneNumber,
    String countryCode = '+98',
  }) async {
    // Validate phone number
    if (phoneNumber.isEmpty) {
      return const Left(ValidationFailure('شماره تلفن نمی‌تواند خالی باشد'));
    }

    // Remove non-numeric characters
    final cleanedPhoneNumber = phoneNumber.replaceAll(RegExp(r'[^\d]'), '');

    // Validate Iranian phone number format
    if (countryCode == '+98') {
      if (!_isValidIranianPhoneNumber(cleanedPhoneNumber)) {
        return const Left(ValidationFailure('شماره تلفن معتبر نیست'));
      }
    }

    return await _repository.sendVerificationCode(
      phoneNumber: cleanedPhoneNumber,
      countryCode: countryCode,
    );
  }

  bool _isValidIranianPhoneNumber(String phoneNumber) {
    // Iranian mobile numbers start with 9 and have 10 digits
    // Or they can start with 09 and have 11 digits
    if (phoneNumber.length == 10 && phoneNumber.startsWith('9')) {
      return true;
    }
    if (phoneNumber.length == 11 && phoneNumber.startsWith('09')) {
      return true;
    }
    return false;
  }
}
