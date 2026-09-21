import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../core/errors/failures.dart';
import '../domain/inbox_item.dart';
import '../domain/inbox_repository.dart';
import 'notification_api_service.dart';

class InboxRepositoryImpl implements InboxRepository {
  final NotificationApiService _api;
  InboxRepositoryImpl(this._api);

  @override
  Future<Either<Failure, InboxResult>> fetchPage({int pageNumber = 1, int pageSize = 20}) =>
      _guard(() async {
        final json = await _api.getInbox(pageNumber: pageNumber, pageSize: pageSize);
        final raw = json['items'];
        final items = raw is List
            ? raw.whereType<Map>().map((m) => InboxItem.fromJson(Map<String, dynamic>.from(m))).toList()
            : <InboxItem>[];
        return InboxResult(
          items: items,
          totalCount: (json['totalCount'] as num?)?.toInt() ?? items.length,
          unreadCount: (json['unreadCount'] as num?)?.toInt() ?? 0,
        );
      });

  @override
  Future<Either<Failure, int>> unreadCount() => _guard(_api.getUnreadCount);

  @override
  Future<Either<Failure, void>> markRead(String id) => _guard(() => _api.markRead(id));

  @override
  Future<Either<Failure, void>> markAllRead() => _guard(_api.markAllRead);

  Future<Either<Failure, T>> _guard<T>(Future<T> Function() call) async {
    try {
      return Right(await call());
    } on DioException catch (e) {
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }

  Failure _mapDioError(DioException error) {
    if (error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout ||
        error.type == DioExceptionType.sendTimeout) {
      return NetworkFailure(error.message ?? 'خطای شبکه');
    }
    final message = error.message ?? 'خطای سرور';
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
