import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/dio_failure_mapper.dart';
import '../../../../core/errors/failures.dart';
import '../../../home/data/models/provider_model.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../../domain/repositories/search_repository.dart';
import '../datasources/search_remote_datasource.dart';

class SearchRepositoryImpl implements SearchRepository {
  final SearchRemoteDataSource remoteDataSource;

  SearchRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, List<ProviderSummary>>> searchProviders({
    String? searchTerm,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    try {
      final dtos = await remoteDataSource.searchProviders(
        searchTerm: searchTerm,
        serviceCategory: serviceCategory,
        pageNumber: pageNumber,
        pageSize: pageSize,
      );
      return Right(dtos.map((dto) => dto.toEntity()).toList());
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return Left(ServerFailure('خطا در جستجو: ${e.toString()}'));
    }
  }
}
