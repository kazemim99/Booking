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
      String providerId) async {
    try {
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
                createdAt: r['createdAt'] is String
                    ? DateTime.tryParse(r['createdAt'] as String)
                    : null,
                providerResponse: r['providerResponse'] as String?,
              ))
          .toList();

      return Right(ProviderReviews(
        averageRating: (statistics['averageRating'] as num?)?.toDouble() ?? 0,
        totalReviews: (statistics['totalReviews'] as num?)?.toInt() ?? items.length,
        items: items,
      ));
    } on DioException catch (e) {
      return Left(_failureFor(e, 'دریافت نظرها ناموفق بود'));
    } catch (_) {
      return const Left(ServerFailure('دریافت نظرها ناموفق بود'));
    }
  }

  @override
  Future<Either<Failure, void>> createReview({
    required String bookingId,
    required double rating,
    String? comment,
  }) async {
    try {
      await remoteDataSource.createReview(
          bookingId: bookingId, rating: rating, comment: comment);
      return const Right(null);
    } on DioException catch (e) {
      return Left(_failureFor(e, 'ثبت نظر ناموفق بود'));
    } catch (_) {
      return const Left(ServerFailure('ثبت نظر ناموفق بود'));
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
