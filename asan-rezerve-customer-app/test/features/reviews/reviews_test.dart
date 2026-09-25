import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/widgets/app_button.dart';
import 'package:asan_rezerve_customer_app/features/reviews/domain/entities/review.dart';
import 'package:asan_rezerve_customer_app/features/reviews/presentation/widgets/provider_reviews_section.dart';
import 'package:asan_rezerve_customer_app/features/reviews/presentation/widgets/write_review_dialog.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/review_dialog.dart';

/// Reading a salon's reviews, and leaving one after a visit
/// (openspec/changes/customer-app-discovery-pass).
void main() {
  Future<void> pumpSection(WidgetTester tester,
      {ProviderReviews? reviews,
      bool loading = false,
      bool failed = false,
      VoidCallback? onRetry,
      VoidCallback? onLoadMore,
      String? salonName}) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(
            body: SingleChildScrollView(
              child: ProviderReviewsSection(
                reviews: reviews,
                loading: loading,
                failed: failed,
                onRetry: onRetry,
                onLoadMore: onLoadMore,
                salonName: salonName,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();
  }

  List<Review> many(int n) => [
        for (var i = 1; i <= n; i++)
          Review(id: 'r$i', customerName: 'مریم ر.', rating: 5, comment: 'عالی بود، ممنون از شما $i', isVerified: true),
      ];

  // openspec/changes/_inline/customer-reviews-and-nahal-seed
  group('the reviews section, as a customer reads it', () {
    testWidgets('a failed read says so with a retry, and never «no reviews yet»', (tester) async {
      var retried = 0;
      await pumpSection(tester, failed: true, onRetry: () => retried++);

      expect(find.byKey(const Key('provider-reviews-failed')), findsOneWidget);
      expect(find.text(AppStrings.reviewsEmpty), findsNothing);
      await tester.tap(find.byKey(const Key('provider-reviews-retry')));
      expect(retried, 1);
    });

    testWidgets('three reviews show first; the rest are one tap away', (tester) async {
      await pumpSection(tester, reviews: ProviderReviews(averageRating: 5, totalReviews: 18, items: many(18)));

      expect(find.byKey(const Key('review-r3')), findsOneWidget);
      expect(find.byKey(const Key('review-r4')), findsNothing);
      expect(find.text(AppStrings.reviewsShowAll('۱۸')), findsOneWidget);

      await tester.tap(find.byKey(const Key('provider-reviews-show-all')));
      await tester.pump();

      expect(find.byKey(const Key('review-r18')), findsOneWidget);
      expect(find.byKey(const Key('provider-reviews-show-all')), findsNothing);
    });

    testWidgets('when the server has more, «نظرهای بیشتر» asks for them', (tester) async {
      var asked = 0;
      await pumpSection(
        tester,
        reviews: ProviderReviews(averageRating: 5, totalReviews: 25, hasMore: true, items: many(20)),
        onLoadMore: () => asked++,
      );

      await tester.tap(find.byKey(const Key('provider-reviews-show-all')));
      await tester.pump();
      await tester.ensureVisible(find.byKey(const Key('provider-reviews-more')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('provider-reviews-more')));

      expect(asked, 1);
    });

    testWidgets('each review says it followed a real visit, and how the stars fall is shown', (tester) async {
      await pumpSection(
        tester,
        reviews: ProviderReviews(
          averageRating: 4,
          totalReviews: 3,
          distribution: const {5: 2, 4: 0, 3: 0, 2: 0, 1: 1},
          items: many(3),
        ),
      );

      expect(find.byKey(const Key('review-r1-verified')), findsOneWidget);
      expect(find.textContaining(AppStrings.reviewVerifiedVisit, findRichText: true), findsNWidgets(3));
      expect(find.byKey(const Key('review-distribution')), findsOneWidget);
    });

    testWidgets('fits a 360 phone at 1.3x text', (tester) async {
      tester.view.physicalSize = const Size(360 * 3, 640 * 3);
      tester.view.devicePixelRatio = 3;
      addTearDown(tester.view.reset);
      await tester.pumpWidget(MaterialApp(
        home: MediaQuery(
          data: const MediaQueryData(size: Size(360, 640), textScaler: TextScaler.linear(1.3)),
          child: Directionality(
            textDirection: TextDirection.rtl,
            child: Scaffold(
              body: SingleChildScrollView(
                child: ProviderReviewsSection(
                  reviews: ProviderReviews(
                    averageRating: 4,
                    totalReviews: 18,
                    distribution: const {5: 10, 4: 4, 3: 2, 2: 1, 1: 1},
                    items: [
                      ...many(2),
                      const Review(
                        id: 'rx',
                        customerName: 'نیلوفر ص.',
                        rating: 2,
                        comment: 'یک ساعت منتظر موندم و آخرش هم با عجله کارم رو انجام دادن.',
                        providerResponse: 'از اینکه تجربه خوبی نداشتید واقعاً متأسفیم. لطفاً با ما تماس بگیرید.',
                        isVerified: true,
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ));
      await tester.pump();

      expect(tester.takeException(), isNull);
    });
  });

  group('the reviews on a profile', () {
    testWidgets('a salon nobody reviewed says so, rather than showing nothing',
        (tester) async {
      await pumpSection(tester, reviews: const ProviderReviews());

      expect(find.byKey(const Key('provider-reviews-empty')), findsOneWidget);
      expect(find.text(AppStrings.reviewsEmpty), findsOneWidget);
    });

    testWidgets('what people wrote is shown, with the salon\'s reply',
        (tester) async {
      await pumpSection(
        tester,
        reviews: ProviderReviews(
          averageRating: 4.5,
          totalReviews: 2,
          items: [
            Review(
              id: 'r1',
              customerName: 'سارا احمدی',
              rating: 5,
              comment: 'خیلی راضی بودم، حتماً دوباره می‌آیم.',
              createdAt: DateTime(2026, 9, 18),
              providerResponse: 'ممنون از شما!',
            ),
            const Review(id: 'r2', customerName: '', rating: 4),
          ],
        ),
      );

      expect(find.byKey(const Key('review-r1')), findsOneWidget);
      expect(find.text('سارا احمدی'), findsOneWidget);
      expect(find.text('خیلی راضی بودم، حتماً دوباره می‌آیم.'), findsOneWidget);
      expect(find.text('ممنون از شما!'), findsOneWidget);
      // A review with no name is still someone's review.
      expect(find.text(AppStrings.reviewAnonymous), findsOneWidget);
    });
  });

  // reviews-and-reschedule-round2 item 7: the reply read as a separate grey note, not as the answer to that review.
  group('the salon\'s reply', () {
    ProviderReviews withReply() => const ProviderReviews(
          averageRating: 4,
          totalReviews: 1,
          items: [
            Review(id: 'r1', customerName: 'سارا احمدی', rating: 4, comment: 'خوب بود ولی کمی منتظر ماندم',
                providerResponse: 'ممنون از صبرتان؛ جبران می‌کنیم.'),
          ],
        );

    testWidgets('is signed with the salon\'s name, a reply icon and its picture', (tester) async {
      await pumpSection(tester, reviews: withReply(), salonName: 'سالن نهال');

      final reply = find.byKey(const Key('review-r1-reply'));
      expect(reply, findsOneWidget);
      expect(find.descendant(of: reply, matching: find.text(AppStrings.reviewProviderReplyFrom('سالن نهال'))),
          findsOneWidget);
      expect(find.descendant(of: reply, matching: find.byIcon(Icons.reply_rounded)), findsOneWidget);
      expect(find.descendant(of: reply, matching: find.byIcon(Icons.storefront_outlined)), findsOneWidget,
          reason: 'no logo: the storefront glyph in the avatar');
      expect(find.descendant(of: reply, matching: find.byKey(const Key('salon-reply-connector'))), findsOneWidget);
    });

    testWidgets('says «پاسخ سالن» when the name is not at hand', (tester) async {
      await pumpSection(tester, reviews: withReply());

      expect(find.text(AppStrings.reviewProviderReply), findsOneWidget);
    });

    testWidgets('is indented under its review from the start edge (RTL: from the right)', (tester) async {
      await pumpSection(tester, reviews: withReply(), salonName: 'سالن نهال');

      final review = tester.getRect(find.byKey(const Key('review-r1')));
      final reply = tester.getRect(find.byKey(const Key('salon-reply-connector')));
      expect(reply.right, lessThan(review.right - 8), reason: 'indented from the right edge in RTL');
      final comment = tester.getRect(find.text('خوب بود ولی کمی منتظر ماندم'));
      expect(reply.top, greaterThan(comment.bottom), reason: 'under the review it answers');
    });

    testWidgets('is tinted from the theme\'s primary container, with a primary connector', (tester) async {
      await pumpSection(tester, reviews: withReply(), salonName: 'سالن نهال');
      final scheme = Theme.of(tester.element(find.byKey(const Key('review-r1-reply')))).colorScheme;

      final connector = tester.widget<Container>(find.byKey(const Key('salon-reply-connector')));
      expect((connector.decoration! as BoxDecoration).color, scheme.primary);
      final tinted = tester.widgetList<Container>(find.descendant(
          of: find.byKey(const Key('review-r1-reply')), matching: find.byType(Container)))
          .map((c) => c.decoration)
          .whereType<BoxDecoration>()
          .map((d) => d.color);
      expect(tinted, contains(scheme.primaryContainer.withValues(alpha: 0.35)));
    });
  });

  // QA recording 2026-09-23 #10: "where do I leave my review?" — the profile only shows reviews, and the only way
  // to write one is from a completed appointment, which nothing said.
  group('where a review is written', () {
    final cases = <String, ({ProviderReviews? reviews, bool loading})>{
      'no reviews yet': (reviews: const ProviderReviews(), loading: false),
      'some reviews': (
        reviews: const ProviderReviews(
          averageRating: 5,
          totalReviews: 1,
          items: [Review(id: 'r1', customerName: 'سارا', rating: 5)],
        ),
        loading: false,
      ),
      'still loading': (reviews: null, loading: true),
    };
    for (final entry in cases.entries) {
      testWidgets('is said under the heading — ${entry.key}', (tester) async {
        await pumpSection(tester, reviews: entry.value.reviews, loading: entry.value.loading);

        final hint = find.byKey(const Key('provider-reviews-how-to'));
        expect(hint, findsOneWidget);
        expect(find.text(AppStrings.reviewsHowToWrite), findsOneWidget);
        expect(tester.getTopLeft(hint).dy,
            greaterThan(tester.getTopLeft(find.text(AppStrings.reviewsTitle)).dy));
      });
    }

    test('it names the appointments tab it points to', () {
      expect(AppStrings.reviewsHowToWrite, contains(AppStrings.appointmentsTitle));
    });
  });

  group('leaving a review', () {
    Future<ReviewDraft?> open(WidgetTester tester) async {
      ReviewDraft? result;
      await tester.pumpWidget(
        MaterialApp(
          home: Directionality(
            textDirection: TextDirection.rtl,
            child: Builder(
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
        ),
      );
      await tester.tap(find.byKey(const Key('open')));
      await tester.pumpAndSettle();
      return result;
    }

    testWidgets('all four aspects are required; the words are not', (tester) async {
      await open(tester);

      AppButton submit() => tester.widget<AppButton>(find.byKey(const Key('review-submit')));
      expect(submit().onPressed, isNull, reason: 'nothing rated yet');

      // Three of four is not a review yet.
      for (final d in ReviewDimension.values.take(3)) {
        await tester.ensureVisible(find.byKey(Key('review-dim-${d.name}-4')));
        await tester.tap(find.byKey(Key('review-dim-${d.name}-4')));
        await tester.pump();
      }
      expect(submit().onPressed, isNull);

      await rateAllAspects(tester, stars: 4);
      expect(submit().onPressed, isNotNull);
      await tester.tap(find.byKey(const Key('review-submit')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('review-submit')), findsNothing,
          reason: 'four aspects without a word is a complete review');
    });

    testWidgets('a comment too short for the server is caught here first',
        (tester) async {
      await open(tester);

      await rateAllAspects(tester, stars: 5);
      await tester.enterText(find.byKey(const Key('review-comment')), 'خوب');
      await tester.tap(find.byKey(const Key('review-submit')));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.reviewCommentTooShort), findsOneWidget);
    });
  });
}
