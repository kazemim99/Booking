import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../core/errors/failures.dart';
import '../../auth/domain/repositories/auth_repository.dart';
import '../domain/promotion.dart';
import '../domain/promotions_repository.dart';
import 'promotions_api_service.dart';

class PromotionsRepositoryImpl implements PromotionsRepository {
  final PromotionsApiService _api;
  final AuthRepository _auth;

  PromotionsRepositoryImpl(this._api, this._auth);

  @override
  Future<Either<Failure, List<Promotion>>> list() => _withProviderId(
      (providerId) => _guard(() async => (await _api.list(providerId)).map(Promotion.fromJson).toList()));

  @override
  Future<Either<Failure, Promotion>> create(PromotionDraft draft) => _withProviderId(
      (providerId) => _guard(() async => Promotion.fromJson(await _api.create(providerId, draft.toJson()))));

  @override
  Future<Either<Failure, Promotion>> update(String promotionId, PromotionDraft draft) => _withProviderId(
      (providerId) => _guard(() async => Promotion.fromJson(await _api.update(providerId, promotionId, draft.toJson()))));

  @override
  Future<Either<Failure, Promotion>> change(String promotionId, PromotionAction action) => _withProviderId(
      (providerId) => _guard(() async => Promotion.fromJson(await _api.change(providerId, promotionId, action.name))));

  @override
  Future<Either<Failure, List<CampaignOffer>>> campaigns() => _withProviderId(
      (providerId) => _guard(() async => (await _api.campaigns(providerId)).map(CampaignOffer.fromJson).toList()));

  @override
  Future<Either<Failure, CampaignOffer>> setJoined(String campaignId, {required bool join}) =>
      _withProviderId((providerId) => _guard(() async => CampaignOffer.fromJson(
          join ? await _api.join(providerId, campaignId) : await _api.leave(providerId, campaignId))));

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

  /// The server's own Persian reason ("درصد تخفیف باید بین ۱ تا ۹۰ باشد.") beats a status code.
  Failure _mapDioError(DioException error) {
    if (error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout ||
        error.type == DioExceptionType.sendTimeout) {
      return NetworkFailure(error.message ?? 'خطای شبکه');
    }
    final data = error.response?.data;
    Object? serverMessage;
    if (data is Map) {
      final errors = data['error'] is Map ? (data['error'] as Map)['errors'] : null;
      // A field error's own sentence, without the "Validation failed for property …" prefix of the envelope message.
      if (errors is Map && errors.values.isNotEmpty && errors.values.first is List && (errors.values.first as List).isNotEmpty) {
        serverMessage = (errors.values.first as List).first;
      } else {
        serverMessage = data['message'] ?? (data['error'] is Map ? data['error']['message'] : null);
      }
    }
    final message = serverMessage is String && serverMessage.isNotEmpty ? serverMessage : (error.message ?? 'خطای سرور');
    switch (error.response?.statusCode) {
      case 400:
        return ValidationFailure(message);
      case 401:
        return AuthFailure(message);
      case 404:
        return NotFoundFailure(message);
      default:
        return ServerFailure(message);
    }
  }
}
