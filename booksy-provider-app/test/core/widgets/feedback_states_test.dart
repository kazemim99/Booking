import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/config/theme/app_tokens.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/widgets/app_empty_state.dart';
import 'package:booksy_provider_app/core/widgets/app_error_state.dart';
import 'package:booksy_provider_app/core/widgets/app_loading.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Feedback-state components (spec: feedback-states): the canonical
/// loading / empty / error presentations every screen composes.
void main() {
  Future<void> pump(WidgetTester tester, Widget child) {
    return tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: child),
        ),
      ),
    );
  }

  group('AppLoading', () {
    testWidgets('renders a brand-primary spinner', (tester) async {
      await pump(tester, const AppLoading());
      await tester.pump();
      final spinner = tester.widget<CircularProgressIndicator>(
          find.byType(CircularProgressIndicator));
      expect(spinner.color, AppTheme.light.colorScheme.primary);
      expect(find.byType(Text), findsNothing);
    });

    testWidgets('shows the optional message in muted grey', (tester) async {
      await pump(tester, const AppLoading(message: 'در حال بارگذاری'));
      await tester.pump();
      final text = tester.widget<Text>(find.text('در حال بارگذاری'));
      expect(text.style?.color, AppColors.muted);
    });

    testWidgets('page variant centers the loader', (tester) async {
      await pump(tester, const AppLoading.page());
      await tester.pump();
      expect(
        find.ancestor(
          of: find.byType(CircularProgressIndicator),
          matching: find.byType(Center),
        ),
        findsWidgets,
      );
    });
  });

  group('AppEmptyState', () {
    testWidgets('renders hero icon, bold navy caption, muted description',
        (tester) async {
      await pump(
        tester,
        const AppEmptyState(
          icon: Icons.photo_library_outlined,
          message: 'خالی است',
          description: 'توضیح',
        ),
      );
      final icon = tester.widget<Icon>(find.byIcon(Icons.photo_library_outlined));
      expect(icon.size, AppIconSize.hero);
      expect(icon.color, AppColors.icon);
      final caption = tester.widget<Text>(find.text('خالی است'));
      expect(caption.style?.color, AppColors.ink);
      expect(caption.style?.fontWeight, FontWeight.bold);
      final description = tester.widget<Text>(find.text('توضیح'));
      expect(description.style?.color, AppColors.muted);
      expect(find.byType(FilledButton), findsNothing);
    });

    testWidgets('renders a compact action when provided', (tester) async {
      var tapped = false;
      await pump(
        tester,
        AppEmptyState(
          message: 'خالی است',
          actionLabel: 'افزودن',
          onAction: () => tapped = true,
        ),
      );
      await tester.tap(find.widgetWithText(FilledButton, 'افزودن'));
      expect(tapped, isTrue);
    });

    testWidgets('custom icon widget slot replaces the default icon',
        (tester) async {
      await pump(
        tester,
        const AppEmptyState(
          iconWidget: FlutterLogo(size: 90),
          message: 'خالی است',
        ),
      );
      expect(find.byType(FlutterLogo), findsOneWidget);
      expect(find.byType(Icon), findsNothing);
    });
  });

  group('AppErrorState', () {
    testWidgets('renders muted icon and message, retry fires callback',
        (tester) async {
      var retried = false;
      await pump(
        tester,
        AppErrorState(message: 'خطا رخ داد', onRetry: () => retried = true),
      );
      final icon = tester.widget<Icon>(find.byIcon(Icons.warning_rounded));
      expect(icon.size, 50);
      expect(icon.color, AppColors.muted);
      final message = tester.widget<Text>(find.text('خطا رخ داد'));
      expect(message.style?.color, AppColors.muted);

      await tester.tap(find.byKey(const Key('app-error-retry')));
      expect(retried, isTrue);
      expect(find.text(AppStrings.retry), findsOneWidget);
    });

    testWidgets('omits the retry button without a callback', (tester) async {
      await pump(tester, const AppErrorState(message: 'خطا'));
      expect(find.byType(OutlinedButton), findsNothing);
      expect(find.text(AppStrings.retry), findsNothing);
    });
  });
}
