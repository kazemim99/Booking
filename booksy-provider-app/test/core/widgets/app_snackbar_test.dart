import 'package:booksy_provider_app/core/widgets/app_snackbar.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A snackbar must never sit on top of the screen's primary action. The
/// login-success snackbar carried over into the onboarding wizard and covered
/// its "بعدی" button, so the step could not be submitted at all.
void main() {
  Future<Rect> showAndMeasure(WidgetTester tester) async {
    late BuildContext ctx;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          // Mirrors StepScaffold: the action row lives INSIDE the body, at the
          // bottom of a Column — not in Scaffold.bottomNavigationBar, which is
          // the only case Flutter lifts a docked snackbar above by itself.
          body: Builder(
            builder: (context) {
              ctx = context;
              return Column(
                children: [
                  const Expanded(child: SizedBox.expand()),
                  SafeArea(
                    top: false,
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: FilledButton(
                        key: const Key('primary-action'),
                        onPressed: () {},
                        child: const Text('بعدی'),
                      ),
                    ),
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );

    AppSnackbar.success(ctx, 'ورود موفقیت‌آمیز بود!');
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 750));
    // The SnackBar element spans its margin too; measure the visible surface.
    return tester.getRect(
      find
          .descendant(of: find.byType(SnackBar), matching: find.byType(Material))
          .first,
    );
  }

  testWidgets('does not overlap the bottom action button', (tester) async {
    final snackRect = await showAndMeasure(tester);
    final buttonRect = tester.getRect(find.byKey(const Key('primary-action')));

    expect(snackRect.overlaps(buttonRect), isFalse,
        reason: 'snack $snackRect must clear the action button $buttonRect');
  });

  testWidgets('floats, so it is clearly a transient message', (tester) async {
    await showAndMeasure(tester);
    final snack = tester.widget<SnackBar>(find.byType(SnackBar));

    expect(snack.behavior, SnackBarBehavior.floating);
  });
}
