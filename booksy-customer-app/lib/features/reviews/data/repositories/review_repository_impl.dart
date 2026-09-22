import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../../core/errors/failures.dart';
import '../../domain/entities/review.dart';
import '../../domain/repositories/review_repository.dart';
import '../datasources/review_remote_datasource.dart';

class ReviewRepositoryImpl implements ReviewRepository {
  final ReviewRemoteDataSource remoteDataSource;

  ReviewRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, ProviderReviews>> getProviderReviews(
      String providerId) =>
      _guard('دریافت نظرها ناموفق بود', () async {
        final data = await remoteDataSource.getProviderReviews(providerId);
        final statistics = data['statistics'] as Map<String, dynamic>? ?? const {};
        final reviews = data['reviews'] as Map<String, dynamic>? ?? const {};
        final items = (reviews['items'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map((r) => Review(
                  id: (r['reviewId'] ?? r['id'] ?? '').toString(),
                  customerName: r['customerName'] as String? ?? '',
                  rating: (r['rating'] as num?)?.toDouble() ?? 0,
                  comment: r['comment'] as String?,
                  createdAt: _date(r['createdAt']),
                  providerResponse: r['providerResponse'] as String?,
                  dimensions: ReviewDimension.readFrom(r),
                  helpfulCount: (r['helpfulCount'] as num?)?.toInt() ?? 0,
                  notHelpfulCount: (r['notHelpfulCount'] as num?)?.toInt() ?? 0,
                  myVote: ReviewVote.parse(r['myVote']),
                ))
            .toList();

        return ProviderReviews(
          averageRating: (statistics['averageRating'] as num?)?.toDouble() ?? 0,
          totalReviews:
              (statistics['totalReviews'] as num?)?.toInt() ?? items.length,
          items: items,
          dimensions: {
            for (final d in ReviewDimension.values)
              if (_dimension(statistics[d.statisticsKey]) case final avg?) d: avg,
          },
        );
      });

  @override
  Future<Either<Failure, void>> createReview({
    required String bookingId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
  }) =>
      _guard('ثبت نظر ناموفق بود', () => remoteDataSource.createReview(
          bookingId: bookingId, body: _body(rating, comment, dimensions)));

  @override
  Future<Either<Failure, void>> editReview({
    required String reviewId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
  }) =>
      _guard('ویرایش نظر ناموفق بود', () => remoteDataSource.editReview(
          reviewId: reviewId, body: _body(rating, comment, dimensions)));

  @override
  Future<Either<Failure, ReviewVoteResult>> vote(
          String reviewId, bool isHelpful) =>
      _guard('ثبت رأی ناموفق بود', () async {
        final data = await remoteDataSource.vote(reviewId, isHelpful);
        return ReviewVoteResult(
          helpfulCount: (data['helpfulCount'] as num?)?.toInt() ?? 0,
          notHelpfulCount: (data['notHelpfulCount'] as num?)?.toInt() ?? 0,
          myVote: ReviewVote.parse(data['myVote']),
        );
      });

  @override
  Future<Either<Failure, List<MyReview>>> getMyReviews() =>
      _guard('دریافت نظرهای شما ناموفق بود', () async {
        final data = await remoteDataSource.getMyReviews();
        return (data['items'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map((r) => MyReview(
                  id: (r['reviewId'] ?? '').toString(),
                  providerId: r['providerId'] as String?,
                  providerName: r['providerName'] as String?,
                  providerLogoUrl: r['providerLogoUrl'] as String?,
                  serviceName: r['serviceName'] as String?,
                  rating: (r['rating'] as num?)?.toDouble() ?? 0,
                  dimensions: ReviewDimension.readFrom(r),
                  comment: r['comment'] as String?,
                  status: ReviewModerationStatus.parse(r['moderationStatus']),
                  moderationReason: r['moderationReason'] as String?,
                  // The author sees the salon's reply only once it is public.
                  providerResponse: '${r['replyModerationStatus']}'.toLowerCase() ==
                          'published'
                      ? r['providerResponse'] as String?
                      : null,
                  createdAt: _date(r['createdAt']),
                  editedAt: _date(r['editedAt']),
                  canEdit: r['canEdit'] == true,
                ))
            .toList();
      });

  /// Create and edit share a body: the overall star, the words if any, and only
  /// the dimensions the customer actually rated — never a zero for the rest.
  static Map<String, dynamic> _body(
          double rating, String? comment, Map<ReviewDimension, double> dimensions) =>
      {
        'rating': rating,
        if (comment != null && comment.trim().isNotEmpty) 'comment': comment.trim(),
        for (final e in dimensions.entries) e.key.wireName: e.value,
      };

  static DateTime? _date(Object? value) =>
      value is String ? DateTime.tryParse(value) : null;

  static DimensionAverage? _dimension(Object? json) {
    if (json is! Map) return null;
    final average = json['average'];
    final count = (json['count'] as num?)?.toInt() ?? 0;
    return average is num && count > 0
        ? DimensionAverage(average: average.toDouble(), count: count)
        : null;
  }

  Future<Either<Failure, T>> _guard<T>(
      String fallback, Future<T> Function() call) async {
    try {
      return Right(await call());
    } on DioException catch (e) {
      return Left(_failureFor(e, fallback));
    } catch (_) {
      return Left(ServerFailure(fallback));
    }
  }

  /// The server's own reason where it gave one: «نظر قبلاً ثبت شده» reads better
  /// than a status code.
  Failure _failureFor(DioException e, String fallback) {
    final data = e.response?.data;
    final message = data is Map
        ? (data['message'] ?? (data['error'] is Map ? data['error']['message'] : null))
        : null;
    return ServerFailure(
        message is String && message.isNotEmpty ? message : fallback);
  }
}
