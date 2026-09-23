import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/theme/app_colors.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/utils/price_formatter.dart';
import 'package:booksy_customer_app/core/widgets/provider_meta_line.dart';
import 'package:booksy_customer_app/core/widgets/provider_rating.dart';
import 'package:booksy_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/provider_result_card.dart';

Widget _wrap(Widget child, {double textScale = 1.0}) => MaterialApp(
      theme: AppTheme.light,
      builder: (context, appChild) => MediaQuery(
        data: MediaQuery.of(context)
            .copyWith(textScaler: TextScaler.linear(textScale)),
        child: Directionality(
          textDirection: TextDirection.rtl,
          child: appChild!,
        ),
      ),
      home: Scaffold(body: ListView(children: [child])),
    );

final _today = DateTime(2026, 9, 23, 10);

ProviderSummary _provider({
  double rating = 4.7,
  int reviewCount = 23,
  double? distance,
  int startingPrice = 0,
  DateTime? nextFreeDate,
  int freeSlotCount = 0,
}) =>
    ProviderSummary(
      id: '1',
      name: 'سالن زیبایی رز',
      rating: rating,
      reviewCount: reviewCount,
      distance: distance,
      startingPrice: startingPrice,
      isOpen: true,
      nextFreeDate: nextFreeDate,
      freeSlotCount: freeSlotCount,
    );

void main() {
  group('ProviderResultCard', () {
    // customer-app-ux-review-fixes F.2: explore used its own «★ 4.7 (23)» row in
    // Latin digits; it now uses the one meta line every other card uses.
    testWidgets('renders name, rating and review count in Persian digits',
        (tester) async {
      await tester.pumpWidget(
          _wrap(ProviderResultCard(provider: _provider(), now: _today)));

      expect(find.text('سالن زیبایی رز'), findsOneWidget);
      expect(find.byType(ProviderMetaLine), findsOneWidget);
      expect(find.text('۴.۷'), findsOneWidget);
      expect(find.text(AppStrings.reviewCountLabel('۲۳')), findsOneWidget);
      expect(find.textContaining('4.7'), findsNothing);
      expect(find.textContaining('(23)'), findsNothing);
    });

    testWidgets('an unrated salon says «هنوز نظری ندارد», not «★ 0.0 (0)»',
        (tester) async {
      await tester.pumpWidget(_wrap(ProviderResultCard(
        provider: _provider(rating: 0, reviewCount: 0),
        now: _today,
      )));

      expect(find.text(AppStrings.noReviewsYet), findsOneWidget);
      expect(find.byType(ProviderRating), findsNothing);
      expect(find.textContaining('0.0'), findsNothing);
      expect(find.textContaining('۰.۰'), findsNothing);
      expect(find.byIcon(Icons.star), findsNothing);
    });

    testWidgets('draws its stars in the one display colour', (tester) async {
      await tester.pumpWidget(
          _wrap(ProviderResultCard(provider: _provider(), now: _today)));

      final star = tester.widget<Icon>(find.byIcon(Icons.star_rounded));
      // Was AppColors.warning (1.52:1 on white); the one star colour is now AppColors.star (>= 3:1).
      expect(star.color, AppColors.star);
      expect(
        tester.widgetList<Icon>(find.byType(Icon)).where(
              (icon) => icon.color == Colors.amber,
            ),
        isEmpty,
      );
    });

    testWidgets('shows free slots when the availability summary sent them',
        (tester) async {
      await tester.pumpWidget(_wrap(ProviderResultCard(
        provider: _provider(nextFreeDate: _today, freeSlotCount: 5),
        now: _today,
      )));

      expect(
        find.text(ProviderMetaLine.freeSlotsLabel(_today, 5, _today)!),
        findsOneWidget,
      );
    });

    testWidgets('shows the starting price only when it is known',
        (tester) async {
      await tester.pumpWidget(_wrap(ProviderResultCard(
        provider: _provider(startingPrice: 120000),
        now: _today,
      )));
      expect(find.text(PriceFormatter.formatFrom(120000)), findsOneWidget);

      await tester.pumpWidget(
          _wrap(ProviderResultCard(provider: _provider(), now: _today)));
      expect(find.textContaining('تومان'), findsNothing);
    });

    testWidgets('shows distance only when the response provides it',
        (tester) async {
      await tester.pumpWidget(_wrap(ProviderResultCard(
        provider: _provider(distance: 1.5),
        now: _today,
      )));
      expect(find.text(AppStrings.distanceKmLabel('۱.۵')), findsOneWidget);

      await tester.pumpWidget(
          _wrap(ProviderResultCard(provider: _provider(), now: _today)));
      expect(find.textContaining('کیلومتر'), findsNothing);
    });

    testWidgets('fits a 360-wide phone at 1.3x text with every part shown',
        (tester) async {
      tester.view.physicalSize = const Size(360, 640);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.reset);

      await tester.pumpWidget(_wrap(
        ProviderResultCard(
          provider: _provider(
            distance: 12.4,
            startingPrice: 120000,
            nextFreeDate: _today,
            freeSlotCount: 5,
          ),
          now: _today,
        ),
        textScale: 1.3,
      ));

      expect(tester.takeException(), isNull);
    });

    testWidgets('opens the salon on tap', (tester) async {
      final router = GoRouter(
        initialLocation: '/explore',
        routes: [
          GoRoute(
            path: '/explore',
            builder: (_, __) => Scaffold(
              body: ProviderResultCard(provider: _provider(), now: _today),
            ),
          ),
          GoRoute(
            path: '/providers/:id',
            builder: (_, state) =>
                Scaffold(body: Text('detail ${state.pathParameters['id']}')),
          ),
        ],
      );
      await tester.pumpWidget(
          MaterialApp.router(theme: AppTheme.light, routerConfig: router));

      await tester.tap(find.text('سالن زیبایی رز'));
      await tester.pumpAndSettle();

      expect(find.text('detail 1'), findsOneWidget);
    });
  });
}
