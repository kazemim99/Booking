import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../core/errors/failures.dart';
import '../../auth/domain/repositories/auth_repository.dart';
import '../domain/business_review.dart';
import '../domain/reviews_repository.dart';
import 'reviews_api_service.dart';

class ReviewsRepositoryImpl implements ReviewsRepository {
  final ReviewsApiService _api;
  final AuthRepository _auth;

  ReviewsRepositoryImpl(this._api, this._auth);

  @override
  Future<Either<Failure, ReviewsOverview>> load() => _withProviderId((providerId) => _guard(() async {
        final inboxF = _api.getInbox(providerId);
        final statisticsF = _api.getStatistics(providerId);
        final inbox = await inboxF;
        final statistics = await statisticsF;

        final items = (inbox['items'] is List ? inbox['items'] as List : const [])
            .whereType<Map>()
            .map((m) => BusinessReview.fromJson(Map<String, dynamic>.from(m)))
            .toList();

        return ReviewsOverview(
          averageRating: (statistics['averageRating'] as num?)?.toDouble() ?? 0,
          publishedCount: (statistics['totalReviews'] as num?)?.toInt() ?? 0,
          awaitingReplyCount: (inbox['awaitingReplyCount'] as num?)?.toInt() ??
              items.where((r) => r.awaitingReply).length,
          dimensions: {
            for (final d in ReviewDimension.values) d: ?_dimension(statistics[d.statisticsKey]),
          },
          items: items,
          totalCount: (inbox['totalCount'] as num?)?.toInt() ?? items.length,
        );
      }));

  @override
  Future<Either<Failure, ReplyResult>> reply(String reviewId, String text, {required bool edit}) =>
      _guard(() async {
        final trimmed = text.trim();
        final json = edit ? await _api.editReply(reviewId, trimmed) : await _api.addReply(reviewId, trimmed);
        return ReplyResult(
          text: json['providerResponse'] as String? ?? trimmed,
          status: ReplyStatus.parse(json['replyModerationStatus']) ?? ReplyStatus.pending,
        );
      });

  @override
  Future<Either<Failure, void>> removeReply(String reviewId) => _guard(() => _api.removeReply(reviewId));

  static DimensionAverage? _dimension(Object? json) {
    if (json is! Map) return null;
    final average = json['average'];
    final count = (json['count'] as num?)?.toInt() ?? 0;
    return average is num && count > 0 ? DimensionAverage(average: average.toDouble(), count: count) : null;
  }

  Future<Either<Failure, T>> _withProviderId<T>(Future<Either<Failure, T>> Function(String providerId) body) async {
    final sessionOr = await _auth.getCurrentSession();
    return sessionOr.fold(Left.new, (session) {
      final providerId = session?.providerId;
      if (providerId == null) {
        return Future.value(const Left<Failure, Never>(AuthFailure('نشست معتبر یافت نشد')));
      }
      return body(providerId);
    });
  }

  Future<Either<Failure, T>> _guard<T>(Future<T> Function() call) async {
    try {
      return Right(await call());
    } on DioException catch (e) {
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }

  /// The server's own reason where it gave one ("این نظر هنوز منتشر نشده است" beats a status code).
  Failure _mapDioError(DioException error) {
    if (error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout ||
        error.type == DioExceptionType.sendTimeout) {
      return NetworkFailure(error.message ?? 'خطای شبکه');
    }
    final data = error.response?.data;
    final serverMessage = data is Map
        ? (data['message'] ?? (data['error'] is Map ? data['error']['message'] : null))
        : null;
    final message = serverMessage is String && serverMessage.isNotEmpty
        ? serverMessage
        : (error.message ?? 'خطای سرور');
    switch (error.response?.statusCode) {
      case 401:
        return AuthFailure(message);
      case 404:
        return NotFoundFailure(message);
      default:
        return ServerFailure(message);
    }
  }
}
