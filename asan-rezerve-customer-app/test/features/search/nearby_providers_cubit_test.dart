import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/core/location/location_service.dart';
import 'package:asan_rezerve_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:asan_rezerve_customer_app/features/search/domain/repositories/search_repository.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/bloc/nearby_providers_cubit.dart';

ProviderSummary _provider(String id, {double? distance}) => ProviderSummary(
      id: id,
      name: 'Provider $id',
      rating: 4.5,
      reviewCount: 10,
      distance: distance,
      startingPrice: 100,
      isOpen: true,
    );

class _FakeLocationService implements LocationService {
  final LocationResult result;
  const _FakeLocationService(this.result);
  @override
  Future<LocationResult> currentPosition() async => result;
}

/// Captures the geo arguments so the distance-sort contract can be asserted.
class _FakeSearchRepository implements SearchRepository {
  final Either<Failure, List<ProviderSummary>> response;
  double? capturedLat;
  double? capturedLng;
  double? capturedRadius;
  String? capturedSortBy;

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
    capturedLat = latitude;
    capturedLng = longitude;
    capturedRadius = radiusKm;
    capturedSortBy = sortBy;
    return response;
  }

  bool byLocationCalled = false;

  @override
  Future<Either<Failure, List<ProviderSummary>>> providersByLocation({
    required double latitude,
    required double longitude,
    double radiusKm = 10,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    byLocationCalled = true;
    capturedLat = latitude;
    capturedLng = longitude;
    capturedRadius = radiusKm;
    return response;
  }
}

void main() {
  group('NearbyProvidersCubit', () {
    test('granted location searches by distance and loads results', () async {
      final repo = _FakeSearchRepository(
        Right([_provider('1', distance: 0.5), _provider('2', distance: 1.2)]),
      );
      final cubit = NearbyProvidersCubit(
        locationService: const _FakeLocationService(
          LocationSuccess(35.7, 51.4),
        ),
        repository: repo,
        radiusKm: 8,
      );

      await cubit.load();

      expect(cubit.state.status, NearbyStatus.loaded);
      expect(cubit.state.providers, hasLength(2));
      // /Providers/search drops radiusKm (it has no such parameter) and returns no distance, so the "nearest"
      // list showed the closest salon in the country and could not say how far it was (QA 2026-09-22). The
      // by-location endpoint applies the radius server-side and returns distanceKm.
      expect(repo.byLocationCalled, isTrue);
      expect(repo.capturedLat, 35.7);
      expect(repo.capturedLng, 51.4);
      expect(repo.capturedRadius, 8);
      expect(cubit.state.providers.first.distance, 0.5, reason: 'the card shows how far away it is');
    });

    test('granted location with no results yields empty', () async {
      final cubit = NearbyProvidersCubit(
        locationService: const _FakeLocationService(LocationSuccess(1, 2)),
        repository: _FakeSearchRepository(const Right([])),
      );

      await cubit.load();

      expect(cubit.state.status, NearbyStatus.empty);
    });

    test('permission denied falls back (does not query or block)', () async {
      final repo = _FakeSearchRepository(const Right([]));
      final cubit = NearbyProvidersCubit(
        locationService: const _FakeLocationService(
          LocationPermissionDenied(),
        ),
        repository: repo,
      );

      await cubit.load();

      expect(cubit.state.status, NearbyStatus.permissionDenied);
      expect(repo.capturedSortBy, isNull); // never queried the backend
    });

    test('location service disabled surfaces the disabled status', () async {
      final cubit = NearbyProvidersCubit(
        locationService: const _FakeLocationService(
          LocationServiceDisabled(),
        ),
        repository: _FakeSearchRepository(const Right([])),
      );

      await cubit.load();

      expect(cubit.state.status, NearbyStatus.serviceDisabled);
    });

    test('search failure surfaces an error with its message', () async {
      final cubit = NearbyProvidersCubit(
        locationService: const _FakeLocationService(LocationSuccess(1, 2)),
        repository: _FakeSearchRepository(
          const Left(NetworkFailure('offline')),
        ),
      );

      await cubit.load();

      expect(cubit.state.status, NearbyStatus.error);
      expect(cubit.state.errorMessage, 'offline');
    });
  
    test('a fix too coarse to be a street is not called "nearest"', () async {
      // A VPN or IP fix puts the customer in another city — measured in پارس‌آباد with a Dubai exit. The map
      // cubit already refuses these; the nearby list used to search from them anyway.
      final repo = _FakeSearchRepository(Right([_provider('1', distance: 0.5)]));
      final cubit = NearbyProvidersCubit(
        locationService: const _FakeLocationService(
          LocationSuccess(35.7, 51.4, accuracyMeters: 40000),
        ),
        repository: repo,
      );

      await cubit.load();

      expect(cubit.state.status, NearbyStatus.imprecise);
      expect(repo.byLocationCalled, isFalse, reason: 'nothing is asked of the server for a guess this coarse');
    });

    test('a normal GPS fix is trusted', () async {
      final repo = _FakeSearchRepository(Right([_provider('1', distance: 0.5)]));
      final cubit = NearbyProvidersCubit(
        locationService: const _FakeLocationService(
          LocationSuccess(35.7, 51.4, accuracyMeters: 25),
        ),
        repository: repo,
      );

      await cubit.load();

      expect(cubit.state.status, NearbyStatus.loaded);
    });

  });
}
