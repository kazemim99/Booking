import 'package:asan_rezerve_customer_app/core/widgets/forward_chevron.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A row's chevron points the way the reader moves: left in Persian (QA walkthrough 2026-09-22 — "the arrow on
/// ویرایش پروفایل points right, it should point left; it is like that everywhere").
void main() {
  Future<void> pump(WidgetTester tester, TextDirection direction) => tester.pumpWidget(
        MaterialApp(
          home: Directionality(
            textDirection: direction,
            child: const Scaffold(body: ForwardChevron()),
          ),
        ),
      );

  testWidgets('is the mirroring icon, never a hand-picked left one', (tester) async {
    await pump(tester, TextDirection.rtl);

    final icon = tester.widget<Icon>(find.byType(Icon));
    expect(icon.icon, Icons.chevron_right);
    expect(icon.icon!.matchTextDirection, isTrue,
        reason: 'Flutter mirrors it in RTL; choosing chevron_left as well flips it back');
  });

  testWidgets('renders in both directions without exception', (tester) async {
    await pump(tester, TextDirection.ltr);
    expect(tester.takeException(), isNull);
  });
}
