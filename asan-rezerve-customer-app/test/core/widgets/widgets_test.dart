import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/config/theme/app_colors.dart';
import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/utils/price_formatter.dart';
import 'package:asan_rezerve_customer_app/core/widgets/widgets.dart';

import '../../helpers/contrast.dart';

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

    // A.2: the destructive button's fill is the AA red (white text 6.47:1), not the coral
    // (2.92:1), which stays for badges and fills.
    testWidgets('destructive variant is white on the AA error red', (tester) async {
      await tester.pumpWidget(
        _wrap(AppButton.destructive(label: 'لغو نوبت', onPressed: () {})),
      );
      final style = tester.widget<ElevatedButton>(find.byType(ElevatedButton)).style!;
      expect(style.backgroundColor!.resolve({}), AppTheme.light.colorScheme.error);
      expect(style.foregroundColor!.resolve({}), AppTheme.light.colorScheme.onError);
      // Pinning the token alone would also pass with the old coral (#FF6171, 2.9:1 under white); the label must
      // actually be readable on the fill.
      expect(style.backgroundColor!.resolve({}), AppColors.errorText);
      expect(
        contrastRatio(style.foregroundColor!.resolve({})!, style.backgroundColor!.resolve({})!),
        greaterThanOrEqualTo(kAaText),
      );
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
    // The layout checks measure real Persian text: the test font draws every glyph as a
    // 1em square, which would make «پروفایل» twice as wide as it is on a phone.
    setUpAll(() async {
      final vazir = FontLoader('Vazir');
      for (final file in ['Vazir.ttf', 'Vazir-Medium.ttf', 'Vazir-Bold.ttf']) {
        final bytes = File('assets/fonts/vazir/$file').readAsBytesSync();
        vazir.addFont(Future.value(ByteData.sublistView(bytes)));
      }
      await vazir.load();
    });

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

    // customer-app-ux-review-fixes A.4 (decision 2, 2026-09-23): the bar shows a label under every
    // icon. It replaces decision O1 (icons only, name in semantics alone), which this group used to
    // pin — an icon-only bar left sighted customers guessing what «calendar» and «person» mean.
    testWidgets('shows each tab name under its icon', (tester) async {
      await tester.pumpWidget(
        _wrap(const AppBottomBar(items: items, activeIndex: 1)),
      );
      for (final item in items) {
        expect(find.text(item.semanticLabel), findsOneWidget);
        final icon = tester.getRect(find.byIcon(
            item == items[1] ? item.selectedIcon : item.icon));
        final label = tester.getRect(find.text(item.semanticLabel));
        expect(label.top, greaterThanOrEqualTo(icon.bottom),
            reason: '${item.semanticLabel} sits under its icon');
      }
    });

    testWidgets('marks the active tab by weight, filled icon and indicator — not by colour',
        (tester) async {
      await tester.pumpWidget(
        _wrap(const AppBottomBar(items: items, activeIndex: 0)),
      );
      TextStyle styleOf(String label) =>
          tester.renderObject<RenderParagraph>(find.text(label)).text.style!;

      final active = styleOf(AppStrings.tabHome);
      final inactive = styleOf(AppStrings.tabExplore);
      expect(active.color, inactive.color,
          reason: 'both labels are full white so both meet 4.5:1');
      expect(active.fontWeight!.value, greaterThan(inactive.fontWeight!.value));
      expect(find.byKey(const Key('app-bottom-bar-indicator-0')), findsOneWidget);
      expect(find.byKey(const Key('app-bottom-bar-indicator-1')), findsNothing);
    });

    testWidgets('each tab is one semantics node: label, button, selected', (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        _wrap(AppBottomBar(items: items, activeIndex: 2, onTap: (_) {})),
      );

      expect(
        tester.getSemantics(find.bySemanticsLabel(AppStrings.tabAppointments)),
        matchesSemantics(
          label: AppStrings.tabAppointments,
          isButton: true,
          isSelected: true,
          hasSelectedState: true,
          hasTapAction: true,
          hasFocusAction: true,
          isFocusable: true,
        ),
      );
      expect(
        tester.getSemantics(find.bySemanticsLabel(AppStrings.tabHome)),
        matchesSemantics(
          label: AppStrings.tabHome,
          isButton: true,
          hasSelectedState: true,
          hasTapAction: true,
          hasFocusAction: true,
          isFocusable: true,
        ),
      );
      // The visible label must not be announced a second time as its own node.
      for (final item in items) {
        expect(find.bySemanticsLabel(item.semanticLabel), findsOneWidget);
      }
      handle.dispose();
    });

    testWidgets('grows to fit its labels at 1.3x text on a 360x640 screen',
        (tester) async {
      tester.view.physicalSize = const Size(360, 640);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.reset);

      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: const TextScaler.linear(1.3)),
          child: Directionality(textDirection: TextDirection.rtl, child: child!),
        ),
        home: const Scaffold(
          body: SizedBox.expand(),
          bottomNavigationBar: AppBottomBar(items: items, activeIndex: 0),
        ),
      ));

      expect(tester.takeException(), isNull);
      final bar = tester.getRect(find.byKey(const Key('app-bottom-bar')));
      for (final item in items) {
        final label = tester.getRect(find.text(item.semanticLabel));
        expect(bar.contains(label.topLeft) && bar.contains(label.bottomRight - const Offset(0.01, 0.01)),
            isTrue,
            reason: '${item.semanticLabel} is drawn inside the bar');
        final paragraph =
            tester.renderObject<RenderParagraph>(find.text(item.semanticLabel));
        expect(paragraph.didExceedMaxLines, isFalse,
            reason: '${item.semanticLabel} is not cut off');
      }
      expect(bar.bottom, lessThanOrEqualTo(640));
    });

    testWidgets('each tab is at least 48dp tall and wide', (tester) async {
      await tester.pumpWidget(
        _wrap(const AppBottomBar(items: items, activeIndex: 0)),
      );
      for (final item in items) {
        final size = tester.getSize(find.ancestor(
          of: find.text(item.semanticLabel),
          matching: find.byType(InkWell),
        ));
        expect(size.height, greaterThanOrEqualTo(48));
        expect(size.width, greaterThanOrEqualTo(48));
      }
    });
  });

  group('ProviderRating', () {
    // provider-reviews-and-ratings: the count is now the published review count,
    // so it decides. It used to be a constant zero, which is why `rating > 0`
    // alone once had to count as rated — `(4.5, 0)` was true until the count
    // became real. Only where no count travels does the rating decide.
    test('hasRating: a known count decides, the rating only stands in for a missing one', () {
      expect(ProviderRating.hasRating(0, 0), isFalse);
      expect(ProviderRating.hasRating(0, null), isFalse);
      expect(ProviderRating.hasRating(4.5, 0), isFalse,
          reason: 'no published review means no rating, whatever number rode along');
      expect(ProviderRating.hasRating(0, 3), isTrue);
      expect(ProviderRating.hasRating(4.5, null), isTrue);
    });

    test('isUnrated only when the count says so — an unknown count is not "no reviews"', () {
      expect(ProviderRating.isUnrated(0), isTrue);
      expect(ProviderRating.isUnrated(null), isFalse);
      expect(ProviderRating.isUnrated(3), isFalse);
    });

    testWidgets('the "no reviews yet" label is words, never a zero star row',
        (tester) async {
      await tester.pumpWidget(_wrap(const NoReviewsYetLabel()));
      expect(find.text(AppStrings.noReviewsYet), findsOneWidget);
      expect(find.byIcon(Icons.star_rounded), findsNothing);
      expect(find.text('۰.۰'), findsNothing);
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
      // No count at all is unknown, not "no reviews" — that one still says nothing.
      const meta = ProviderMetaLine(rating: 0);
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
          startingPrice: 250000,
          distanceKm: 1.5,
        )),
      );

      expect(find.text('پارس‌آباد'), findsOneWidget);
      expect(find.text(PriceFormatter.formatFrom(250000)), findsOneWidget);
      expect(find.text(AppStrings.distanceKmLabel('۱.۵')), findsOneWidget);
      // Rating absent → no star, and two separators for three parts.
      expect(find.byIcon(Icons.star_rounded), findsNothing);
      expect(find.text('·'), findsNWidgets(2));
    });

    testWidgets('a provider with no published reviews says so instead of a zero',
        (tester) async {
      const meta = ProviderMetaLine(category: 'پارس‌آباد', rating: 0, reviewCount: 0);
      expect(meta.hasContent, isTrue);

      await tester.pumpWidget(_wrap(meta));

      expect(find.byKey(const Key('provider-no-reviews')), findsOneWidget);
      expect(find.text(AppStrings.noReviewsYet), findsOneWidget);
      expect(find.byIcon(Icons.star_rounded), findsNothing);
    });

    testWidgets('a rated provider shows its average and published count',
        (tester) async {
      await tester.pumpWidget(
        _wrap(const ProviderMetaLine(rating: 4.2, reviewCount: 7)),
      );
      expect(find.text('۴.۲'), findsOneWidget);
      expect(find.text(AppStrings.reviewCountLabel('۷')), findsOneWidget);
      expect(find.byKey(const Key('provider-no-reviews')), findsNothing);
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

    testWidgets(
        'on the web, a photo the browser will not hand over by CORS still '
        'shows, as an HTML image (salon-images-load, G7)', (tester) async {
      // Photos are cached "public" and every *.nahalkmi.ir app shares one
      // browser cache: a copy an <img> fetched on the admin or Vue site
      // carries no CORS header, and Flutter's CORS fetch of it fails — the
      // salon showed the placeholder instead of its photo.
      ProviderImage.debugIsWebOverride = true;
      addTearDown(() => ProviderImage.debugIsWebOverride = null);

      await tester.pumpWidget(_wrap(const ProviderImage(
        imageUrl: 'https://back.nahalkmi.ir/uploads/providers/p/a_medium.webp',
        width: 96,
        height: 96,
      )));

      final photo = tester.widget<Image>(find.byType(Image)).image;
      expect(photo, isA<NetworkImage>());
      expect((photo as NetworkImage).webHtmlElementStrategy,
          WebHtmlElementStrategy.fallback);
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
