import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart' show CancelToken, ProgressCallback;
import '../../../../core/errors/failures.dart';
import '../entities/onboarding_data.dart';
import '../entities/onboarding_draft.dart';
import '../../../home/domain/entities/saved_customer.dart';

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

  /// One gallery photo, with byte progress and cancellation (UploadQueue).
  Future<Either<Failure, void>> uploadGalleryImage(
    String providerId,
    GalleryImageUpload image, {
    ProgressCallback? onProgress,
    CancelToken? cancelToken,
  });

  /// Completes registration for [providerId] (step 9).
  Future<Either<Failure, void>> complete(String providerId);

  /// Records whether the owner personally provides services (onboarding branch).
  Future<Either<Failure, void>> setOwnerProvidesServices(bool providesServices);

  /// Optional last step: saves one customer to the new salon's book.
  Future<Either<Failure, void>> addCustomer(
      String providerId, CustomerDraft customer);

  /// Optional last step: saves the contacts the owner ticked; how many were added.
  Future<Either<Failure, int>> importCustomers(
      String providerId, List<CustomerDraft> contacts);

  /// Returns the in-progress draft (with every saved field rehydrated), or null.
  Future<Either<Failure, OnboardingDraft?>> getDraft();
}
