import 'package:equatable/equatable.dart';

/// The four optional things a customer may rate beside the overall star
/// (openspec/changes/provider-reviews-and-ratings, design D1). [wireName] is the
/// request field and [statisticsKey] the listing's summary key.
enum ReviewDimension {
  cleanliness('نظافت و بهداشت', 'cleanlinessRating', 'cleanliness'),
  skill('مهارت و کیفیت کار', 'skillRating', 'skill'),
  punctuality('وقت‌شناسی', 'punctualityRating', 'punctuality'),
  conduct('برخورد و رفتار', 'conductRating', 'conduct');

  final String label;
  final String wireName;
  final String statisticsKey;

  const ReviewDimension(this.label, this.wireName, this.statisticsKey);

  /// The dimensions present in a JSON object, skipping any left unrated.
  static Map<ReviewDimension, double> readFrom(Map<String, dynamic> json) => {
        for (final d in values)
          if (json[d.wireName] is num) d: (json[d.wireName] as num).toDouble(),
      };
}

/// A reader's vote on someone else's review.
enum ReviewVote {
  helpful,
  notHelpful;

  static ReviewVote? parse(Object? wire) => switch (wire) {
        'helpful' => ReviewVote.helpful,
        'notHelpful' => ReviewVote.notHelpful,
        _ => null,
      };
}

/// What one customer said about a salon after a visit.
class Review extends Equatable {
  final String id;
  final String customerName;
  final double rating;
  final String? comment;
  final DateTime? createdAt;

  /// The salon's reply, when it wrote one and it was approved.
  final String? providerResponse;

  /// Written after a visit on record — every review is, by construction; said
  /// on the card because it is what makes a review worth trusting.
  final bool isVerified;

  /// Only the dimensions the customer chose to rate.
  final Map<ReviewDimension, double> dimensions;
  final int helpfulCount;
  final int notHelpfulCount;

  /// The signed-in reader's own vote, if any.
  final ReviewVote? myVote;

  const Review({
    required this.id,
    required this.customerName,
    required this.rating,
    this.comment,
    this.createdAt,
    this.providerResponse,
    this.isVerified = false,
    this.dimensions = const {},
    this.helpfulCount = 0,
    this.notHelpfulCount = 0,
    this.myVote,
  });

  /// This review with the server's answer to a vote on it.
  Review withVote(ReviewVoteResult result) => Review(
        id: id,
        customerName: customerName,
        rating: rating,
        comment: comment,
        createdAt: createdAt,
        providerResponse: providerResponse,
        isVerified: isVerified,
        dimensions: dimensions,
        helpfulCount: result.helpfulCount,
        notHelpfulCount: result.notHelpfulCount,
        myVote: result.myVote,
      );

  @override
  List<Object?> get props => [
        id,
        customerName,
        rating,
        comment,
        createdAt,
        providerResponse,
        isVerified,
        dimensions,
        helpfulCount,
        notHelpfulCount,
        myVote,
      ];
}

/// One dimension's average over the published reviews that rated it.
class DimensionAverage extends Equatable {
  final double average;
  final int count;

  const DimensionAverage({required this.average, required this.count});

  @override
  List<Object?> get props => [average, count];
}

/// A salon's reviews, with the summary shown above them.
class ProviderReviews extends Equatable {
  final double averageRating;
  final int totalReviews;
  final List<Review> items;

  /// Only the dimensions somebody rated; a missing key means nobody has.
  final Map<ReviewDimension, DimensionAverage> dimensions;

  /// How many published reviews gave each whole star, 5 down to 1 — the shape
  /// of the average, which one number hides.
  final Map<int, int> distribution;

  /// The last page read, and whether the salon has more reviews than are loaded.
  final int page;
  final bool hasMore;

  const ProviderReviews({
    this.averageRating = 0,
    this.totalReviews = 0,
    this.items = const [],
    this.dimensions = const {},
    this.distribution = const {},
    this.page = 1,
    this.hasMore = false,
  });

  ProviderReviews withItems(List<Review> next) => ProviderReviews(
        averageRating: averageRating,
        totalReviews: totalReviews,
        items: next,
        dimensions: dimensions,
        distribution: distribution,
        page: page,
        hasMore: hasMore,
      );

  /// The next page read after this one: its reviews after these (none twice),
  /// and its paging.
  ProviderReviews appending(ProviderReviews next) {
    final seen = {for (final r in items) r.id};
    return ProviderReviews(
      averageRating: next.averageRating,
      totalReviews: next.totalReviews,
      items: [...items, ...next.items.where((r) => !seen.contains(r.id))],
      dimensions: next.dimensions,
      distribution: next.distribution,
      page: next.page,
      hasMore: next.hasMore,
    );
  }

  @override
  List<Object?> get props =>
      [averageRating, totalReviews, items, dimensions, distribution, page, hasMore];
}

/// The server's tally after a vote: counts and the caller's vote, which is
/// null when the vote was withdrawn.
class ReviewVoteResult extends Equatable {
  final int helpfulCount;
  final int notHelpfulCount;
  final ReviewVote? myVote;

  const ReviewVoteResult({
    required this.helpfulCount,
    required this.notHelpfulCount,
    this.myVote,
  });

  @override
  List<Object?> get props => [helpfulCount, notHelpfulCount, myVote];
}

/// Where a review stands with the administrators.
enum ReviewModerationStatus {
  pending,
  published,
  rejected,
  hidden;

  static ReviewModerationStatus parse(Object? wire) =>
      switch ('$wire'.toLowerCase()) {
        'published' => ReviewModerationStatus.published,
        'rejected' => ReviewModerationStatus.rejected,
        'hidden' => ReviewModerationStatus.hidden,
        _ => ReviewModerationStatus.pending,
      };
}

/// A review as its own author sees it: in any state, with the reason an
/// administrator gave, and whether it can still be edited.
class MyReview extends Equatable {
  final String id;
  final String? providerId;
  final String? providerName;
  final String? providerLogoUrl;
  final String? serviceName;
  final double rating;
  final Map<ReviewDimension, double> dimensions;
  final String? comment;
  final ReviewModerationStatus status;
  final String? moderationReason;

  /// The salon's reply — only once it has been approved.
  final String? providerResponse;
  final DateTime? createdAt;
  final DateTime? editedAt;
  final bool canEdit;

  /// Whether the author's name signs it in public (else «مشتری»).
  final bool showName;

  const MyReview({
    required this.id,
    this.providerId,
    this.providerName,
    this.providerLogoUrl,
    this.serviceName,
    required this.rating,
    this.dimensions = const {},
    this.comment,
    required this.status,
    this.moderationReason,
    this.providerResponse,
    this.createdAt,
    this.editedAt,
    this.canEdit = false,
    this.showName = true,
  });

  bool get isEdited => editedAt != null;

  @override
  List<Object?> get props => [
        id,
        providerId,
        providerName,
        providerLogoUrl,
        serviceName,
        rating,
        dimensions,
        comment,
        status,
        moderationReason,
        providerResponse,
        createdAt,
        editedAt,
        canEdit,
        showName,
      ];
}
