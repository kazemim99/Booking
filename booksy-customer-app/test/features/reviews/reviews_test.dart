import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/features/reviews/domain/entities/review.dart';
import 'package:booksy_customer_app/features/reviews/presentation/widgets/provider_reviews_section.dart';
import 'package:booksy_customer_app/features/reviews/presentation/widgets/write_review_dialog.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Reading a salon's reviews, and leaving one after a visit
/// (openspec/changes/customer-app-discovery-pass).
void main() {
  Future<void> pumpSection(WidgetTester tester,
      {ProviderReviews? reviews, bool loading = false}) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(
            body: ProviderReviewsSection(reviews: reviews, loading: loading),
          ),
        ),
      ),
    );
    await tester.pump();
  }

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

    testWidgets('stars are required; the words are not', (tester) async {
      await open(tester);

      await tester.tap(find.byKey(const Key('review-submit')));
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.reviewRatingRequired), findsOneWidget);

      await tester.tap(find.byKey(const Key('review-star-4')));
      await tester.tap(find.byKey(const Key('review-submit')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('review-submit')), findsNothing,
          reason: 'four stars alone is a complete review');
    });

    testWidgets('a comment too short for the server is caught here first',
        (tester) async {
      await open(tester);

      await tester.tap(find.byKey(const Key('review-star-5')));
      await tester.enterText(find.byKey(const Key('review-comment')), 'خوب');
      await tester.tap(find.byKey(const Key('review-submit')));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.reviewCommentTooShort), findsOneWidget);
    });
  });
}
