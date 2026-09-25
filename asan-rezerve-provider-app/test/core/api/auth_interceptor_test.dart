import 'dart:convert';
import 'dart:typed_data';

import 'package:booksy_provider_app/core/api/interceptors/auth_interceptor.dart';
import 'package:booksy_provider_app/core/api/interceptors/error_interceptor.dart';
import 'package:booksy_provider_app/core/storage/secure_storage_service.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockStorage extends Mock implements SecureStorageService {}

/// Answers each request from a script keyed by path, in order, and records
/// what was sent.
class _ScriptedAdapter implements HttpClientAdapter {
  final Map<String, List<int>> script;
  final List<RequestOptions> sent = [];
  _ScriptedAdapter(this.script);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    sent.add(options);
    final queue = script.entries
        .firstWhere((e) => options.path.endsWith(e.key))
        .value;
    final status = queue.removeAt(0);
    final body = status == 200 && options.path.endsWith('/Auth/refresh')
        ? jsonEncode({
            'data': {
              'accessToken': 'new-access',
              'refreshToken': 'new-refresh',
            },
          })
        : jsonEncode({'message': 'x'});
    return ResponseBody.fromString(
      body,
      status,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  late _MockStorage storage;
  late int expiredCalls;

  setUp(() {
    storage = _MockStorage();
    expiredCalls = 0;
    when(() => storage.getAccessToken()).thenAnswer((_) async => 'old-access');
    when(
      () => storage.getRefreshToken(),
    ).thenAnswer((_) async => 'old-refresh');
    when(() => storage.saveAccessToken(any())).thenAnswer((_) async {});
    when(() => storage.saveRefreshToken(any())).thenAnswer((_) async {});
    when(() => storage.clearSession()).thenAnswer((_) async {});
  });

  Dio build(_ScriptedAdapter adapter) {
    final refreshDio = Dio(BaseOptions(baseUrl: 'https://api.test'))
      ..httpClientAdapter = adapter;
    return Dio(BaseOptions(baseUrl: 'https://api.test'))
      ..httpClientAdapter = adapter
      ..interceptors.add(
        AuthInterceptor(
          storage,
          refreshDio,
          onSessionExpired: () => expiredCalls++,
        ),
      )
      // Same order as production (DioFactory.createAuthenticatedDio).
      ..interceptors.add(ErrorInterceptor());
  }

  test(
    'an expired token is refreshed and the request retried with the new one',
    () async {
      final adapter = _ScriptedAdapter({
        '/Providers/organizations': [401, 200],
        '/Auth/refresh': [200],
      });

      final res = await build(adapter).post('/v1/Providers/organizations');

      expect(res.statusCode, 200);
      expect(adapter.sent.last.headers['Authorization'], 'Bearer new-access');
      verifyNever(() => storage.clearSession());
      expect(expiredCalls, 0);
    },
  );

  test('a failed refresh signs the user out', () async {
    final adapter = _ScriptedAdapter({
      '/Providers/organizations': [401],
      '/Auth/refresh': [401],
    });

    await expectLater(
      build(adapter).post('/v1/Providers/organizations'),
      throwsA(isA<DioException>()),
    );

    verify(() => storage.clearSession()).called(1);
    expect(expiredCalls, 1);
  });

  test(
    'a refresh that WORKED keeps the session even if the retry fails',
    () async {
      // Production 2026-09-19: the server answered 401 for a server fault; the
      // refresh succeeded, the retry failed the same way, and the app wiped a
      // perfectly valid session — every later request went out unauthenticated.
      final adapter = _ScriptedAdapter({
        '/Providers/organizations': [401, 500],
        '/Auth/refresh': [200],
      });

      await expectLater(
        build(adapter).post('/v1/Providers/organizations'),
        throwsA(
          isA<DioException>().having(
            (e) => e.response?.statusCode,
            'status',
            500,
          ),
        ),
      );

      verifyNever(() => storage.clearSession());
      expect(expiredCalls, 0);
      verify(() => storage.saveAccessToken('new-access')).called(1);
    },
  );

  test(
    'errors still reach ErrorInterceptor, so the user sees Persian, not Dio text',
    () async {
      // The screenshot: the banner showed DioException's English explanation of
      // status 401, because handler.reject() skipped every later interceptor.
      final adapter = _ScriptedAdapter({
        '/Providers/organizations': [401, 500],
        '/Auth/refresh': [200],
      });

      final error = await build(adapter)
          .post('/v1/Providers/organizations')
          .then<DioException?>(
            (_) => null,
            onError: (Object e) => e as DioException,
          );

      expect(
        error!.message,
        'x',
        reason: 'ErrorInterceptor surfaces the server message',
      );
      expect(error.message, isNot(contains('RequestOptions.validateStatus')));
    },
  );
}
