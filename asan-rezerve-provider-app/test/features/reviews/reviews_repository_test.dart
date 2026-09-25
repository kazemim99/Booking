import 'dart:convert';
import 'dart:typed_data';

import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:booksy_provider_app/features/reviews/data/reviews_api_service.dart';
import 'package:booksy_provider_app/features/reviews/data/reviews_repository_impl.dart';
import 'package:booksy_provider_app/features/reviews/domain/business_review.dart';
import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

/// The business's side of reviews (openspec/changes/provider-reviews-and-ratings): the owner-scoped inbox, the
/// public summary for the headline numbers, and the reply calls — checked on the wire.
class _MockAuth extends Mock implements AuthRepository {}

class _MockSession extends Mock implements ProviderSession {}

class _Adapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  final Map<String, Object> bodies = {};
  int statusCode = 200;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream,
      Future<void>? cancelFuture) async {
    requests.add(options);
    final body = bodies['${options.method} ${options.path}'] ?? const <String, dynamic>{};
    return ResponseBody.fromString(jsonEncode(body), statusCode, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  late _Adapter adapter;
  late ReviewsRepositoryImpl repository;
  late _MockAuth auth;

  setUp(() {
    adapter = _Adapter();
    auth = _MockAuth();
    final session = _MockSession();
    when(() => session.providerId).thenReturn('p1');
    when(() => auth.getCurrentSession()).thenAnswer((_) async => Right(session));
    final dio = Dio(BaseOptions(baseUrl: 'http://test'))..httpClientAdapter = adapter;
    repository = ReviewsRepositoryImpl(ReviewsApiService(dio), auth);
  });

  test('the overview is the inbox for the rows and the public summary for the numbers', () async {
    adapter.bodies['GET /v1/Reviews/providers/p1/inbox'] = {
      'data': {
        'items': [
          {
            'reviewId': 'r1',
            'rating': 4.5,
            'skillRating': 5,
            'comment': 'کار دقیق و تمیز',
            'moderationStatus': 'Published',
            'providerResponse': 'ممنون',
            'replyModerationStatus': 'Rejected',
            'replyModerationReason': 'شماره تماس دارد',
            'helpfulCount': 3,
            'notHelpfulCount': 0,
            'createdAt': '2026-09-20T10:00:00Z',
          },
          {'reviewId': 'r2', 'rating': 2, 'moderationStatus': 'Pending', 'createdAt': '2026-09-21T10:00:00Z'},
        ],
        'totalCount': 2,
        'awaitingReplyCount': 5,
      },
    };
    adapter.bodies['GET /v1/Reviews/providers/p1'] = {
      'statistics': {
        'totalReviews': 12,
        'averageRating': 4.25,
        'skill': {'average': 4.6, 'count': 9},
        'punctuality': {'average': null, 'count': 0},
      },
    };

    final overview = (await repository.load()).getOrElse(() => throw 'no overview');

    expect(overview.averageRating, 4.25);
    expect(overview.publishedCount, 12);
    expect(overview.awaitingReplyCount, 5, reason: "the server's count, not this page's");
    expect(overview.dimensions.keys, [ReviewDimension.skill]);
    final r1 = overview.items.first;
    expect(r1.status, ReviewStatus.published);
    expect(r1.replyStatus, ReplyStatus.rejected);
    expect(r1.replyReason, 'شماره تماس دارد');
    expect(r1.dimensions, {ReviewDimension.skill: 5.0});
    expect(overview.items[1].status, ReviewStatus.pending);
  });

  test('a first reply is a POST; changing it is a PUT; withdrawing it is a DELETE', () async {
    adapter.bodies['POST /v1/Reviews/r1/reply'] = {
      'reviewId': 'r1',
      'providerResponse': 'ممنون از شما',
      'replyModerationStatus': 'Pending',
    };

    final added = (await repository.reply('r1', '  ممنون از شما  ', edit: false))
        .getOrElse(() => throw 'x');
    expect(adapter.requests.last.method, 'POST');
    expect(adapter.requests.last.data, {'text': 'ممنون از شما'});
    expect(added.text, 'ممنون از شما');
    expect(added.status, ReplyStatus.pending);

    await repository.reply('r1', 'متن تازه', edit: true);
    expect(adapter.requests.last.method, 'PUT');
    expect(adapter.requests.last.path, '/v1/Reviews/r1/reply');

    await repository.removeReply('r1');
    expect(adapter.requests.last.method, 'DELETE');
    expect(adapter.requests.last.path, '/v1/Reviews/r1/reply');
  });

  test('without a business on the session there is nothing to load', () async {
    when(() => auth.getCurrentSession()).thenAnswer((_) async => const Right(null));

    final result = await repository.load();

    expect(result.isLeft(), isTrue);
    expect(adapter.requests, isEmpty);
  });

  test("the server's refusal is the message", () async {
    adapter
      ..statusCode = 400
      ..bodies['POST /v1/Reviews/r1/reply'] = {'message': 'این نظر هنوز منتشر نشده است'};

    final result = await repository.reply('r1', 'ممنون', edit: false);

    expect(result.fold((f) => f.message, (_) => ''), 'این نظر هنوز منتشر نشده است');
    expect(result.fold((f) => f, (_) => null), isA<Failure>());
  });
}
