import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/core/api/models/provider_models.dart';
import 'package:asan_rezerve_customer_app/features/search/data/datasources/search_remote_datasource.dart';
import 'package:asan_rezerve_customer_app/features/search/data/repositories/search_repository_impl.dart';

/// customer-app-ux-review-fixes F.2/F.3: explore and the map show when a salon
/// can next be booked. Neither `/Providers/search` nor `/Providers/by-location`
/// says so, so the repository asks `/Providers/availability-summary` about the
/// salons it found (as home does) — and a failure there must never cost the
/// customer the list itself.

ProviderDto _dto(String id) => ProviderDto.fromJson({
      'id': id,
      'businessName': 'سالن $id',
      'status': 'Active',
      'averageRating': 4.5,
      'totalReviews': 3,
    });

ProviderLocationDto _located(String id, {int? totalReviews}) =>
    ProviderLocationDto.fromJson({
      'id': id,
      'businessName': 'سالن $id',
      'coordinates': {'latitude': 39.6, 'longitude': 47.9},
      'distanceKm': 1.2,
      'averageRating': 4.8,
      if (totalReviews != null) 'totalReviews': totalReviews,
    });

class _FakeRemote extends SearchRemoteDataSource {
  _FakeRemote() : super(serviceCatalogDio: Dio());

  List<ProviderDto> searchResult = const [];
  List<ProviderLocationDto> locationResult = const [];
  List<Map<String, dynamic>> summaryRows = const [];
  Object? summaryError;
  final List<List<String>> summaryCalls = [];

  @override
  Future<List<ProviderDto>> searchProviders({
    String? searchTerm,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 20,
    double? latitude,
    double? longitude,
    double? radiusKm,
    String sortBy = 'rating',
    CancelToken? cancelToken,
  }) async =>
      searchResult;

  @override
  Future<List<ProviderLocationDto>> providersByLocation({
    required double latitude,
    required double longitude,
    double radiusKm = 10,
    String? category,
    int pageNumber = 1,
    int pageSize = 50,
    CancelToken? cancelToken,
  }) async =>
      locationResult;

  @override
  Future<List<Map<String, dynamic>>> getAvailabilitySummary(
      List<String> providerIds) async {
    summaryCalls.add(providerIds);
    if (summaryError != null) throw summaryError!;
    return summaryRows;
  }
}

void main() {
  late _FakeRemote remote;
  late SearchRepositoryImpl repository;

  setUp(() {
    remote = _FakeRemote();
    repository = SearchRepositoryImpl(remoteDataSource: remote);
  });

  group('explore search', () {
    test('carries each salon\'s next free day and free-time count', () async {
      remote.searchResult = [_dto('a'), _dto('b')];
      remote.summaryRows = [
        {'providerId': 'a', 'date': '2026-09-24', 'freeSlotCount': 5},
        {'providerId': 'b', 'date': null, 'freeSlotCount': 0},
      ];

      final result = await repository.searchProviders(searchTerm: 'سالن');
      final providers = result.getOrElse(() => fail('search failed'));

      expect(remote.summaryCalls, [
        ['a', 'b'],
      ]);
      expect(providers[0].nextFreeDate, DateTime(2026, 9, 24));
      expect(providers[0].freeSlotCount, 5);
      expect(providers[1].nextFreeDate, isNull);
      expect(providers[1].freeSlotCount, 0);
      // The search's own facts survive the merge.
      expect(providers[0].name, 'سالن a');
      expect(providers[0].rating, 4.5);
      expect(providers[0].reviewCount, 3);
    });

    test('a failing availability summary still returns every salon', () async {
      remote.searchResult = [_dto('a'), _dto('b')];
      remote.summaryError = DioException(
        requestOptions: RequestOptions(path: '/availability-summary'),
        type: DioExceptionType.connectionError,
      );

      final result = await repository.searchProviders();

      expect(result.isRight(), isTrue);
      final providers = result.getOrElse(() => const []);
      expect(providers.map((p) => p.id), ['a', 'b']);
      expect(providers.every((p) => p.freeSlotCount == 0), isTrue);
    });

    test('asks about a screenful only — the server answers for twenty',
        () async {
      remote.searchResult = [for (var i = 0; i < 25; i++) _dto('p$i')];

      final result = await repository.searchProviders();

      expect(result.getOrElse(() => const []), hasLength(25));
      expect(remote.summaryCalls.single, hasLength(20));
    });

    test('an empty result asks nothing more', () async {
      final result = await repository.searchProviders(searchTerm: 'هیچ');

      expect(result.getOrElse(() => fail('search failed')), isEmpty);
      expect(remote.summaryCalls, isEmpty);
    });
  });

  group('map search', () {
    test('carries free times too', () async {
      remote.locationResult = [_located('m1', totalReviews: 7)];
      remote.summaryRows = [
        {'providerId': 'm1', 'date': '2026-09-23', 'freeSlotCount': 3},
      ];

      final result =
          await repository.providersByLocation(latitude: 39.6, longitude: 47.9);
      final provider = result.getOrElse(() => fail('map search failed')).single;

      expect(provider.freeSlotCount, 3);
      expect(provider.nextFreeDate, DateTime(2026, 9, 23));
      expect(provider.hasCoordinates, isTrue);
      expect(provider.distance, 1.2);
    });

    test('keeps the published review count, so a rated salon is not '
        'called unreviewed', () async {
      remote.locationResult = [_located('m1', totalReviews: 7)];

      final result =
          await repository.providersByLocation(latitude: 39.6, longitude: 47.9);
      final provider = result.getOrElse(() => fail('map search failed')).single;

      expect(provider.rating, 4.8);
      expect(provider.reviewCount, 7);
    });

    test('a failing availability summary still places every pin', () async {
      remote.locationResult = [_located('m1'), _located('m2')];
      remote.summaryError = StateError('summary down');

      final result =
          await repository.providersByLocation(latitude: 39.6, longitude: 47.9);

      expect(result.getOrElse(() => const []).map((p) => p.id), ['m1', 'm2']);
    });
  });
}
