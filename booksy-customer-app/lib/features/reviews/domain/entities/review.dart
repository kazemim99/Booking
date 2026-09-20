import 'package:equatable/equatable.dart';

/// What one customer said about a salon after a visit.
class Review extends Equatable {
  final String id;
  final String customerName;
  final double rating;
  final String? comment;
  final DateTime? createdAt;

  /// The salon's reply, when it wrote one.
  final String? providerResponse;

  const Review({
    required this.id,
    required this.customerName,
    required this.rating,
    this.comment,
    this.createdAt,
    this.providerResponse,
  });

  @override
  List<Object?> get props =>
      [id, customerName, rating, comment, createdAt, providerResponse];
}

/// A salon's reviews, with the summary shown above them.
class ProviderReviews extends Equatable {
  final double averageRating;
  final int totalReviews;
  final List<Review> items;

  const ProviderReviews({
    this.averageRating = 0,
    this.totalReviews = 0,
    this.items = const [],
  });

  @override
  List<Object?> get props => [averageRating, totalReviews, items];
}
