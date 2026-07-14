import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/widgets/widgets.dart';

Widget _wrap(Widget child, {double textScale = 1.0}) {
  return MaterialApp(
    theme: AppTheme.light,
    builder: (context, appChild) => MediaQuery(
      data: MediaQuery.of(context)
          .copyWith(textScaler: TextScaler.linear(textScale)),
      child: Directionality(
        textDirection: TextDirection.rtl,
        child: appChild!,
      ),
    ),
    home: Scaffold(body: Center(child: child)),
  );
}

void main() {
  group('AppButton', () {
    testWidgets('fires onPressed when tapped', (tester) async {
      var pressed = false;
      await tester.pumpWidget(
        _wrap(AppButton(label: 'ثبت', onPressed: () => pressed = true)),
      );
      await tester.tap(find.byType(AppButton));
      expect(pressed, isTrue);
    });

    testWidgets('loading state shows spinner and blocks taps', (tester) async {
      var pressed = false;
      await tester.pumpWidget(
        _wrap(
          AppButton(
            label: 'ثبت',
            loading: true,
            onPressed: () => pressed = true,
          ),
        ),
      );
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('ثبت'), findsNothing);
      await tester.tap(find.byType(AppButton), warnIfMissed: false);
      await tester.pump();
      expect(pressed, isFalse);
    });

    testWidgets('meets minimum 48dp touch target', (tester) async {
      await tester.pumpWidget(
        _wrap(AppButton(label: 'ثبت', onPressed: () {}, expanded: false)),
      );
      final size = tester.getSize(find.byType(ElevatedButton));
      expect(size.height, greaterThanOrEqualTo(48));
      expect(size.width, greaterThanOrEqualTo(48));
    });

    testWidgets('renders without overflow at 1.3x text scale', (tester) async {
      await tester.pumpWidget(
        _wrap(
          AppButton(label: 'ارسال کد تایید', onPressed: () {}),
          textScale: 1.3,
        ),
      );
      expect(tester.takeException(), isNull);
    });
  });

  group('StateSwitcher', () {
    Widget build(ViewStatus status, {VoidCallback? onRetry}) {
      return _wrap(
        StateSwitcher(
          status: status,
          skeleton: SkeletonLoader.list(items: 2),
          contentBuilder: (_) => const Text('محتوا'),
          empty: const EmptyState(
            icon: Icons.inbox_outlined,
            title: 'خالی است',
          ),
          errorMessage: 'خطا رخ داد',
          onRetry: onRetry ?? () {},
        ),
      );
    }

    testWidgets('loading renders skeleton', (tester) async {
      await tester.pumpWidget(build(ViewStatus.loading));
      expect(find.byType(SkeletonLoader), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('content renders builder output', (tester) async {
      await tester.pumpWidget(build(ViewStatus.content));
      expect(find.text('محتوا'), findsOneWidget);
    });

    testWidgets('empty renders provided empty state', (tester) async {
      await tester.pumpWidget(build(ViewStatus.empty));
      expect(find.text('خالی است'), findsOneWidget);
    });

    testWidgets('error renders message and retry re-invokes callback',
        (tester) async {
      var retried = false;
      await tester.pumpWidget(
        build(ViewStatus.error, onRetry: () => retried = true),
      );
      expect(find.text('خطا رخ داد'), findsOneWidget);
      await tester.tap(find.text(AppStrings.retry));
      expect(retried, isTrue);
    });
  });

  group('StatusBadge', () {
    testWidgets('conveys status with text label, not color alone',
        (tester) async {
      await tester.pumpWidget(
        _wrap(const StatusBadge(status: BookingStatus.cancelled)),
      );
      expect(find.text(AppStrings.statusCancelled), findsOneWidget);
      expect(find.byIcon(Icons.cancel_outlined), findsOneWidget);
    });

    test('parses API status strings', () {
      expect(StatusBadge.tryParse('Confirmed'), BookingStatus.confirmed);
      expect(StatusBadge.tryParse('CANCELLED'), BookingStatus.cancelled);
      expect(StatusBadge.tryParse('no_show'), BookingStatus.noShow);
      expect(StatusBadge.tryParse('bogus'), isNull);
    });
  });

  group('SkeletonLoader', () {
    testWidgets('renders static blocks when animations are disabled',
        (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          home: MediaQuery(
            data: const MediaQueryData(disableAnimations: true),
            child: Scaffold(body: SkeletonLoader.list(items: 1)),
          ),
        ),
      );
      // Shimmer must not run under reduced motion.
      expect(find.byType(SkeletonLoader), findsOneWidget);
      await tester.pump(const Duration(seconds: 1));
      expect(tester.hasRunningAnimations, isFalse);
    });
  });

  group('EmptyState', () {
    testWidgets('shows CTA when provided and fires callback', (tester) async {
      var tapped = false;
      await tester.pumpWidget(
        _wrap(
          EmptyState(
            icon: Icons.event_available_outlined,
            title: 'نوبتی ندارید',
            ctaLabel: 'یافتن سالن',
            onCta: () => tapped = true,
          ),
        ),
      );
      await tester.tap(find.text('یافتن سالن'));
      expect(tapped, isTrue);
    });
  });
}
