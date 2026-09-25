import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../../core/errors/dio_failure_mapper.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/errors/server_message.dart';
import '../../domain/entities/review.dart';
import '../../domain/repositories/review_repository.dart';
import '../datasources/review_remote_datasource.dart';

class ReviewRepositoryImpl implements ReviewRepository {
  final ReviewRemoteDataSource remoteDataSource;

  ReviewRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, ProviderReviews>> getProviderReviews(
      String providerId, {int page = 1}) =>
      _guard('دریافت نظرها ناموفق بود', () async {
        final data =
            await remoteDataSource.getProviderReviews(providerId, page: page);
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
                  isVerified: r['isVerified'] == true,
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
          distribution: _distribution(statistics['ratingDistribution']),
          page: (reviews['pageNumber'] as num?)?.toInt() ?? page,
          hasMore: reviews['hasNextPage'] == true,
        );
      });

  @override
  Future<Either<Failure, void>> createReview({
    required String bookingId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
    bool showName = true,
  }) =>
      _guard('ثبت نظر ناموفق بود', () => remoteDataSource.createReview(
          bookingId: bookingId, body: _body(rating, comment, dimensions, showName)));

  @override
  Future<Either<Failure, void>> editReview({
    required String reviewId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
    bool showName = true,
  }) =>
      _guard('ویرایش نظر ناموفق بود', () => remoteDataSource.editReview(
          reviewId: reviewId, body: _body(rating, comment, dimensions, showName)));

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
                  // Absent on an older server: names were always shown.
                  showName: r['showName'] is bool ? r['showName'] as bool : true,
                ))
            .toList();
      });

  /// Create and edit share a body: the overall star (the server derives it
  /// when absent, but an older one requires it), the words if any, the
  /// dimensions the customer rated — never a zero for the rest — and whether
  /// their name signs it.
  static Map<String, dynamic> _body(double rating, String? comment,
          Map<ReviewDimension, double> dimensions, bool showName) =>
      {
        'rating': rating,
        'showName': showName,
        if (comment != null && comment.trim().isNotEmpty) 'comment': comment.trim(),
        for (final e in dimensions.entries) e.key.wireName: e.value,
      };

  static DateTime? _date(Object? value) =>
      value is String ? DateTime.tryParse(value) : null;

  static const _starKeys = {
    5: 'fiveStarCount',
    4: 'fourStarCount',
    3: 'threeStarCount',
    2: 'twoStarCount',
    1: 'oneStarCount',
  };

  static Map<int, int> _distribution(Object? json) => json is Map
      ? {
          for (final e in _starKeys.entries)
            e.key: (json[e.value] as num?)?.toInt() ?? 0,
        }
      : const {};

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

  /// The server's own reason where it gave one: «برای این نوبت قبلاً نظر ثبت
  /// کرده‌اید» reads better than a status code. It was read from `message`
  /// only, which the create-review refusals do not carry (they answer
  /// `errors: [{ message }]`), so every refusal read «ثبت نظر ناموفق بود».
  /// Offline and a lost session say so, as everywhere else.
  Failure _failureFor(DioException e, String fallback) {
    final mapped = mapDioFailure(e);
    if (mapped is NetworkFailure) return mapped;
    if (e.response?.statusCode == 401) return mapped;
    return ServerFailure(serverMessage(e.response?.data) ?? fallback);
  }
}
