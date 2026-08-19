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

  group('AppBottomBar', () {
    const items = [
      AppBottomBarItem(
        icon: Icons.home_outlined,
        selectedIcon: Icons.home,
        semanticLabel: AppStrings.tabHome,
      ),
      AppBottomBarItem(
        icon: Icons.search_outlined,
        selectedIcon: Icons.search,
        semanticLabel: AppStrings.tabExplore,
      ),
      AppBottomBarItem(
        icon: Icons.calendar_today_outlined,
        selectedIcon: Icons.calendar_today,
        semanticLabel: AppStrings.tabAppointments,
      ),
      AppBottomBarItem(
        icon: Icons.person_outline,
        selectedIcon: Icons.person,
        semanticLabel: AppStrings.tabProfile,
      ),
    ];

    testWidgets('renders the active tab with its selected icon', (tester) async {
      await tester.pumpWidget(
        _wrap(const AppBottomBar(items: items, activeIndex: 0)),
      );
      // Active tab (home) shows the filled icon; others show their outline.
      expect(find.byIcon(Icons.home), findsOneWidget);
      expect(find.byIcon(Icons.search_outlined), findsOneWidget);
    });

    testWidgets('tapping a destination fires onTap with its index',
        (tester) async {
      int? tapped;
      await tester.pumpWidget(
        _wrap(AppBottomBar(
          items: items,
          activeIndex: 0,
          onTap: (i) => tapped = i,
        )),
      );
      await tester.tap(find.byIcon(Icons.calendar_today_outlined));
      expect(tapped, 2);
    });
  });

  group('PriceBand', () {
    test('is null when there is nothing to derive from', () {
      // The backend publishes no price band, and every seeded provider has a
      // zero starting price — the band must stay absent, never default to "$".
      expect(PriceBand.fromPrices(const []), isNull);
      expect(PriceBand.fromPrices(const [0]), isNull);
      expect(PriceBand.fromPrices(const [0, 0, 0]), isNull);
    });

    test('derives a band from the median of the priced services', () {
      expect(PriceBand.fromPrices(const [150000]), PriceBand.low);
      expect(PriceBand.fromPrices(const [250000]), PriceBand.mid);
      expect(PriceBand.fromPrices(const [900000]), PriceBand.high);
      // One premium package must not drag a cheap salon into the top band.
      expect(
        PriceBand.fromPrices(const [80000, 100000, 2000000]),
        PriceBand.low,
      );
      // Zero-priced services are ignored rather than pulling the median down.
      expect(PriceBand.fromPrices(const [0, 700000]), PriceBand.high);
      // Even-length lists average the two middle prices (250k + 850k → 550k).
      expect(PriceBand.fromPrices(const [250000, 850000]), PriceBand.mid);
    });

    testWidgets('renders its glyph left-to-right inside RTL text',
        (tester) async {
      await tester.pumpWidget(
        _wrap(const PriceBandLabel(band: PriceBand.mid)),
      );
      expect(find.text(AppStrings.priceBandMid), findsOneWidget);
      final text = tester.widget<Text>(find.text(AppStrings.priceBandMid));
      expect(text.textDirection, TextDirection.ltr);
    });
  });

  group('ProviderRating', () {
    test('hasRating gates the zero-data case', () {
      expect(ProviderRating.hasRating(0, 0), isFalse);
      expect(ProviderRating.hasRating(0, null), isFalse);
      expect(ProviderRating.hasRating(4.5, 0), isTrue);
      expect(ProviderRating.hasRating(0, 3), isTrue);
    });

    testWidgets('shows the rating and review count in Persian digits',
        (tester) async {
      await tester.pumpWidget(
        _wrap(const ProviderRating(rating: 4.7, reviewCount: 23)),
      );
      expect(find.text('۴.۷'), findsOneWidget);
      expect(find.text(AppStrings.reviewCountLabel('۲۳')), findsOneWidget);
    });

    testWidgets('omits the review count when there are no reviews',
        (tester) async {
      await tester.pumpWidget(
        _wrap(const ProviderRating(rating: 4.7, reviewCount: 0)),
      );
      expect(find.textContaining('نظر'), findsNothing);
    });
  });

  group('ProviderMetaLine', () {
    testWidgets('collapses to nothing when no part has data', (tester) async {
      const meta = ProviderMetaLine(rating: 0, reviewCount: 0);
      expect(meta.hasContent, isFalse);

      await tester.pumpWidget(_wrap(meta));
      expect(find.text('·'), findsNothing);
      expect(tester.getSize(find.byType(ProviderMetaLine)), Size.zero);
    });

    testWidgets('joins only the parts that have data', (tester) async {
      await tester.pumpWidget(
        _wrap(const ProviderMetaLine(
          category: 'پارس‌آباد',
          rating: 0,
          reviewCount: 0,
          priceBand: PriceBand.mid,
          distanceKm: 1.5,
        )),
      );

      expect(find.text('پارس‌آباد'), findsOneWidget);
      expect(find.text(AppStrings.priceBandMid), findsOneWidget);
      expect(find.text(AppStrings.distanceKmLabel('۱.۵')), findsOneWidget);
      // Rating absent → no star, and two separators for three parts.
      expect(find.byIcon(Icons.star_rounded), findsNothing);
      expect(find.text('·'), findsNWidgets(2));
    });
  });

  group('ProviderImage', () {
    testWidgets('falls back to the storefront glyph without a URL',
        (tester) async {
      await tester.pumpWidget(
        _wrap(const ProviderImage(imageUrl: null, width: 96, height: 96)),
      );
      expect(find.byIcon(Icons.storefront_outlined), findsOneWidget);
    });

    testWidgets('treats an empty URL as no image', (tester) async {
      await tester.pumpWidget(
        _wrap(const ProviderImage(imageUrl: '', width: 96, height: 96)),
      );
      expect(find.byIcon(Icons.storefront_outlined), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  });

  group('AppCircleIconButton', () {
    testWidgets('is tappable, labelled, and meets the touch target',
        (tester) async {
      var tapped = false;
      await tester.pumpWidget(
        _wrap(AppCircleIconButton(
          icon: Icons.travel_explore,
          semanticLabel: AppStrings.mapSearch,
          onPressed: () => tapped = true,
        )),
      );

      final size = tester.getSize(find.byType(AppCircleIconButton));
      expect(size.height, greaterThanOrEqualTo(48));
      expect(size.width, greaterThanOrEqualTo(48));

      await tester.tap(find.byIcon(Icons.travel_explore));
      expect(tapped, isTrue);
    });
  });
}
