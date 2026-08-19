import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../core/errors/failures.dart';
import '../domain/invitation_repository.dart';
import '../domain/invitation_summary.dart';
import 'invitation_api_service.dart';

class InvitationRepositoryImpl implements InvitationRepository {
  final InvitationApiService _api;
  InvitationRepositoryImpl(this._api);

  @override
  Future<Either<Failure, InvitationSummary?>> fetchSummary(
      String invitationId) async {
    try {
      final json = await _api.getSummary(invitationId);
      if (json == null || json.isEmpty) return const Right(null);
      return Right(InvitationSummary.fromJson(json));
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return const Right(null);
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }

  @override
  Future<Either<Failure, void>> accept(String invitationId) async {
    try {
      await _api.accept(invitationId);
      return const Right(null);
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
    final code = error.response?.statusCode;
    final message = _serverMessage(error) ?? error.message ?? 'خطای سرور';
    switch (code) {
      case 400:
      case 422:
        return ValidationFailure(message);
      case 401:
        return AuthFailure(message);
      case 404:
        return NotFoundFailure(message);
      default:
        return ServerFailure(message);
    }
  }

  /// Surfaces the backend's Persian domain message (e.g. self-invite / already a
  /// member) when present, so the user sees why acceptance was refused.
  String? _serverMessage(DioException error) {
    final data = error.response?.data;
    if (data is Map) {
      final msg = data['message'] ?? data['error'] ?? data['detail'];
      if (msg is String && msg.trim().isNotEmpty) return msg;
    }
    return null;
  }
}
