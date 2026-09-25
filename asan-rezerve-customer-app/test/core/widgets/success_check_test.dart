import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/widgets/success_check.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The tick shown when something worked (QA walkthrough 2026-09-22: "a clean transition with a check mark that
/// disappears — a message is not needed").
void main() {
  testWidgets('appears over the screen and removes itself', (tester) async {
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      home: Builder(
        builder: (context) => Scaffold(
          body: TextButton(
            key: const Key('go'),
            onPressed: () => SuccessCheck.show(context),
            child: const Text('go'),
          ),
        ),
      ),
    ));

    await tester.tap(find.byKey(const Key('go')));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 320));

    expect(find.byKey(const Key('success-check')), findsOneWidget);
    expect(find.byIcon(Icons.check_rounded), findsOneWidget);

    await tester.pump(SuccessCheck.visibleFor);
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('success-check')), findsNothing,
        reason: 'it gets out of the way on its own');
  });
}
