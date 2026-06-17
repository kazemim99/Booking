import 'package:dartz/dartz.dart';
import 'package:injectable/injectable.dart';
import '../../../../core/errors/failures.dart';
import '../entities/user.dart';
import '../repositories/auth_repository.dart';

/// Complete Authentication Use Case
/// Verifies OTP code and completes login/registration
@injectable
class CompleteAuthenticationUseCase {
  final AuthRepository _repository;

  CompleteAuthenticationUseCase(this._repository);

  Future<Either<Failure, AuthSession>> call({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  }) async {
    // Validate inputs
    if (phoneNumber.isEmpty) {
      return const Left(ValidationFailure('شماره تلفن نمی‌تواند خالی باشد'));
    }

    if (code.isEmpty) {
      return const Left(ValidationFailure('کد تایید نمی‌تواند خالی باشد'));
    }

    if (code.length != 6) {
      return const Left(ValidationFailure('کد تایید باید 6 رقمی باشد'));
    }

    // Validate email format if provided
    if (email != null && email.isNotEmpty) {
      if (!_isValidEmail(email)) {
        return const Left(ValidationFailure('فرمت ایمیل معتبر نیست'));
      }
    }

    return await _repository.completeAuthentication(
      phoneNumber: phoneNumber,
      code: code,
      firstName: firstName,
      lastName: lastName,
      email: email,
    );
  }

  bool _isValidEmail(String email) {
    final emailRegex = RegExp(
      r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$',
    );
    return emailRegex.hasMatch(email);
  }
}
