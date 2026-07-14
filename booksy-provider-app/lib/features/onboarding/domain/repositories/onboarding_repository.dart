import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/onboarding_data.dart';

/// Provider onboarding repository contract.
abstract class OnboardingRepository {
  /// Creates the organization draft (step 3). Returns the draft providerId.
  Future<Either<Failure, String>> createDraft(OnboardingData data);

  /// Saves services for [providerId] (step 4).
  Future<Either<Failure, void>> saveServices(
    String providerId,
    List<ServiceDraft> services,
  );

  /// Saves working hours for [providerId] (step 6).
  Future<Either<Failure, void>> saveWorkingHours(
    String providerId,
    List<DayHours> hours,
  );

  /// Completes registration for [providerId] (step 9).
  Future<Either<Failure, void>> complete(String providerId);

  /// Returns an in-progress draft's providerId, or null.
  Future<Either<Failure, String?>> getDraftProviderId();
}
