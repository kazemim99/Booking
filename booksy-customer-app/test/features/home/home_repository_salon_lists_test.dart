import 'dart:convert';
import 'dart:typed_data';

import 'package:booksy_customer_app/features/home/data/datasources/home_remote_datasource.dart';
import 'package:booksy_customer_app/features/home/data/repositories/home_repository_impl.dart';
import 'package:booksy_customer_app/features/home/domain/entities/favorite_provider.dart';
import 'package:booksy_customer_app/features/home/domain/entities/recently_visited_provider.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Home's "recent visits & favourites" as the app reads them off the wire
/// (customer-app-ux-review-fixes P1).
///
/// Two real server shapes exist. Before the backend fix a row carried only the
/// salon's id (FavoriteProviderViewModel {providerId, notes, addedAt} and
/// RecentlyVisitedProviderViewModel {providerId, visitedAt, viewSource}); the
/// app required a name, threw, and the section showed its load error. After it,
/// each row also carries the salon's name, photo, city and rating, and a recent
/// visit carries lastVisitedAt and visitCount. The app must read both: a row
/// with no name is dropped (the section stays hidden) instead of failing.
class _Adapter implements HttpClientAdapter {
  Object? body = const <dynamic>[];
  final paths = <String>[];

  @override
  Future<ResponseBody> fetch(RequestOptions options,
      Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    paths.add(options.path);
    return ResponseBody.fromString(jsonEncode(body), 200, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

// ---- The shapes the server has actually sent ----

/// FavoriteProviderViewModel before P1: no name.
const _favouriteOld = {
  'providerId': 'p1',
  'notes': null,
  'addedAt': '2026-09-20T10:00:00Z',
};

/// FavoriteProviderViewModel after P1.
const _favouriteNew = {
  'providerId': 'p2',
  'notes': 'near work',
  'addedAt': '2026-09-21T10:00:00Z',
  'providerName': 'سالن آفتاب',
  'logoUrl': 'https://back.example/uploads/providers/p2/logo.webp',
  'city': 'تهران',
  'averageRating': 4.5,
  'totalReviews': 2,
};

/// RecentlyVisitedProviderViewModel before P1: no name.
const _visitOld = {
  'providerId': 'p1',
  'visitedAt': '2026-09-20T10:00:00Z',
  'viewSource': 'profile',
};

/// RecentlyVisitedProviderViewModel after P1.
const _visitNew = {
  'providerId': 'p2',
  'visitedAt': '2026-09-22T08:30:00Z',
  'viewSource': 'profile',
  'providerName': 'سالن مهتاب',
  'logoUrl': null,
  'city': 'شیراز',
  'averageRating': 0,
  'totalReviews': 0,
  'lastVisitedAt': '2026-09-22T08:30:00Z',
  'visitCount': 3,
};

void main() {
  late _Adapter adapter;
  late HomeRepositoryImpl repository;

  setUp(() {
    adapter = _Adapter();
    final dio = Dio(BaseOptions(baseUrl: 'http://test'))
      ..httpClientAdapter = adapter;
    repository = HomeRepositoryImpl(HomeRemoteDataSource(
      serviceCatalogDio: dio,
      userManagementDio: dio,
    ));
  });

  group('favourite salons on Home', () {
    test('rows from a server without names leave the section empty, not failed',
        () async {
      adapter.body = [_favouriteOld];

      final result = await repository.getFavoriteProviders('c1');

      expect(result.isRight(), isTrue,
          reason: 'an old server must not show the load error');
      expect(result.getOrElse(() => []), isEmpty);
    });

    test('rows with a name carry the salon to its card', () async {
      adapter.body = [_favouriteNew];

      final result = await repository.getFavoriteProviders('c1');

      expect(adapter.paths.single, '/v1/Customers/c1/favorites');
      final list = result.getOrElse(() => []);
      expect(list, [
        FavoriteProvider(
          providerId: 'p2',
          providerName: 'سالن آفتاب',
          logoUrl: 'https://back.example/uploads/providers/p2/logo.webp',
          city: 'تهران',
          averageRating: 4.5,
          totalReviews: 2,
          addedAt: DateTime.parse('2026-09-21T10:00:00Z'),
          notes: 'near work',
        ),
      ]);
    });

    test('a nameless row is dropped and the named ones kept', () async {
      adapter.body = {
        'success': true,
        'data': [_favouriteOld, _favouriteNew],
      };

      final result = await repository.getFavoriteProviders('c1');

      expect(result.getOrElse(() => []).map((f) => f.providerId), ['p2']);
    });

    test('a row that cannot be read is dropped, not the whole list', () async {
      adapter.body = [
        {..._favouriteNew, 'providerId': 'bad', 'addedAt': 'not a date'},
        _favouriteNew,
      ];

      final result = await repository.getFavoriteProviders('c1');

      expect(result.getOrElse(() => []).map((f) => f.providerId), ['p2']);
    });
  });

  group('recently visited salons on Home', () {
    test('rows from a server without names leave the section empty, not failed',
        () async {
      adapter.body = [_visitOld];

      final result = await repository.getRecentlyVisitedProviders('c1');

      expect(result.isRight(), isTrue,
          reason: 'an old server must not show the load error');
      expect(result.getOrElse(() => []), isEmpty);
    });

    test('rows with a name carry the salon, its last visit and visit count',
        () async {
      adapter.body = [_visitNew];

      final result = await repository.getRecentlyVisitedProviders('c1', limit: 5);

      expect(adapter.paths.single, '/v1/Customers/c1/recently-visited');
      expect(result.getOrElse(() => []), [
        RecentlyVisitedProvider(
          providerId: 'p2',
          providerName: 'سالن مهتاب',
          city: 'شیراز',
          averageRating: 0,
          totalReviews: 0,
          lastVisitedAt: DateTime.parse('2026-09-22T08:30:00Z'),
          visitCount: 3,
        ),
      ]);
    });

    test('without lastVisitedAt and visitCount, visitedAt and 1 stand in',
        () async {
      adapter.body = [
        {..._visitOld, 'providerName': 'سالن آفتاب'},
      ];

      final result = await repository.getRecentlyVisitedProviders('c1');

      final visit = result.getOrElse(() => []).single;
      expect(visit.lastVisitedAt, DateTime.parse('2026-09-20T10:00:00Z'));
      expect(visit.visitCount, 1);
    });

    test('a nameless row is dropped and the named ones kept, in order',
        () async {
      adapter.body = {
        'success': true,
        'data': [
          _visitNew,
          _visitOld,
          {..._visitNew, 'providerId': 'p3', 'providerName': 'سالن سوم'},
        ],
      };

      final result = await repository.getRecentlyVisitedProviders('c1');

      expect(result.getOrElse(() => []).map((v) => v.providerId), ['p2', 'p3']);
    });

    test('a row with no visit time at all is dropped', () async {
      adapter.body = [
        {'providerId': 'p9', 'providerName': 'سالن بی‌تاریخ'},
        _visitNew,
      ];

      final result = await repository.getRecentlyVisitedProviders('c1');

      expect(result.getOrElse(() => []).map((v) => v.providerId), ['p2']);
    });
  });
}
