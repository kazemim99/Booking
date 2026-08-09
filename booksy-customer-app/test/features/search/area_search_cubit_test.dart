import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/core/location/geocoding_service.dart';
import 'package:booksy_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:booksy_customer_app/features/search/domain/repositories/search_repository.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/area_search_cubit.dart';

ProviderSummary _provider(String id) => ProviderSummary(
      id: id,
      name: 'Provider $id',
      rating: 4.5,
      reviewCount: 10,
      startingPrice: 100,
      isOpen: true,
    );

class _FakeGeocodingService implements GeocodingService {
  final GeoCoordinates? result;
  String? capturedTerm;
  _FakeGeocodingService(this.result);
  @override
  Future<GeoCoordinates?> geocode(String term) async {
    capturedTerm = term;
    return result;
  }
}

class _FakeSearchRepository implements SearchRepository {
  final Either<Failure, List<ProviderSummary>> response;
  double? capturedLat;
  double? capturedLng;
  String? capturedSortBy;
  var queried = false;

  _FakeSearchRepository(this.response);

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
    queried = true;
    capturedLat = latitude;
    capturedLng = longitude;
    capturedSortBy = sortBy;
    return response;
  }
}

void main() {
  group('AreaSearchCubit', () {
    test('geocodes the area then distance-searches around it', () async {
      final geo = _FakeGeocodingService(const GeoCoordinates(35.7, 51.4));
      final repo = _FakeSearchRepository(Right([_provider('1')]));
      final cubit = AreaSearchCubit(
        geocodingService: geo,
        repository: repo,
        radiusKm: 5,
      );

      await cubit.searchArea('ونک');

      expect(geo.capturedTerm, 'ونک');
      expect(cubit.state.status, AreaSearchStatus.loaded);
      expect(cubit.state.areaName, 'ونک');
      expect(repo.capturedLat, 35.7);
      expect(repo.capturedLng, 51.4);
      expect(repo.capturedSortBy, 'distance');
    });

    test('unknown area yields areaNotFound and never queries providers',
        () async {
      final repo = _FakeSearchRepository(const Right([]));
      final cubit = AreaSearchCubit(
        geocodingService: _FakeGeocodingService(null),
        repository: repo,
      );

      await cubit.searchArea('nowhere-xyz');

      expect(cubit.state.status, AreaSearchStatus.areaNotFound);
      expect(repo.queried, isFalse);
    });

    test('found area with no providers yields empty', () async {
      final cubit = AreaSearchCubit(
        geocodingService: _FakeGeocodingService(const GeoCoordinates(1, 2)),
        repository: _FakeSearchRepository(const Right([])),
      );

      await cubit.searchArea('ونک');

      expect(cubit.state.status, AreaSearchStatus.empty);
    });

    test('blank query is a no-op', () async {
      final repo = _FakeSearchRepository(const Right([]));
      final cubit = AreaSearchCubit(
        geocodingService: _FakeGeocodingService(const GeoCoordinates(1, 2)),
        repository: repo,
      );

      await cubit.searchArea('   ');

      expect(cubit.state.status, AreaSearchStatus.initial);
      expect(repo.queried, isFalse);
    });

    test('search failure surfaces an error', () async {
      final cubit = AreaSearchCubit(
        geocodingService: _FakeGeocodingService(const GeoCoordinates(1, 2)),
        repository: _FakeSearchRepository(
          const Left(NetworkFailure('offline')),
        ),
      );

      await cubit.searchArea('ونک');

      expect(cubit.state.status, AreaSearchStatus.error);
      expect(cubit.state.errorMessage, 'offline');
    });
  });
}
