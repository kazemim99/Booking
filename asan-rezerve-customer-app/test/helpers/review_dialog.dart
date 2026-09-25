import 'package:asan_rezerve_customer_app/features/reviews/domain/entities/review.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';

/// Rates every aspect in the open review dialog: [stars] for all four, or one
/// value per aspect from [each] (the four are the review, reviews-and-reschedule-round2 D1).
Future<void> rateAllAspects(WidgetTester tester,
    {int stars = 5, Map<ReviewDimension, int>? each}) async {
  for (final d in ReviewDimension.values) {
    final star = find.byKey(Key('review-dim-${d.name}-${each?[d] ?? stars}'));
    await tester.ensureVisible(star);
    await tester.pump();
    await tester.tap(star);
    await tester.pump();
  }
}
