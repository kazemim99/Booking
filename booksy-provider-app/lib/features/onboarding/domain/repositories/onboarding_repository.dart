import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/onboarding_data.dart';
import '../entities/onboarding_draft.dart';

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

  /// Uploads gallery images (step 7, optional). The backend resolves the draft
  /// from the authenticated user; [providerId] is required only to guard that a
  /// draft exists client-side.
  Future<Either<Failure, void>> uploadGallery(
    String providerId,
    List<GalleryImageUpload> images,
  );

  /// Completes registration for [providerId] (step 9).
  Future<Either<Failure, void>> complete(String providerId);

  /// Returns the in-progress draft (with every saved field rehydrated), or null.
  Future<Either<Failure, OnboardingDraft?>> getDraft();
}
