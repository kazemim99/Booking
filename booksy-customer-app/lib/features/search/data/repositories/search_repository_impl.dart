import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/dio_failure_mapper.dart';
import '../../../../core/errors/failures.dart';
import '../../../home/data/models/provider_model.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../../domain/repositories/search_repository.dart';
import '../datasources/search_remote_datasource.dart';
import '../models/provider_location_model.dart';

class SearchRepositoryImpl implements SearchRepository {
  final SearchRemoteDataSource remoteDataSource;

  SearchRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, List<ProviderSummary>>> searchProviders({
    String? searchTerm,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 20,
    double? latitude,
    double? longitude,
    double? radiusKm,
    String sortBy = 'rating',
  }) async {
    try {
      final dtos = await remoteDataSource.searchProviders(
        searchTerm: searchTerm,
        serviceCategory: serviceCategory,
        pageNumber: pageNumber,
        pageSize: pageSize,
        latitude: latitude,
        longitude: longitude,
        radiusKm: radiusKm,
        sortBy: sortBy,
      );
      return Right(
          await _withAvailability(dtos.map((dto) => dto.toEntity()).toList()));
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return Left(ServerFailure('خطا در جستجو: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, List<ProviderSummary>>> providersByLocation({
    required double latitude,
    required double longitude,
    double radiusKm = 10,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    try {
      final dtos = await remoteDataSource.providersByLocation(
        latitude: latitude,
        longitude: longitude,
        radiusKm: radiusKm,
        category: serviceCategory,
        pageNumber: pageNumber,
        pageSize: pageSize,
      );
      return Right(
          await _withAvailability(dtos.map((dto) => dto.toEntity()).toList()));
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return Left(ServerFailure('خطا در جستجو: ${e.toString()}'));
    }
  }

  /// The server answers for at most this many salons per request.
  static const int _availabilityBatch = 20;

  /// Adds each salon's next free day and free-time count from
  /// `/Providers/availability-summary`, as home does for its rail. Neither
  /// search endpoint carries them, and the cards say «امروز ۵ وقت خالی» only
  /// when they are known. One request covers the first screenful; the rest keep
  /// no free-time line rather than costing a request each.
  ///
  /// Free times are a nicety on a card: any failure here returns the salons as
  /// they were, never an error and never an empty list.
  Future<List<ProviderSummary>> _withAvailability(
      List<ProviderSummary> providers) async {
    if (providers.isEmpty) return providers;
    try {
      final rows = await remoteDataSource.getAvailabilitySummary(
        providers.take(_availabilityBatch).map((p) => p.id).toList(),
      );
      final byId = {
        for (final row in rows) row['providerId']?.toString(): row,
      };
      return providers.map((p) {
        final row = byId[p.id];
        if (row == null) return p;
        final date = row['date'];
        return p.withAvailability(
          date is String ? DateTime.tryParse(date) : null,
          (row['freeSlotCount'] as num?)?.toInt() ?? 0,
        );
      }).toList();
    } catch (_) {
      return providers;
    }
  }
}
