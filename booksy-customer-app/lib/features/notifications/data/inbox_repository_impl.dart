import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../core/errors/dio_failure_mapper.dart';
import '../../../core/errors/failures.dart';
import '../domain/inbox_item.dart';
import '../domain/inbox_repository.dart';
import 'notifications_remote_datasource.dart';

class InboxRepositoryImpl implements InboxRepository {
  final NotificationsRemoteDataSource remote;

  InboxRepositoryImpl({required this.remote});

  @override
  Future<Either<Failure, InboxResult>> fetchPage({int pageNumber = 1, int pageSize = 20}) =>
      _guard(() async {
        final json = await remote.getInbox(pageNumber: pageNumber, pageSize: pageSize);
        final raw = json['items'];
        final items = raw is List
            ? raw
                .whereType<Map>()
                .map((m) => InboxItem.fromJson(Map<String, dynamic>.from(m)))
                .toList()
            : <InboxItem>[];
        return InboxResult(
          items: items,
          totalCount: (json['totalCount'] as num?)?.toInt() ?? items.length,
          unreadCount: (json['unreadCount'] as num?)?.toInt() ?? 0,
        );
      });

  @override
  Future<Either<Failure, int>> unreadCount() => _guard(remote.getUnreadCount);

  @override
  Future<Either<Failure, Unit>> markRead(String id) => _guard(() async {
        await remote.markRead(id);
        return unit;
      });

  @override
  Future<Either<Failure, Unit>> markAllRead() => _guard(() async {
        await remote.markAllRead();
        return unit;
      });

  Future<Either<Failure, T>> _guard<T>(Future<T> Function() call) async {
    try {
      return Right(await call());
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }
}
