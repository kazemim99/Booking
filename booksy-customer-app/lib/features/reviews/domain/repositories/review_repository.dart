import 'package:dartz/dartz.dart';

import '../../../../core/errors/failures.dart';
import '../entities/review.dart';

/// Reading a salon's reviews, leaving and editing one after a visit, and voting
/// on what others wrote.
abstract class ReviewRepository {
  Future<Either<Failure, ProviderReviews>> getProviderReviews(String providerId);

  /// A review belongs to a booking: only someone who actually went can write one.
  /// It is public only once an administrator approves it.
  Future<Either<Failure, void>> createReview({
    required String bookingId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
  });

  /// The author's own edit. It goes back to approval before it is public again.
  Future<Either<Failure, void>> editReview({
    required String reviewId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
  });

  /// The same vote again withdraws it; the other one moves it. Signed-in only.
  Future<Either<Failure, ReviewVoteResult>> vote(String reviewId, bool isHelpful);

  /// The signed-in customer's own reviews, in every moderation state.
  Future<Either<Failure, List<MyReview>>> getMyReviews();
}
