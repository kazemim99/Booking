import 'dart:convert';
import 'dart:typed_data';

import 'package:booksy_customer_app/features/reviews/data/datasources/review_remote_datasource.dart';
import 'package:booksy_customer_app/features/reviews/data/repositories/review_repository_impl.dart';
import 'package:booksy_customer_app/features/reviews/domain/entities/review.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// What the app sends to the reviews endpoints and how it reads the answers
/// (openspec/changes/provider-reviews-and-ratings). Against the wire, because a
/// repository fake could not show a dimension going out as zero.
class _CapturingAdapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  Object body = const <String, dynamic>{};
  int statusCode = 200;

  RequestOptions get last => requests.last;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream,
      Future<void>? cancelFuture) async {
    requests.add(options);
    return ResponseBody.fromString(jsonEncode(body), statusCode,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        });
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  late _CapturingAdapter adapter;
  late ReviewRepositoryImpl repository;

  setUp(() {
    adapter = _CapturingAdapter();
    final dio = Dio(BaseOptions(baseUrl: 'http://test'))..httpClientAdapter = adapter;
    repository = ReviewRepositoryImpl(
        remoteDataSource: ReviewRemoteDataSource(serviceCatalogDio: dio));
  });

  test('a new review carries only the dimensions the customer gave', () async {
    await repository.createReview(
      bookingId: 'b1',
      rating: 4.5,
      comment: '  خیلی خوب بود، ممنونم  ',
      dimensions: const {ReviewDimension.skill: 5, ReviewDimension.conduct: 4},
    );

    expect(adapter.last.method, 'POST');
    expect(adapter.last.path, '/v1/Reviews/bookings/b1');
    expect(adapter.last.data, {
      'rating': 4.5,
      'comment': 'خیلی خوب بود، ممنونم',
      'skillRating': 5.0,
      'conductRating': 4.0,
    });
  });

  test('an edit goes to the review itself, with the same body', () async {
    adapter.body = {'reviewId': 'r1', 'moderationStatus': 'Pending'};

    final result = await repository.editReview(
      reviewId: 'r1',
      rating: 3,
      comment: 'بعد از چند روز نظرم عوض شد',
      dimensions: const {ReviewDimension.cleanliness: 2},
    );

    expect(result.isRight(), isTrue);
    expect(adapter.last.method, 'PUT');
    expect(adapter.last.path, '/v1/Reviews/r1');
    expect(adapter.last.data,
        {'rating': 3.0, 'comment': 'بعد از چند روز نظرم عوض شد', 'cleanlinessRating': 2.0});
  });

  test('a vote is a PUT, and the answer is the server\'s tally', () async {
    adapter.body = {
      'reviewId': 'r1',
      'helpfulCount': 6,
      'notHelpfulCount': 2,
      'myVote': 'notHelpful',
    };

    final result = await repository.vote('r1', false);

    expect(adapter.last.method, 'PUT');
    expect(adapter.last.path, '/v1/Reviews/r1/helpful');
    expect(adapter.last.data, {'isHelpful': false});
    final tally = result.getOrElse(() => throw 'no tally');
    expect(tally.helpfulCount, 6);
    expect(tally.notHelpfulCount, 2);
    expect(tally.myVote, ReviewVote.notHelpful);
  });

  test('withdrawing a vote comes back as no vote', () async {
    adapter.body = {'helpfulCount': 0, 'notHelpfulCount': 0, 'myVote': null};

    final tally = (await repository.vote('r1', true)).getOrElse(() => throw 'x');

    expect(tally.myVote, isNull);
  });

  test('a salon\'s listing carries dimensions, votes and the reader\'s own vote',
      () async {
    adapter.body = {
      'data': {
        'statistics': {
          'totalReviews': 3,
          'averageRating': 4.33,
          'cleanliness': {'average': 4.5, 'count': 2},
          'skill': {'average': null, 'count': 0},
          'punctuality': {'average': 3.0, 'count': 1},
          'conduct': {'count': 0},
        },
        'reviews': {
          'items': [
            {
              'reviewId': 'r1',
              'customerName': 'سارا',
              'rating': 5,
              'helpfulCount': 4,
              'notHelpfulCount': 1,
              'myVote': 'helpful',
              'cleanlinessRating': 5,
            },
          ],
        },
      },
    };

    final listing =
        (await repository.getProviderReviews('p1')).getOrElse(() => throw 'x');

    expect(listing.totalReviews, 3);
    expect(listing.dimensions.keys,
        [ReviewDimension.cleanliness, ReviewDimension.punctuality],
        reason: 'a dimension nobody rated is absent, not an average of zero');
    expect(listing.dimensions[ReviewDimension.cleanliness]?.average, 4.5);
    final r1 = listing.items.single;
    expect(r1.helpfulCount, 4);
    expect(r1.notHelpfulCount, 1);
    expect(r1.myVote, ReviewVote.helpful);
    expect(r1.dimensions, {ReviewDimension.cleanliness: 5.0});
  });

  test('my reviews come in every state, with the reason', () async {
    adapter.body = {
      'items': [
        {
          'reviewId': 'r1',
          'providerId': 'p1',
          'providerName': 'سالن رز',
          'serviceName': 'رنگ مو',
          'rating': 2,
          'conductRating': 1,
          'comment': 'برخورد مناسبی نداشتند',
          'moderationStatus': 'Rejected',
          'moderationReason': 'توهین‌آمیز',
          'providerResponse': 'پاسخ در انتظار',
          'replyModerationStatus': 'Pending',
          'createdAt': '2026-09-20T10:00:00Z',
          'canEdit': false,
        },
        {
          'reviewId': 'r2',
          'rating': 5,
          'moderationStatus': 'Published',
          'providerResponse': 'ممنون',
          'replyModerationStatus': 'Published',
          'createdAt': '2026-09-21T10:00:00Z',
          'editedAt': '2026-09-21T12:00:00Z',
          'canEdit': true,
        },
      ],
      'totalCount': 2,
    };

    final mine = (await repository.getMyReviews()).getOrElse(() => throw 'x');

    expect(adapter.last.path, '/v1/Reviews/me');
    expect(mine.map((r) => r.status),
        [ReviewModerationStatus.rejected, ReviewModerationStatus.published]);
    expect(mine[0].moderationReason, 'توهین‌آمیز');
    expect(mine[0].providerName, 'سالن رز');
    expect(mine[0].dimensions, {ReviewDimension.conduct: 1.0});
    expect(mine[0].providerResponse, isNull,
        reason: 'a reply still awaiting approval is not shown to anyone yet');
    expect(mine[1].providerResponse, 'ممنون');
    expect(mine[1].canEdit, isTrue);
    expect(mine[1].isEdited, isTrue);
  });

  test('the server\'s refusal is the message', () async {
    adapter
      ..statusCode = 400
      ..body = {'message': 'مهلت ویرایش گذشته است'};

    final result = await repository.editReview(reviewId: 'r1', rating: 4);

    expect(result.fold((f) => f.message, (_) => ''), 'مهلت ویرایش گذشته است');
  });

  // openspec/changes/_inline/customer-reviews-and-nahal-seed: every refusal of «ثبت نظر» read «ثبت نظر ناموفق بود»,
  // because the create-review endpoint answers `errors: [{ message }]` and only `message` was read.
  test('a create-review refusal says the server\'s words', () async {
    adapter
      ..statusCode = 409
      ..body = {
        'success': false,
        'errors': [
          {'code': 'ERR_CONFLICT', 'message': 'برای این نوبت قبلاً نظر ثبت کرده‌اید.'}
        ],
      };

    final result = await repository.createReview(bookingId: 'b1', rating: 5);

    expect(result.fold((f) => f.message, (_) => ''), 'برای این نوبت قبلاً نظر ثبت کرده‌اید.');
  });

  test('a validation refusal says the customer\'s words, not the English wrapper', () async {
    adapter
      ..statusCode = 400
      ..body = {
        'success': false,
        'message': "Validation failed for property 'Comment': متن نظر باید دست‌کم ۱۰ نویسه باشد.",
        'error': {
          'code': 'DOMAIN_VALIDATION_FAILED',
          'errors': {
            'Comment': ['متن نظر باید دست‌کم ۱۰ نویسه باشد.']
          },
        },
      };

    final result = await repository.editReview(reviewId: 'r1', rating: 4, comment: 'کوتاه');

    expect(result.fold((f) => f.message, (_) => ''), 'متن نظر باید دست‌کم ۱۰ نویسه باشد.');
  });
}
