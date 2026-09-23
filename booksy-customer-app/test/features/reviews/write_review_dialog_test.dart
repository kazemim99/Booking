import 'package:booksy_customer_app/config/theme/app_colors.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/utils/persian_formatter.dart';
import 'package:booksy_customer_app/core/widgets/app_button.dart';
import 'package:booksy_customer_app/core/widgets/app_text_field.dart';
import 'package:booksy_customer_app/features/reviews/domain/entities/review.dart';
import 'package:booksy_customer_app/features/reviews/presentation/widgets/write_review_dialog.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

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

  testWidgets('each overall star says how many stars it is, and which is chosen',
      (tester) async {
    final semantics = tester.ensureSemantics();
    await open(tester)();

    for (var n = 1; n <= 5; n++) {
      expect(
        tester.getSemantics(find.byKey(Key('review-star-$n'))),
        isSemantics(
            tooltip: stars(n), isButton: true, hasSelectedState: true, isSelected: false),
      );
    }

    await tester.tap(find.byKey(const Key('review-star-3')));
    await tester.pumpAndSettle();

    expect(tester.getSemantics(find.byKey(const Key('review-star-3'))),
        isSemantics(isSelected: true));
    expect(tester.getSemantics(find.byKey(const Key('review-star-4'))),
        isSemantics(isSelected: false));
    semantics.dispose();
  });

  testWidgets('filled stars are amber, empty ones the muted ink', (tester) async {
    await open(tester)();
    await tester.tap(find.byKey(const Key('review-star-2')));
    await tester.pumpAndSettle();

    Icon icon(String key) => tester.widget<Icon>(find.descendant(
        of: find.byKey(Key(key)), matching: find.byType(Icon)));
    final scheme = AppTheme.light.colorScheme;

    expect(icon('review-star-2').icon, Icons.star);
    expect(icon('review-star-2').color, AppColors.warning);
    expect(icon('review-star-3').icon, Icons.star_border);
    expect(icon('review-star-3').color, scheme.onSurfaceVariant);
  });

  testWidgets('every star, overall and per dimension, is at least 48 dp',
      (tester) async {
    await open(tester)();
    await tester.tap(find.byKey(const Key('review-dimensions')));
    await tester.pumpAndSettle();

    final keys = [
      for (var n = 1; n <= 5; n++) 'review-star-$n',
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

  testWidgets('a dimension star still says what it rates', (tester) async {
    final semantics = tester.ensureSemantics();
    await open(tester)();
    await tester.tap(find.byKey(const Key('review-dimensions')));
    await tester.pumpAndSettle();

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
    testWidgets('fits a 360×640 phone at $scale× text, dimensions open',
        (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(360, 640);
      addTearDown(tester.view.reset);

      await open(tester, textScale: scale)();
      await tester.tap(find.byKey(const Key('review-dimensions')));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
    });
  }
}
