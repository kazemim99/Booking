import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/reviews/domain/entities/review.dart';
import 'package:booksy_customer_app/features/reviews/domain/repositories/review_repository.dart';
import 'package:booksy_customer_app/features/reviews/presentation/pages/my_reviews_page.dart';
import 'package:booksy_customer_app/features/reviews/presentation/widgets/provider_reviews_section.dart';
import 'package:booksy_customer_app/features/reviews/presentation/widgets/write_review_dialog.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/provider_detail_cubit.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Rating in dimensions, voting on what others wrote, and a customer's own
/// reviews with where each one stands (openspec/changes/provider-reviews-and-ratings).
void main() {
  Widget host(Widget child) => MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: SingleChildScrollView(child: child)),
        ),
      );

  group('rating in dimensions', () {
    Future<ReviewDraft?> Function() openDialog(WidgetTester tester,
        {ReviewDraft? initial}) {
      ReviewDraft? result;
      return () async {
        await tester.pumpWidget(
          MaterialApp(
            home: Builder(
              builder: (context) => Scaffold(
                body: TextButton(
                  key: const Key('open'),
                  onPressed: () async => result =
                      await showWriteReviewDialog(context, initial: initial),
                  child: const Text('open'),
                ),
              ),
            ),
          ),
        );
        await tester.tap(find.byKey(const Key('open')));
        await tester.pumpAndSettle();
        return result;
      };
    }

    testWidgets('the four dimensions wait behind a disclosure and are optional',
        (tester) async {
      ReviewDraft? result;
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                key: const Key('open'),
                onPressed: () async =>
                    result = await showWriteReviewDialog(context),
                child: const Text('open'),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.byKey(const Key('open')));
      await tester.pumpAndSettle();

      // Closed by default: the overall star is the whole required review.
      expect(find.byKey(const Key('review-dim-cleanliness-1')), findsNothing);
      await tester.tap(find.byKey(const Key('review-dimensions')));
      await tester.pumpAndSettle();
      for (final d in ReviewDimension.values) {
        expect(find.text(d.label), findsOneWidget);
      }

      await tester.tap(find.byKey(const Key('review-star-5')));
      await tester.tap(find.byKey(const Key('review-dim-cleanliness-4')));
      await tester.tap(find.byKey(const Key('review-dim-punctuality-2')));
      await tester.tap(find.byKey(const Key('review-submit')));
      await tester.pumpAndSettle();

      expect(result?.rating, 5);
      expect(result?.dimensions, {
        ReviewDimension.cleanliness: 4.0,
        ReviewDimension.punctuality: 2.0,
      }, reason: 'a dimension left untouched is not sent as zero');
    });

    testWidgets('editing opens on what was said before', (tester) async {
      final open = openDialog(tester,
          initial: const ReviewDraft(
            rating: 4,
            comment: 'کار تمیز و به‌موقع بود، ممنون',
            dimensions: {ReviewDimension.skill: 5},
          ));
      await open();

      expect(find.text('کار تمیز و به‌موقع بود، ممنون'), findsOneWidget);
      // A given dimension opens the disclosure, so the customer sees it.
      expect(find.byKey(const Key('review-dim-skill-5')), findsOneWidget);
      expect(find.text(AppStrings.reviewEditNotice), findsOneWidget,
          reason: 'an edit returns to approval, and the customer is told first');
    });
  });

  group('a salon\'s reviews', () {
    ProviderReviews sample({ReviewVote? myVote}) => ProviderReviews(
          averageRating: 4.5,
          totalReviews: 2,
          dimensions: const {
            ReviewDimension.cleanliness: DimensionAverage(average: 4.8, count: 2),
            ReviewDimension.skill: DimensionAverage(average: 4.1, count: 1),
          },
          items: [
            Review(
              id: 'r1',
              customerName: 'سارا',
              rating: 5,
              helpfulCount: 3,
              notHelpfulCount: 1,
              myVote: myVote,
            ),
          ],
        );

    testWidgets('shows the dimensions people rated, and only those',
        (tester) async {
      await tester.pumpWidget(host(ProviderReviewsSection(reviews: sample())));

      expect(find.byKey(const Key('review-breakdown-cleanliness')), findsOneWidget);
      expect(find.byKey(const Key('review-breakdown-skill')), findsOneWidget);
      expect(find.text('۴.۸'), findsOneWidget);
      expect(find.byKey(const Key('review-breakdown-punctuality')), findsNothing,
          reason: 'nobody rated it — a zero bar would read as a bad score');
    });

    testWidgets('a signed-in reader votes, and sees their own vote',
        (tester) async {
      final votes = <(String, bool)>[];
      await tester.pumpWidget(host(ProviderReviewsSection(
        reviews: sample(myVote: ReviewVote.helpful),
        onVote: (review, helpful) => votes.add((review.id, helpful)),
      )));

      expect(find.byIcon(Icons.thumb_up), findsOneWidget,
          reason: 'their helpful vote is shown as theirs');
      expect(find.byIcon(Icons.thumb_down_outlined), findsOneWidget);
      expect(find.text('۳'), findsOneWidget);

      await tester.tap(find.byKey(const Key('review-r1-not-helpful')));
      expect(votes, [('r1', false)]);
    });

    testWidgets('without a way to vote the counts show but nothing is tappable',
        (tester) async {
      await tester.pumpWidget(host(ProviderReviewsSection(reviews: sample())));

      final button = tester.widget<IconButton>(
          find.byKey(const Key('review-r1-helpful')));
      expect(button.onPressed, isNull);
    });
  });

  group('voting from the profile page', () {
    test('the cubit takes the server\'s counts, not its own arithmetic', () async {
      final reviews = _FakeReviews()
        ..listing = const ProviderReviews(totalReviews: 1, items: [
          Review(id: 'r1', customerName: 'x', rating: 4, helpfulCount: 2),
        ])
        ..voteResult = const ReviewVoteResult(
            helpfulCount: 7, notHelpfulCount: 1, myVote: ReviewVote.helpful);
      final cubit = ProviderDetailCubit(_NoBookings(), reviewRepository: reviews);
      await cubit.loadReviews('p1');

      final error = await cubit.vote('r1', true);

      expect(error, isNull);
      expect(reviews.votes, [('r1', true)]);
      final r1 = cubit.state.reviews!.items.single;
      expect(r1.helpfulCount, 7);
      expect(r1.myVote, ReviewVote.helpful);
    });

    test('a refused vote leaves the review as it was and says why', () async {
      final reviews = _FakeReviews()
        ..listing = const ProviderReviews(totalReviews: 1, items: [
          Review(id: 'r1', customerName: 'x', rating: 4, helpfulCount: 2),
        ])
        ..voteFailure = const ServerFailure('به نظر خودتان نمی‌توانید رأی بدهید');
      final cubit = ProviderDetailCubit(_NoBookings(), reviewRepository: reviews);
      await cubit.loadReviews('p1');

      final error = await cubit.vote('r1', true);

      expect(error, 'به نظر خودتان نمی‌توانید رأی بدهید');
      expect(cubit.state.reviews!.items.single.helpfulCount, 2);
    });
  });

  group('my reviews', () {
    MyReview mine(
      String id,
      ReviewModerationStatus status, {
      String? reason,
      bool canEdit = false,
      Map<ReviewDimension, double> dimensions = const {},
    }) =>
        MyReview(
          id: id,
          providerName: 'سالن $id',
          serviceName: 'کوتاهی مو',
          rating: 4,
          dimensions: dimensions,
          comment: 'نظر $id دربارهٔ این سالن',
          status: status,
          moderationReason: reason,
          createdAt: DateTime(2026, 9, 20),
          canEdit: canEdit,
        );

    testWidgets('every review is listed with where it stands', (tester) async {
      await tester.pumpWidget(host(MyReviewsList(reviews: [
        mine('a', ReviewModerationStatus.pending, canEdit: true),
        mine('b', ReviewModerationStatus.published,
            dimensions: const {ReviewDimension.conduct: 5}),
        mine('c', ReviewModerationStatus.rejected, reason: 'شماره تلفن دارد'),
        mine('d', ReviewModerationStatus.hidden, reason: 'گزارش تأیید شد'),
      ])));

      expect(find.text(AppStrings.reviewStatusPending), findsOneWidget);
      expect(find.text(AppStrings.reviewStatusPublished), findsOneWidget);
      expect(find.text(AppStrings.reviewStatusRejected), findsOneWidget);
      expect(find.text(AppStrings.reviewStatusHidden), findsOneWidget);
      // The administrator's reason is the customer's to read.
      expect(find.textContaining('شماره تلفن دارد'), findsOneWidget);
      expect(find.textContaining('گزارش تأیید شد'), findsOneWidget);
      expect(find.text('سالن a'), findsOneWidget);
      expect(find.textContaining(ReviewDimension.conduct.label), findsOneWidget);
    });

    testWidgets('only an editable review offers editing', (tester) async {
      MyReview? edited;
      await tester.pumpWidget(host(MyReviewsList(
        reviews: [
          mine('a', ReviewModerationStatus.pending, canEdit: true),
          mine('c', ReviewModerationStatus.rejected, reason: 'x'),
        ],
        onEdit: (r) => edited = r,
      )));

      expect(find.byKey(const Key('my-review-a-edit')), findsOneWidget);
      expect(find.byKey(const Key('my-review-c-edit')), findsNothing);
      await tester.tap(find.byKey(const Key('my-review-a-edit')));
      expect(edited?.id, 'a');
    });

    testWidgets('none yet is said plainly', (tester) async {
      await tester.pumpWidget(host(const MyReviewsList(reviews: [])));
      expect(find.text(AppStrings.myReviewsEmpty), findsOneWidget);
    });

    testWidgets('the page loads them, and a failure can be retried',
        (tester) async {
      final reviews = _FakeReviews()..mineFailure = const ServerFailure('قطع شد');
      await tester.pumpWidget(MaterialApp(home: MyReviewsPage(repository: reviews)));
      await tester.pumpAndSettle();

      expect(find.text('قطع شد'), findsOneWidget);

      reviews
        ..mineFailure = null
        ..mine = [mine('a', ReviewModerationStatus.pending)];
      await tester.tap(find.byKey(const Key('my-reviews-retry')));
      await tester.pumpAndSettle();

      expect(find.text('سالن a'), findsOneWidget);
    });
  });

  test('a new review is announced as awaiting approval, not as live', () {
    expect(AppStrings.reviewSaved, contains('تأیید'));
  });
}

class _NoBookings implements BookingRepository {
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _FakeReviews implements ReviewRepository {
  ProviderReviews listing = const ProviderReviews();
  ReviewVoteResult? voteResult;
  Failure? voteFailure;
  List<MyReview> mine = const [];
  Failure? mineFailure;
  final votes = <(String, bool)>[];

  @override
  Future<Either<Failure, ProviderReviews>> getProviderReviews(String providerId) async =>
      Right(listing);

  @override
  Future<Either<Failure, ReviewVoteResult>> vote(String reviewId, bool isHelpful) async {
    votes.add((reviewId, isHelpful));
    return voteFailure != null ? Left(voteFailure!) : Right(voteResult!);
  }

  @override
  Future<Either<Failure, List<MyReview>>> getMyReviews() async =>
      mineFailure != null ? Left(mineFailure!) : Right(mine);

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
