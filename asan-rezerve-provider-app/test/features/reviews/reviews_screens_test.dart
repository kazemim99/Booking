import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/reviews/domain/business_review.dart';
import 'package:booksy_provider_app/features/reviews/domain/reviews_repository.dart';
import 'package:booksy_provider_app/features/reviews/presentation/reviews_cubit.dart';
import 'package:booksy_provider_app/features/reviews/presentation/reviews_home_card.dart';
import 'package:booksy_provider_app/features/reviews/presentation/reviews_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

/// What a salon owner sees of their reviews. Pumped with the REAL theme at phone width: the app's buttons are
/// infinite-width (`Size.fromHeight`), so a themed button in a Row blanks the page — only this catches it.
class _MockRepo extends Mock implements ReviewsRepository {}

BusinessReview _review(
  String id, {
  ReviewStatus status = ReviewStatus.published,
  String? reply,
  ReplyStatus? replyStatus,
  String? replyReason,
}) =>
    BusinessReview(
      id: id,
      rating: 4,
      comment: 'نظر $id دربارهٔ سالن',
      status: status,
      reply: reply,
      replyStatus: replyStatus,
      replyReason: replyReason,
      createdAt: DateTime.utc(2026, 9, 20),
    );

ReviewsOverview _overview(List<BusinessReview> items,
        {double average = 4.3, int published = 7, int awaiting = 2}) =>
    ReviewsOverview(
      averageRating: average,
      publishedCount: published,
      awaitingReplyCount: awaiting,
      dimensions: const {ReviewDimension.conduct: DimensionAverage(average: 4.9, count: 6)},
      items: items,
      totalCount: items.length,
    );

Future<void> _pump(WidgetTester tester, Widget child) async {
  tester.view.physicalSize = const Size(390 * 3, 844 * 3);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(MaterialApp(
    theme: AppTheme.light,
    builder: (context, c) => Directionality(textDirection: TextDirection.rtl, child: c!),
    home: child,
  ));
  await tester.pumpAndSettle();
}

void main() {
  late _MockRepo repo;

  setUp(() => repo = _MockRepo());

  group('ReviewsCubit', () {
    blocTest<ReviewsCubit, ReviewsState>(
      'a failed load is a failure, never "no reviews"',
      build: () {
        when(() => repo.load()).thenAnswer((_) async => const Left(ServerFailure('قطع شد')));
        return ReviewsCubit(repo);
      },
      act: (c) => c.load(),
      verify: (c) {
        expect(c.state.loaded, isFalse);
        expect(c.state.error, 'قطع شد');
      },
    );

    test('a reply takes the state the server gave it, and the awaiting count drops', () async {
      when(() => repo.load()).thenAnswer((_) async => Right(_overview([_review('r1')], awaiting: 1)));
      when(() => repo.reply('r1', 'ممنون', edit: false)).thenAnswer(
          (_) async => const Right(ReplyResult(text: 'ممنون', status: ReplyStatus.pending)));
      final cubit = ReviewsCubit(repo);
      await cubit.load();

      final error = await cubit.reply('r1', 'ممنون');

      expect(error, isNull);
      final r1 = cubit.state.overview!.items.single;
      expect(r1.reply, 'ممنون');
      expect(r1.replyStatus, ReplyStatus.pending);
      expect(cubit.state.overview!.awaitingReplyCount, 0);
    });

    test('a refused reply leaves the review as it was and says why', () async {
      when(() => repo.load()).thenAnswer((_) async => Right(_overview([_review('r1')])));
      when(() => repo.reply('r1', any(), edit: any(named: 'edit')))
          .thenAnswer((_) async => const Left(ServerFailure('نه')));
      final cubit = ReviewsCubit(repo);
      await cubit.load();

      expect(await cubit.reply('r1', 'ممنون'), 'نه');
      expect(cubit.state.overview!.items.single.reply, isNull);
    });
  });

  group('the reviews page', () {
    Future<ReviewsCubit> open(WidgetTester tester, List<BusinessReview> items) async {
      when(() => repo.load()).thenAnswer((_) async => Right(_overview(items)));
      final cubit = ReviewsCubit(repo);
      await _pump(tester, BlocProvider.value(value: cubit..load(), child: const ReviewsPage()));
      return cubit;
    }

    testWidgets('headline numbers, the dimensions people rated, and where each review stands',
        (tester) async {
      await open(tester, [
        _review('pub'),
        _review('pend', status: ReviewStatus.pending),
        _review('rej', status: ReviewStatus.rejected),
      ]);

      expect(tester.takeException(), isNull);
      expect(find.text('۴.۳'), findsOneWidget);
      expect(find.text(AppStrings.reviewsPublishedCount(7)), findsOneWidget);
      expect(find.byKey(const Key('reviews-breakdown-conduct')), findsOneWidget);
      expect(find.byKey(const Key('reviews-breakdown-skill')), findsNothing);
      expect(find.text(AppStrings.reviewStatusPending), findsOneWidget);
      expect(find.text(AppStrings.reviewStatusRejected), findsOneWidget);
    });

    testWidgets('only a published, unanswered review offers a reply', (tester) async {
      await open(tester, [
        _review('pub'),
        _review('pend', status: ReviewStatus.pending),
      ]);

      expect(find.byKey(const Key('review-pub-reply')), findsOneWidget);
      expect(find.byKey(const Key('review-pend-reply')), findsNothing,
          reason: 'a pending review may yet be rejected; a reply to it would be wasted');
    });

    testWidgets('a reply says it is not live yet — and a refused one says why and can be rewritten',
        (tester) async {
      await open(tester, [
        _review('a', reply: 'ممنون', replyStatus: ReplyStatus.pending),
        _review('b', reply: 'تماس بگیرید ۰۹۱۲', replyStatus: ReplyStatus.rejected, replyReason: 'شماره تماس'),
        _review('c', reply: 'خوشحالیم', replyStatus: ReplyStatus.published),
      ]);

      expect(tester.takeException(), isNull);
      expect(find.text(AppStrings.replyStatusPending), findsOneWidget);
      expect(find.textContaining('شماره تماس'), findsOneWidget);
      expect(find.text(AppStrings.replyStatusPublished), findsOneWidget);
      expect(find.byKey(const Key('review-b-edit-reply')), findsOneWidget);
    });

    testWidgets('writing a reply sends it and shows it as awaiting approval', (tester) async {
      when(() => repo.reply('pub', 'ممنون از لطف شما', edit: false)).thenAnswer(
          (_) async => const Right(ReplyResult(text: 'ممنون از لطف شما', status: ReplyStatus.pending)));
      await open(tester, [_review('pub')]);

      await tester.tap(find.byKey(const Key('review-pub-reply')));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      await tester.enterText(find.byKey(const Key('reply-text')), 'ممنون از لطف شما');
      await tester.tap(find.byKey(const Key('reply-submit')));
      await tester.pumpAndSettle();

      verify(() => repo.reply('pub', 'ممنون از لطف شما', edit: false)).called(1);
      expect(find.text('ممنون از لطف شما'), findsOneWidget);
      expect(find.text(AppStrings.replyStatusPending), findsOneWidget);
    });

    testWidgets('an empty reply is caught before it is sent', (tester) async {
      await open(tester, [_review('pub')]);

      await tester.tap(find.byKey(const Key('review-pub-reply')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('reply-submit')));
      await tester.pumpAndSettle();

      verifyNever(() => repo.reply(any(), any(), edit: any(named: 'edit')));
      expect(find.text(AppStrings.replyRequired), findsOneWidget);
    });
  });

  group('the Home card', () {
    testWidgets('rating, published count and how many wait on a reply', (tester) async {
      var tapped = false;
      await _pump(
        tester,
        Scaffold(body: ReviewsHomeCard(overview: _overview(const []), onTap: () => tapped = true)),
      );

      expect(tester.takeException(), isNull);
      expect(find.text('۴.۳'), findsOneWidget);
      expect(find.text(AppStrings.reviewsPublishedCount(7)), findsOneWidget);
      expect(find.text(AppStrings.reviewsAwaitingReply(2)), findsOneWidget);
      await tester.tap(find.byType(ReviewsHomeCard));
      expect(tapped, isTrue);
    });

    testWidgets('no published reviews is said in words, not a zero rating', (tester) async {
      await _pump(
        tester,
        Scaffold(
          body: ReviewsHomeCard(
              overview: _overview(const [], average: 0, published: 0, awaiting: 0), onTap: () {}),
        ),
      );

      expect(find.text(AppStrings.reviewsNoneYet), findsOneWidget);
      expect(find.text('۰.۰'), findsNothing);
      expect(find.textContaining(AppStrings.reviewsAwaitingReply(0)), findsNothing);
    });
  });
}
