import 'package:dartz/dartz.dart';

import '../../../../core/errors/failures.dart';
import '../entities/review.dart';

/// Reading a salon's reviews, and leaving one after a visit.
abstract class ReviewRepository {
  Future<Either<Failure, ProviderReviews>> getProviderReviews(String providerId);

  /// A review belongs to a booking: only someone who actually went can write one.
  Future<Either<Failure, void>> createReview({
    required String bookingId,
    required double rating,
    String? comment,
  });
}
