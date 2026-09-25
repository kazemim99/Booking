import 'package:equatable/equatable.dart';

/// The four optional things a customer may rate beside the overall star
/// (openspec/changes/provider-reviews-and-ratings, design D1). [wireName] is the
/// review's field, [statisticsKey] the listing summary's.
enum ReviewDimension {
  cleanliness('نظافت و بهداشت', 'cleanlinessRating', 'cleanliness'),
  skill('مهارت و کیفیت کار', 'skillRating', 'skill'),
  punctuality('وقت‌شناسی', 'punctualityRating', 'punctuality'),
  conduct('برخورد و رفتار', 'conductRating', 'conduct');

  final String label;
  final String wireName;
  final String statisticsKey;

  const ReviewDimension(this.label, this.wireName, this.statisticsKey);

  /// The dimensions present in a review, skipping any left unrated.
  static Map<ReviewDimension, double> readFrom(Map<String, dynamic> json) => {
        for (final d in values)
          if (json[d.wireName] is num) d: (json[d.wireName] as num).toDouble(),
      };
}

/// Where a review stands with the administrators.
enum ReviewStatus {
  pending,
  published,
  rejected,
  hidden;

  static ReviewStatus parse(Object? wire) => switch ('$wire'.toLowerCase()) {
        'published' => ReviewStatus.published,
        'rejected' => ReviewStatus.rejected,
        'hidden' => ReviewStatus.hidden,
        _ => ReviewStatus.pending,
      };
}

/// Where the business's reply stands. A reply is public only once approved.
enum ReplyStatus {
  pending,
  published,
  rejected;

  static ReplyStatus? parse(Object? wire) => switch ('$wire'.toLowerCase()) {
        'pending' => ReplyStatus.pending,
        'published' => ReplyStatus.published,
        'rejected' => ReplyStatus.rejected,
        _ => null,
      };
}

/// One review of this business, as its owner sees it: in any state.
class BusinessReview extends Equatable {
  final String id;
  final double rating;
  final Map<ReviewDimension, double> dimensions;
  final String? comment;
  final ReviewStatus status;
  final String? reply;
  final ReplyStatus? replyStatus;

  /// Why an administrator refused the reply, when they did.
  final String? replyReason;
  final int helpfulCount;
  final int notHelpfulCount;
  final DateTime? createdAt;

  const BusinessReview({
    required this.id,
    required this.rating,
    this.dimensions = const {},
    this.comment,
    required this.status,
    this.reply,
    this.replyStatus,
    this.replyReason,
    this.helpfulCount = 0,
    this.notHelpfulCount = 0,
    this.createdAt,
  });

  bool get hasReply => reply != null && reply!.isNotEmpty;

  /// Only a published review can be answered — a pending one may yet be rejected.
  bool get canReply => status == ReviewStatus.published && !hasReply;

  /// Waiting on the business: public and unanswered, or answered with a refused reply.
  bool get awaitingReply =>
      status == ReviewStatus.published && (!hasReply || replyStatus == ReplyStatus.rejected);

  BusinessReview withReply(ReplyResult? result) => BusinessReview(
        id: id,
        rating: rating,
        dimensions: dimensions,
        comment: comment,
        status: status,
        reply: result?.text,
        replyStatus: result?.status,
        replyReason: null,
        helpfulCount: helpfulCount,
        notHelpfulCount: notHelpfulCount,
        createdAt: createdAt,
      );

  factory BusinessReview.fromJson(Map<String, dynamic> json) {
    DateTime? date(Object? v) => v is String ? DateTime.tryParse(v) : null;
    return BusinessReview(
      id: (json['reviewId'] ?? json['id'] ?? '').toString(),
      rating: (json['rating'] as num?)?.toDouble() ?? 0,
      dimensions: ReviewDimension.readFrom(json),
      comment: json['comment'] as String?,
      status: ReviewStatus.parse(json['moderationStatus']),
      reply: json['providerResponse'] as String?,
      replyStatus: ReplyStatus.parse(json['replyModerationStatus']),
      replyReason: json['replyModerationReason'] as String?,
      helpfulCount: (json['helpfulCount'] as num?)?.toInt() ?? 0,
      notHelpfulCount: (json['notHelpfulCount'] as num?)?.toInt() ?? 0,
      createdAt: date(json['createdAt']),
    );
  }

  @override
  List<Object?> get props => [
        id,
        rating,
        dimensions,
        comment,
        status,
        reply,
        replyStatus,
        replyReason,
        helpfulCount,
        notHelpfulCount,
        createdAt,
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

/// The business's reviews: the published headline numbers, and every review in any state.
class ReviewsOverview extends Equatable {
  final double averageRating;

  /// Published reviews only — the count customers see.
  final int publishedCount;
  final int awaitingReplyCount;

  /// Only the dimensions somebody rated.
  final Map<ReviewDimension, DimensionAverage> dimensions;
  final List<BusinessReview> items;
  final int totalCount;

  const ReviewsOverview({
    this.averageRating = 0,
    this.publishedCount = 0,
    this.awaitingReplyCount = 0,
    this.dimensions = const {},
    this.items = const [],
    this.totalCount = 0,
  });

  bool get hasRating => publishedCount > 0;

  ReviewsOverview copyWith({List<BusinessReview>? items, int? awaitingReplyCount}) => ReviewsOverview(
        averageRating: averageRating,
        publishedCount: publishedCount,
        awaitingReplyCount: awaitingReplyCount ?? this.awaitingReplyCount,
        dimensions: dimensions,
        items: items ?? this.items,
        totalCount: totalCount,
      );

  @override
  List<Object?> get props =>
      [averageRating, publishedCount, awaitingReplyCount, dimensions, items, totalCount];
}

/// The reply as the server now holds it.
class ReplyResult extends Equatable {
  final String? text;
  final ReplyStatus? status;

  const ReplyResult({this.text, this.status});

  @override
  List<Object?> get props => [text, status];
}
