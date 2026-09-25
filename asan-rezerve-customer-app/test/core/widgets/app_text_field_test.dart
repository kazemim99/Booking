import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/widgets/app_text_field.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The character counter is opt-in: a name or a phone number with a
/// maxLength has no use for «0/20» under it, but a long comment does.
void main() {
  Future<void> pump(WidgetTester tester, AppTextField field) =>
      tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: field),
        ),
      ));

  testWidgets('a maxLength alone shows no counter', (tester) async {
    await pump(tester, const AppTextField(maxLength: 20));

    expect(find.text('0/20'), findsNothing);
  });

  testWidgets('showCounter shows how much of the maxLength is used',
      (tester) async {
    await pump(
        tester, const AppTextField(maxLength: 20, showCounter: true));

    expect(find.text('0/20'), findsOneWidget);
    await tester.enterText(find.byType(TextField), 'سلام');
    await tester.pump();
    expect(find.text('4/20'), findsOneWidget);
  });
}
