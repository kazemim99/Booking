import 'dart:async' show unawaited;

import 'package:booksy_provider_app/core/widgets/app_confirm_dialog.dart';
import 'package:booksy_provider_app/core/widgets/app_dialog_header.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('shows the header title and message', (tester) async {
    bool? result;
    late BuildContext ctx;

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) {
            ctx = context;
            return const Scaffold(body: SizedBox());
          },
        ),
      ),
    );

    unawaited(
      showAppConfirmDialog(ctx, title: 'حذف', message: 'مطمئن هستید؟')
          .then((v) => result = v),
    );
    await tester.pumpAndSettle();

    expect(find.byType(AppDialogHeader), findsOneWidget);
    expect(find.text('حذف'), findsOneWidget);
    expect(find.text('مطمئن هستید؟'), findsOneWidget);
    expect(result, isNull);
  });

  testWidgets('tapping cancel resolves false', (tester) async {
    bool? result;
    late BuildContext ctx;

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) {
            ctx = context;
            return const Scaffold(body: SizedBox());
          },
        ),
      ),
    );

    unawaited(
      showAppConfirmDialog(ctx, title: 'حذف', message: 'مطمئن هستید؟')
          .then((v) => result = v),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('انصراف'));
    await tester.pumpAndSettle();

    expect(result, isFalse);
  });

  testWidgets('tapping confirm resolves true', (tester) async {
    bool? result;
    late BuildContext ctx;

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) {
            ctx = context;
            return const Scaffold(body: SizedBox());
          },
        ),
      ),
    );

    unawaited(
      showAppConfirmDialog(
        ctx,
        title: 'حذف',
        message: 'مطمئن هستید؟',
        confirmLabel: 'حذف کن',
      ).then((v) => result = v),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('confirm-dialog-confirm')));
    await tester.pumpAndSettle();

    expect(result, isTrue);
  });

  testWidgets('non-destructive variant uses the primary role button',
      (tester) async {
    late BuildContext ctx;

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) {
            ctx = context;
            return const Scaffold(body: SizedBox());
          },
        ),
      ),
    );

    unawaited(
      showAppConfirmDialog(
        ctx,
        title: 'ادامه',
        message: 'آیا ادامه می‌دهید؟',
        destructive: false,
      ),
    );
    await tester.pumpAndSettle();

    final confirmButton = tester.widget<FilledButton>(
      find.descendant(
        of: find.byKey(const Key('confirm-dialog-confirm')),
        matching: find.byType(FilledButton),
      ),
    );
    // Primary role: no destructive-red background override.
    expect(
      confirmButton.style?.backgroundColor?.resolve({}),
      isNot(const Color(0xFFFF6171)),
    );
  });
}
