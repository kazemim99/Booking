import 'package:asan_rezerve_customer_app/config/theme/app_colors.dart';
import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/utils/persian_formatter.dart';
import 'package:asan_rezerve_customer_app/core/widgets/app_button.dart';
import 'package:asan_rezerve_customer_app/core/widgets/app_text_field.dart';
import 'package:asan_rezerve_customer_app/features/reviews/domain/entities/review.dart';
import 'package:asan_rezerve_customer_app/features/reviews/presentation/widgets/write_review_dialog.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/review_dialog.dart';

/// The review dialog on the app's own components, and stars a screen reader and a thumb can both use
/// (UX review 2026-09-23, G.1: it was the one screen on FilledButton and a raw outline border, the overall
/// stars had no label, and the dimension stars were ~30 px targets).
void main() {
  Future<ReviewDraft?> Function() open(WidgetTester tester,
      {double textScale = 1.0, ReviewDraft? initial}) {
    ReviewDraft? result;
    return () async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => MediaQuery(
            data: MediaQuery.of(context)
                .copyWith(textScaler: TextScaler.linear(textScale)),
            child: Directionality(
                textDirection: TextDirection.rtl, child: child!),
          ),
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

  String stars(int n) => '${PersianFormatter.toPersianDigits('$n')} ستاره';

  testWidgets('its buttons and comment field are the shared ones', (tester) async {
    await open(tester)();

    expect(find.byType(FilledButton), findsNothing);
    expect(find.byType(AppButton), findsNWidgets(2),
        reason: 'cancel (secondary) and submit (primary)');
    expect(
      find.descendant(
          of: find.byKey(const Key('review-submit')),
          matching: find.byType(ElevatedButton)),
      findsOneWidget,
      reason: 'submit is the primary AppButton',
    );
    expect(
      find.ancestor(
          of: find.byKey(const Key('review-comment')),
          matching: find.byType(AlertDialog)),
      findsOneWidget,
    );
    expect(tester.widget(find.byKey(const Key('review-comment'))),
        isA<AppTextField>());
    final field = tester.widget<TextField>(find.descendant(
        of: find.byKey(const Key('review-comment')),
        matching: find.byType(TextField)));
    expect(field.maxLines, 3, reason: 'a comment is a few lines, not one');
    final themed = AppTheme.light.inputDecorationTheme;
    expect(field.decoration?.border, themed.border,
        reason: 'the border comes from the theme, not a raw OutlineInputBorder');
    expect(field.decoration?.enabledBorder, themed.enabledBorder);
  });

  testWidgets('the comment shows how much of its 2000 characters is used',
      (tester) async {
    await open(tester)();

    final comment = find.byKey(const Key('review-comment'));
    expect(
        find.descendant(of: comment, matching: find.text('0/2000')),
        findsOneWidget);
    await tester.enterText(
        find.descendant(of: comment, matching: find.byType(TextField)),
        'خیلی خوب بود');
    await tester.pump();
    expect(
        find.descendant(of: comment, matching: find.text('12/2000')),
        findsOneWidget);
  });

  // reviews-and-reschedule-round2 D1: no separate overall row; the four aspects are the review.
  testWidgets('shows the four aspects directly, with no overall row and no disclosure', (tester) async {
    await open(tester)();

    for (final d in ReviewDimension.values) {
      expect(find.text(d.label), findsOneWidget);
      expect(find.byKey(Key('review-dim-${d.name}-1')), findsOneWidget);
    }
    expect(find.byType(ExpansionTile), findsNothing);
    expect(find.byKey(const Key('review-star-1')), findsNothing);
    expect(find.text('امتیاز شما'), findsNothing);
  });

  testWidgets('the overall is live: the aspects\' average to the nearest half, in Persian digits', (tester) async {
    await open(tester)();
    expect(find.byKey(const Key('review-overall')), findsNothing, reason: 'nothing to average yet');

    // 3, 3, 4, 3 → 3.25 → ۳.۵, shown with a half star.
    await rateAllAspects(tester, each: {
      ReviewDimension.cleanliness: 3,
      ReviewDimension.skill: 3,
      ReviewDimension.punctuality: 4,
      ReviewDimension.conduct: 3,
    });
    expect(find.text(AppStrings.reviewOverallLabel('۳.۵')), findsOneWidget);
    Icon star(int n) => tester.widget<Icon>(find.byKey(Key('review-overall-star-$n')));
    expect(star(3).icon, Icons.star_rounded);
    expect(star(4).icon, Icons.star_half_rounded);
    expect(star(5).icon, Icons.star_border_rounded);

    // One more star on conduct: 3, 3, 4, 4 → 3.5; then 4, 3, 4, 4 → 3.75 → ۴.۰.
    await tester.tap(find.byKey(const Key('review-dim-conduct-4')));
    await tester.pump();
    expect(find.text(AppStrings.reviewOverallLabel('۳.۵')), findsOneWidget);
    await tester.ensureVisible(find.byKey(const Key('review-dim-cleanliness-4')));
    await tester.tap(find.byKey(const Key('review-dim-cleanliness-4')));
    await tester.pump();
    expect(find.text(AppStrings.reviewOverallLabel('۴.۰')), findsOneWidget);
  });

  testWidgets('submit waits for all four aspects', (tester) async {
    await open(tester)();

    AppButton submit() => tester.widget<AppButton>(find.byKey(const Key('review-submit')));
    expect(submit().onPressed, isNull);
    await rateAllAspects(tester, stars: 4);
    expect(submit().onPressed, isNotNull);
    await tester.tap(find.byKey(const Key('review-submit')));
    await tester.pumpAndSettle();
    expect(find.byType(AlertDialog), findsNothing);
  });

  group('ReviewDraft.overallOf', () {
    double of(List<double> v) => ReviewDraft.overallOf({
          for (var i = 0; i < v.length; i++) ReviewDimension.values[i]: v[i],
        });

    test('rounds to the nearest half, halves away from zero', () {
      expect(of([3, 3, 4, 3]), 3.5, reason: '3.25');
      expect(of([4, 3, 4, 4]), 4.0, reason: '3.75');
      expect(of([5, 5, 5, 5]), 5.0);
      expect(of([1, 1, 1, 2]), 1.5, reason: '1.25');
      expect(of([4, 4, 5, 4]), 4.5, reason: '4.25');
      expect(of([1, 1, 1, 1]), 1.0);
    });

    test('nothing rated is zero', () {
      expect(ReviewDraft.overallOf(const {}), 0);
    });
  });

  group('the name choice', () {
    Future<ReviewDraft?> submitWith(WidgetTester tester,
        {ReviewDraft? initial, bool tick = false, Map<ReviewDimension, int>? each}) async {
      ReviewDraft? result;
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: Builder(
          builder: (context) => Scaffold(
            body: TextButton(
              key: const Key('open'),
              onPressed: () async => result = await showWriteReviewDialog(context, initial: initial),
              child: const Text('open'),
            ),
          ),
        ),
      ));
      await tester.tap(find.byKey(const Key('open')));
      await tester.pumpAndSettle();
      if (initial == null) await rateAllAspects(tester, stars: 4, each: each);
      if (tick) {
        await tester.ensureVisible(find.byKey(const Key('review-hide-name')));
        await tester.tap(find.byKey(const Key('review-hide-name')));
        await tester.pump();
      }
      await tester.tap(find.byKey(const Key('review-submit')));
      await tester.pumpAndSettle();
      return result;
    }

    testWidgets('is unticked by default: the name signs the review', (tester) async {
      final draft = await submitWith(tester);
      expect(find.text(AppStrings.reviewHideName), findsNothing, reason: 'dialog closed');
      expect(draft?.showName, isTrue);
      expect(draft?.rating, 4);
      expect(draft?.dimensions.length, 4);
    });

    testWidgets('the draft carries the aspects and their rounded average', (tester) async {
      final draft = await submitWith(tester, each: {
        ReviewDimension.cleanliness: 5,
        ReviewDimension.skill: 4,
        ReviewDimension.punctuality: 4,
        ReviewDimension.conduct: 4,
      });
      expect(draft?.rating, 4.5, reason: '4.25 → 4.5');
      expect(draft?.dimensions[ReviewDimension.cleanliness], 5);
    });

    testWidgets('ticked, the review is signed «مشتری»', (tester) async {
      final draft = await submitWith(tester, tick: true);
      expect(draft?.showName, isFalse);
    });

    testWidgets('an edit opens with the review\'s own choice, and can change it', (tester) async {
      const hidden = ReviewDraft(rating: 4, showName: false, dimensions: {
        ReviewDimension.cleanliness: 4,
        ReviewDimension.skill: 4,
        ReviewDimension.punctuality: 4,
        ReviewDimension.conduct: 4,
      });
      expect((await submitWith(tester, initial: hidden))?.showName, isFalse);
      expect((await submitWith(tester, initial: hidden, tick: true))?.showName, isTrue);
    });

    testWidgets('an old review with only an overall star opens with each aspect at that star, ready to save',
        (tester) async {
      final draft = await submitWith(tester, initial: const ReviewDraft(rating: 4.5));
      expect(draft?.dimensions, {for (final d in ReviewDimension.values) d: 5.0},
          reason: '4.5 rounds to 5 for a whole-star aspect');
      expect(draft?.rating, 5);
      expect(draft?.showName, isTrue);
    });
  });

  testWidgets('filled stars are the one star colour, empty ones the muted ink', (tester) async {
    await open(tester)();
    await tester.tap(find.byKey(const Key('review-dim-cleanliness-2')));
    await tester.pumpAndSettle();

    Icon icon(String key) => tester.widget<Icon>(find.descendant(
        of: find.byKey(Key(key)), matching: find.byType(Icon)));
    final scheme = AppTheme.light.colorScheme;

    expect(icon('review-dim-cleanliness-2').icon, Icons.star);
    // Was AppColors.warning (1.52:1 on white); the star colour is now the AA-graphic AppColors.star.
    expect(icon('review-dim-cleanliness-2').color, AppColors.star);
    expect(icon('review-dim-cleanliness-3').icon, Icons.star_border);
    expect(icon('review-dim-cleanliness-3').color, scheme.onSurfaceVariant);
  });

  testWidgets('every aspect star is at least 48 dp', (tester) async {
    await open(tester)();

    final keys = [
      for (final d in ReviewDimension.values)
        for (var n = 1; n <= 5; n++) 'review-dim-${d.name}-$n',
    ];
    for (final key in keys) {
      final size = tester.getSize(find.byKey(Key(key)));
      expect(size.width, greaterThanOrEqualTo(48), reason: key);
      expect(size.height, greaterThanOrEqualTo(48), reason: key);
    }

    // The label sits above its row, so five 48 dp stars have the dialog's whole width.
    final label = tester.getRect(find.text(ReviewDimension.cleanliness.label));
    final star = tester.getRect(find.byKey(const Key('review-dim-cleanliness-1')));
    expect(label.bottom, lessThanOrEqualTo(star.top));
  });

  testWidgets('an aspect star says what it rates, and which is chosen', (tester) async {
    final semantics = tester.ensureSemantics();
    await open(tester)();

    for (var n = 1; n <= 5; n++) {
      expect(
        tester.getSemantics(find.byKey(Key('review-dim-cleanliness-$n'))),
        isSemantics(isButton: true, hasSelectedState: true, isSelected: false),
      );
    }

    await tester.tap(find.byKey(const Key('review-dim-skill-4')));
    await tester.pumpAndSettle();

    final node = tester.getSemantics(find.byKey(const Key('review-dim-skill-4')));
    expect(node.tooltip, contains(ReviewDimension.skill.label));
    expect(node.tooltip, contains(stars(4)));
    expect(node, isSemantics(isSelected: true));
    semantics.dispose();
  });

  // Both scales: Material's dialog padding shrinks as text grows, so 1.0× is the tighter fit for the stars.
  for (final scale in [1.0, 1.3]) {
    testWidgets('fits a 360×640 phone at $scale× text', (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(360, 640);
      addTearDown(tester.view.reset);

      await open(tester, textScale: scale)();
      await rateAllAspects(tester, stars: 3);

      expect(tester.takeException(), isNull);
      // Every star lies inside the dialog: the 16 dp inset leaves a 328 dp
      // dialog and 280 dp of content for five 48 dp stars (240 dp).
      final dialog = tester.getRect(find.descendant(
          of: find.byType(AlertDialog), matching: find.byType(Material)).first);
      for (final d in ReviewDimension.values) {
        for (var star = 1; star <= 5; star++) {
          final rect = tester.getRect(find.byKey(Key('review-dim-${d.name}-$star')));
          expect(rect.left, greaterThanOrEqualTo(dialog.left), reason: '${d.name}-$star');
          expect(rect.right, lessThanOrEqualTo(dialog.right), reason: '${d.name}-$star');
        }
      }
    });
  }
}
