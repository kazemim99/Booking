import 'package:dartz/dartz.dart';

import '../../../core/errors/failures.dart';
import 'business_review.dart';

/// The signed-in business's reviews and its replies to them.
abstract class ReviewsRepository {
  /// Headline numbers (published only) and every review in any state.
  Future<Either<Failure, ReviewsOverview>> load();

  /// Writes the first reply ([edit] false) or rewrites it ([edit] true). Either way it goes to moderation.
  Future<Either<Failure, ReplyResult>> reply(String reviewId, String text, {required bool edit});

  Future<Either<Failure, void>> removeReply(String reviewId);
}
