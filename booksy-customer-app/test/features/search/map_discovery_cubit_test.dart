import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/core/location/geocoding_service.dart';
import 'package:booksy_customer_app/core/location/location_service.dart';
import 'package:booksy_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:booksy_customer_app/features/search/domain/repositories/search_repository.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/map_discovery_cubit.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

/// Behaviour of the map discovery screen.
///
/// This suite is the successor to `area_search_cubit_test.dart`, which was removed along with
/// `AreaSearchCubit` when «جستجو در محله» stopped being its own destination. Everything that cubit
/// protected — geocoding a typed area, handling a place that cannot be found, surfacing failures — is
/// re-covered here, alongside the map's own concerns.
///
/// Two rules matter most and are asserted repeatedly:
///   * the map ALWAYS has a centre and always tries to show providers, so a permission refusal or a
///     failed geocode degrades in place rather than emptying the screen;
///   * the category sent to the API is the `ServiceCategory` ENUM NAME, never a Persian label — the
///     backend answers HTTP 500 for a Persian value.
void main() {
  const parsabadLat = MapDiscoveryCubit.fallbackLatitude;
  const parsabadLon = MapDiscoveryCubit.fallbackLongitude;

  ProviderSummary provider(String id, {double? lat, double? lon, double? distance}) => ProviderSummary(
        id: id,
        name: 'سالن $id',
        rating: 0,
        reviewCount: 0,
        startingPrice: 0,
        isOpen: true,
        distance: distance,
        latitude: lat ?? parsabadLat,
        longitude: lon ?? parsabadLon,
      );

  late _FakeRepository repository;
  late _FakeLocationService location;
  late _FakeGeocodingService geocoding;

  MapDiscoveryCubit build() => MapDiscoveryCubit(
        repository: repository,
        locationService: location,
        geocodingService: geocoding,
      );

  setUp(() {
    repository = _FakeRepository();
    location = _FakeLocationService();
    geocoding = _FakeGeocodingService();
  });

  group('start', () {
    test('centres on the device position and loads providers around it', () async {
      location.result = const LocationSuccess(39.70, 47.95);
      repository.result = Right([provider('a'), provider('b')]);

      final cubit = build();
      await cubit.start();

      expect(cubit.state.status, MapDiscoveryStatus.loaded);
      expect(cubit.state.providers, hasLength(2));
      expect(cubit.state.latitude, 39.70);
      expect(cubit.state.longitude, 47.95);
      expect(repository.lastLatitude, 39.70);
      expect(repository.lastLongitude, 47.95);
      expect(cubit.state.notice, MapNotice.none);

      await cubit.close();
    });

    test('falls back to the launch city when permission is denied, and still shows providers', () async {
      location.result = const LocationPermissionDenied();
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();

      // The screen must never be a dead end just because location was refused.
      expect(cubit.state.latitude, parsabadLat);
      expect(cubit.state.longitude, parsabadLon);
      expect(cubit.state.providers, hasLength(1));
      expect(cubit.state.status, MapDiscoveryStatus.loaded);
      expect(cubit.state.notice, MapNotice.permissionDenied);

      await cubit.close();
    });

    test('falls back when the OS location service is switched off', () async {
      location.result = const LocationServiceDisabled();
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();

      expect(cubit.state.latitude, parsabadLat);
      expect(cubit.state.notice, MapNotice.serviceDisabled);
      expect(cubit.state.providers, isNotEmpty);

      await cubit.close();
    });

    test('reports empty separately from error when nothing is nearby', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = const Right(<ProviderSummary>[]);

      final cubit = build();
      await cubit.start();

      expect(cubit.state.status, MapDiscoveryStatus.empty);
      expect(cubit.state.providers, isEmpty);

      await cubit.close();
    });

    test('surfaces a failure as an error state carrying the message', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = const Left(ServerFailure('قطع ارتباط'));

      final cubit = build();
      await cubit.start();

      expect(cubit.state.status, MapDiscoveryStatus.error);
      expect(cubit.state.errorMessage, 'قطع ارتباط');

      await cubit.close();
    });
  });

  group('retry', () {
    test('re-queries and recovers after an error', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = const Left(ServerFailure('خطا'));

      final cubit = build();
      await cubit.start();
      expect(cubit.state.status, MapDiscoveryStatus.error);

      repository.result = Right([provider('a')]);
      await cubit.retry();

      expect(cubit.state.status, MapDiscoveryStatus.loaded);
      expect(cubit.state.providers, hasLength(1));

      await cubit.close();
    });
  });

  group('category filter', () {
    test('passes the ServiceCategory ENUM NAME to the API, never a Persian label', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();
      await cubit.selectCategory('Barbershop');

      expect(cubit.state.category, 'Barbershop');
      expect(repository.lastCategory, 'Barbershop');
      // A Persian label here is the defect that made the backend answer HTTP 500.
      expect(repository.lastCategory, isNot(contains('آرایش')));

      await cubit.close();
    });

    test('clearing the filter sends no category at all', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();
      await cubit.selectCategory('Spa');
      await cubit.selectCategory(null);

      expect(cubit.state.category, isNull);
      expect(repository.lastCategory, isNull);

      await cubit.close();
    });
  });

  group('search this area', () {
    test('re-queries around the new map centre', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();

      repository.result = Right([provider('b'), provider('c')]);
      await cubit.searchVisibleArea(latitude: 39.70, longitude: 47.99, radiusKm: 3);

      expect(repository.lastLatitude, 39.70);
      expect(repository.lastLongitude, 47.99);
      expect(repository.lastRadiusKm, 3);
      expect(cubit.state.providers, hasLength(2));

      await cubit.close();
    });

    test('keeps the active category when re-searching an area', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();
      await cubit.selectCategory('NailSalon');
      await cubit.searchVisibleArea(latitude: 39.66, longitude: 47.93, radiusKm: 2);

      expect(repository.lastCategory, 'NailSalon');

      await cubit.close();
    });
  });

  group('city search', () {
    test('geocodes the typed area and moves the map there', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();

      geocoding.result = const GeoCoordinates(38.25, 48.29);
      repository.result = Right([provider('z')]);
      await cubit.searchArea('اردبیل');

      expect(cubit.state.latitude, 38.25);
      expect(cubit.state.longitude, 48.29);
      expect(repository.lastLatitude, 38.25);
      expect(cubit.state.notice, MapNotice.none);

      await cubit.close();
    });

    test('an unknown place leaves the map where it was and says so', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();

      geocoding.result = null; // no match
      await cubit.searchArea('جایی که وجود ندارد');

      // Not moving is the point: a failed lookup must not throw the customer somewhere arbitrary.
      expect(cubit.state.latitude, parsabadLat);
      expect(cubit.state.longitude, parsabadLon);
      expect(cubit.state.notice, MapNotice.areaNotFound);
      expect(cubit.state.providers, isNotEmpty);

      await cubit.close();
    });

    test('a blank term is ignored rather than geocoded', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();
      geocoding.calls = 0;

      await cubit.searchArea('   ');

      expect(geocoding.calls, 0);

      await cubit.close();
    });
  });

  group('selection', () {
    test('selecting a provider exposes it and its carousel index', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a'), provider('b'), provider('c')]);

      final cubit = build();
      await cubit.start();

      cubit.selectProvider('b');

      expect(cubit.state.selectedProviderId, 'b');
      expect(cubit.state.selectedIndex, 1);

      await cubit.close();
    });

    test('clearing the selection is allowed', () async {
      location.result = const LocationSuccess(parsabadLat, parsabadLon);
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();

      cubit.selectProvider('a');
      cubit.selectProvider(null);

      expect(cubit.state.selectedProviderId, isNull);

      await cubit.close();
    });
  });

  group('use my location', () {
    test('recentres on the device and reloads', () async {
      location.result = const LocationPermissionDenied();
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();
      expect(cubit.state.latitude, parsabadLat);

      location.result = const LocationSuccess(39.72, 47.88);
      await cubit.useMyLocation();

      expect(cubit.state.latitude, 39.72);
      expect(cubit.state.longitude, 47.88);
      expect(repository.lastLatitude, 39.72);

      await cubit.close();
    });

    test('a still-denied permission reports it without losing the map', () async {
      location.result = const LocationPermissionDenied();
      repository.result = Right([provider('a')]);

      final cubit = build();
      await cubit.start();
      cubit.dismissNotice();

      await cubit.useMyLocation();

      expect(cubit.state.notice, MapNotice.permissionDenied);
      expect(cubit.state.providers, isNotEmpty);
      expect(cubit.state.latitude, parsabadLat);

      await cubit.close();
    });
  });

  test('dismissing a notice clears it', () async {
    location.result = const LocationPermissionDenied();
    repository.result = Right([provider('a')]);

    final cubit = build();
    await cubit.start();
    expect(cubit.state.notice, MapNotice.permissionDenied);

    cubit.dismissNotice();

    expect(cubit.state.notice, MapNotice.none);

    await cubit.close();
  });
}

class _FakeRepository implements SearchRepository {
  Either<Failure, List<ProviderSummary>> result = const Right(<ProviderSummary>[]);

  double? lastLatitude;
  double? lastLongitude;
  double? lastRadiusKm;
  String? lastCategory;
  int calls = 0;

  @override
  Future<Either<Failure, List<ProviderSummary>>> providersByLocation({
    required double latitude,
    required double longitude,
    double radiusKm = 10,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    calls++;
    lastLatitude = latitude;
    lastLongitude = longitude;
    lastRadiusKm = radiusKm;
    lastCategory = serviceCategory;
    return result;
  }

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
  }) async =>
      const Right(<ProviderSummary>[]);
}

class _FakeLocationService implements LocationService {
  LocationResult result = const LocationPermissionDenied();

  @override
  Future<LocationResult> currentPosition() async => result;
}

class _FakeGeocodingService implements GeocodingService {
  GeoCoordinates? result;
  int calls = 0;

  @override
  Future<GeoCoordinates?> geocode(String term) async {
    calls++;
    return result;
  }
}
