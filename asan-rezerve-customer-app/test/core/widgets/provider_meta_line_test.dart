import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/utils/price_formatter.dart';
import 'package:asan_rezerve_customer_app/core/widgets/provider_meta_line.dart';

/// The provider meta line (customer-app-ux-review-fixes F.1): a Toman app says
/// what a salon costs as «از ۱۲۰٬۰۰۰ تومان», never as `$$`, and a separator
/// never hangs at the end of a line when the parts wrap.

Widget _wrap(Widget child, {double width = 360, double textScale = 1.0}) =>
    MaterialApp(
      theme: AppTheme.light,
      builder: (context, appChild) => MediaQuery(
        data: MediaQuery.of(context)
            .copyWith(textScaler: TextScaler.linear(textScale)),
        child: Directionality(
          textDirection: TextDirection.rtl,
          child: appChild!,
        ),
      ),
      home: Scaffold(
        body: Align(
          alignment: AlignmentDirectional.topStart,
          child: SizedBox(width: width, child: child),
        ),
      ),
    );

final _today = DateTime(2026, 9, 23, 10);

void main() {
  group('starting price', () {
    testWidgets('shows «از … تومان» in Persian digits, never a dollar band',
        (tester) async {
      await tester.pumpWidget(_wrap(ProviderMetaLine(
        category: 'پارس‌آباد',
        startingPrice: 120000,
        now: _today,
      )));

      expect(find.text(PriceFormatter.formatFrom(120000)), findsOneWidget);
      expect(find.textContaining(r'$'), findsNothing);
      expect(find.textContaining('120'), findsNothing,
          reason: 'user-visible numbers are Persian digits');
    });

    testWidgets('is hidden when the price is unknown or zero', (tester) async {
      // The search payload's startingPrice is 0 for every salon today.
      final zero = ProviderMetaLine(startingPrice: 0, now: _today);
      expect(zero.hasContent, isFalse);
      final unknown = ProviderMetaLine(now: _today);
      expect(unknown.hasContent, isFalse);

      await tester.pumpWidget(_wrap(ProviderMetaLine(
        category: 'پارس‌آباد',
        startingPrice: 0,
        now: _today,
      )));
      expect(find.textContaining('تومان'), findsNothing);
      expect(find.text('·'), findsNothing);
    });

    test('a known price alone is content', () {
      expect(ProviderMetaLine(startingPrice: 90000, now: _today).hasContent,
          isTrue);
    });
  });

  group('wrapping', () {
    // Every part the line can carry, long enough to wrap on a card.
    Widget meta() => ProviderMetaLine(
          category: 'پارس‌آباد',
          rating: 0,
          reviewCount: 0,
          startingPrice: 120000,
          nextFreeDate: _today,
          freeSlotCount: 5,
          distanceKm: 12.7,
          now: _today,
        );

    /// A separator belongs to the part after it: on its own line, something
    /// must follow it (to its left, in RTL). One that ends a line dangles.
    void expectNoDanglingSeparator(WidgetTester tester, double width) {
      final texts = find.descendant(
        of: find.byType(ProviderMetaLine),
        matching: find.byType(Text),
      );
      final dots = <Rect>[];
      final others = <Rect>[];
      for (final element in texts.evaluate()) {
        final text = element.widget as Text;
        final rect = tester.getRect(find.byWidget(text));
        (text.data == '·' ? dots : others).add(rect);
      }
      expect(dots, isNotEmpty);
      for (final dot in dots) {
        final followed = others.any((part) =>
            (part.center.dy - dot.center.dy).abs() < dot.height / 2 &&
            part.right <= dot.left + 0.5);
        expect(followed, isTrue,
            reason: 'a «·» ends a line at width $width');
      }
    }

    testWidgets('never leaves «·» at the end of a line, at any card width',
        (tester) async {
      for (var width = 140.0; width <= 360; width += 4) {
        await tester.pumpWidget(_wrap(meta(), width: width));
        expectNoDanglingSeparator(tester, width);
        expect(tester.takeException(), isNull);
      }
    });

    testWidgets('holds at 1.3x text scale on a narrow card', (tester) async {
      for (var width = 160.0; width <= 300; width += 7) {
        await tester.pumpWidget(_wrap(meta(), width: width, textScale: 1.3));
        expectNoDanglingSeparator(tester, width);
        expect(tester.takeException(), isNull);
      }
    });

    testWidgets('still separates every part it shows', (tester) async {
      await tester.pumpWidget(_wrap(meta(), width: 1000));
      // category, no-reviews, price, free slots, distance → four separators.
      expect(find.text('·'), findsNWidgets(4));
      expect(find.text(AppStrings.noReviewsYet), findsOneWidget);
    });
  });
}
