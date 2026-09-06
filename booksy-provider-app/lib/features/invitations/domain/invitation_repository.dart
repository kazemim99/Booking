import 'package:dartz/dartz.dart';

import '../../../core/errors/failures.dart';
import 'invitation_summary.dart';

/// Accept-invitation repository contract.
abstract class InvitationRepository {
  /// The public invitation summary, or null when the invitation does not exist.
  Future<Either<Failure, InvitationSummary?>> fetchSummary(String invitationId);

  /// Accept as the authenticated (existing) person. An [AuthFailure] means the
  /// caller must log in first.
  Future<Either<Failure, void>> accept(String invitationId);

  /// New-user path, step 1: send an OTP to the invitation's own phone. Returns
  /// the masked phone the server echoes back (for "code sent to ••••1234").
  Future<Either<Failure, String>> sendOtp(String invitationId);

  /// New-user path, step 2: register (name) + verify the OTP + accept.
  Future<Either<Failure, void>> registerAndAccept(
    String invitationId, {
    required String firstName,
    required String lastName,
    String? email,
    required String otpCode,
  });
}
