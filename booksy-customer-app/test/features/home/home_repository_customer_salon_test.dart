import 'dart:convert';
import 'dart:typed_data';

import 'package:booksy_customer_app/features/home/data/datasources/home_remote_datasource.dart';
import 'package:booksy_customer_app/features/home/data/repositories/home_repository_impl.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// What the salon profile sends for a signed-in customer's visit and favourite
/// (customer-app-ux-review-fixes C.4/C.5), checked on the wire against
/// CustomersController: RecordProviderVisitRequest, AddFavoriteProviderRequest
/// and the favourites list, which carries only `providerId`, `notes` and
/// `addedAt` per row.
class _CapturingAdapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  Object? body = const <String, dynamic>{};
  int statusCode = 200;

  RequestOptions get last => requests.last;

  @override
  Future<ResponseBody> fetch(RequestOptions options,
      Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    requests.add(options);
    return ResponseBody.fromString(jsonEncode(body), statusCode, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  late _CapturingAdapter adapter;
  late HomeRepositoryImpl repository;

  setUp(() {
    adapter = _CapturingAdapter();
    final dio = Dio(BaseOptions(baseUrl: 'http://test'))
      ..httpClientAdapter = adapter;
    repository = HomeRepositoryImpl(HomeRemoteDataSource(
      serviceCatalogDio: dio,
      userManagementDio: dio,
    ));
  });

  group('recording a visit', () {
    test('posts the salon and where it was seen to the customer\'s list',
        () async {
      adapter.body = {'success': true};

      final result = await repository.recordProviderVisit('c1', 'p1',
          viewSource: 'profile');

      expect(result.isRight(), isTrue);
      expect(adapter.last.method, 'POST');
      expect(adapter.last.path, '/v1/Customers/c1/recently-visited');
      expect(adapter.last.data, {'providerId': 'p1', 'viewSource': 'profile'});
    });
  });

  group('favourite salons', () {
    test('the ids come from the list the server actually sends', () async {
      // FavoriteProviderViewModel: no name, no logo — only these three.
      adapter.body = [
        {'providerId': 'p1', 'notes': null, 'addedAt': '2026-09-20T10:00:00Z'},
        {'providerId': 'p2', 'notes': 'x', 'addedAt': '2026-09-21T10:00:00Z'},
      ];

      final result = await repository.getFavoriteProviderIds('c1');

      expect(adapter.last.method, 'GET');
      expect(adapter.last.path, '/v1/Customers/c1/favorites');
      expect(result.getOrElse(() => {}), {'p1', 'p2'});
    });

    test('a wrapped list reads the same', () async {
      adapter.body = {
        'success': true,
        'data': [
          {'providerId': 'p3', 'addedAt': '2026-09-20T10:00:00Z'},
        ],
      };

      final result = await repository.getFavoriteProviderIds('c1');

      expect(result.getOrElse(() => {}), {'p3'});
    });

    test('adding posts AddFavoriteProviderRequest', () async {
      adapter
        ..statusCode = 201
        ..body = {'success': true};

      final result = await repository.addFavoriteProvider('c1', 'p1');

      expect(result.isRight(), isTrue);
      expect(adapter.last.method, 'POST');
      expect(adapter.last.path, '/v1/Customers/c1/favorites');
      expect(adapter.last.data, {'providerId': 'p1'});
    });

    test('adding one that is already a favourite is not a failure', () async {
      adapter
        ..statusCode = 409
        ..body = {'error': 'already a favorite'};

      final result = await repository.addFavoriteProvider('c1', 'p1');

      expect(result.isRight(), isTrue,
          reason: 'the salon is a favourite, which is what the customer asked');
    });

    test('removing deletes the one salon', () async {
      adapter.body = {'success': true};

      final result = await repository.removeFavoriteProvider('c1', 'p1');

      expect(result.isRight(), isTrue);
      expect(adapter.last.method, 'DELETE');
      expect(adapter.last.path, '/v1/Customers/c1/favorites/p1');
    });

    test('removing one that is not a favourite is not a failure', () async {
      adapter
        ..statusCode = 404
        ..body = {'error': 'not a favorite'};

      final result = await repository.removeFavoriteProvider('c1', 'p1');

      expect(result.isRight(), isTrue);
    });

    test('a refused change is a failure', () async {
      adapter
        ..statusCode = 403
        ..body = {'error': 'forbidden'};

      expect(
          (await repository.addFavoriteProvider('c1', 'p1')).isLeft(), isTrue);
      expect((await repository.removeFavoriteProvider('c1', 'p1')).isLeft(),
          isTrue);
    });
  });
}
