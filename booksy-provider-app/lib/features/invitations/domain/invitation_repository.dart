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
}
