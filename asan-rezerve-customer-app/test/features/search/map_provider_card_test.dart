import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:asan_rezerve_customer_app/config/theme/app_colors.dart';
import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/widgets/forward_chevron.dart';
import 'package:asan_rezerve_customer_app/core/widgets/provider_meta_line.dart';
import 'package:asan_rezerve_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/widgets/map_provider_card.dart';

/// customer-app-ux-review-fixes F.3: the map card is the salon — tapping it
/// opens the profile, like every other provider card — so the full-width
/// «مشاهده پروفایل» button the QA pass removed from the nearby card goes here
/// too, and the card says when the salon is next free.

final _today = DateTime(2026, 9, 23, 10);

ProviderSummary _provider({
  double rating = 4.8,
  int reviewCount = 12,
  DateTime? nextFreeDate,
  int freeSlotCount = 0,
  String? addressLine,
}) =>
    ProviderSummary(
      id: 'm1',
      name: 'سالن آرایش و زیبایی مغان',
      rating: rating,
      reviewCount: reviewCount,
      distance: 12.7,
      startingPrice: 0,
      isOpen: true,
      latitude: 39.6,
      longitude: 47.9,
      addressLine: addressLine,
      nextFreeDate: nextFreeDate,
      freeSlotCount: freeSlotCount,
    );

Widget _app(Widget card, {double textScale = 1.0}) {
  final router = GoRouter(
    initialLocation: '/map',
    routes: [
      GoRoute(
        path: '/map',
        builder: (_, __) => Scaffold(
          body: Align(
            alignment: Alignment.bottomCenter,
            child: SizedBox(width: 300, child: card),
          ),
        ),
      ),
      GoRoute(
        path: '/providers/:id',
        builder: (_, state) =>
            Scaffold(body: Text('provider-detail-${state.pathParameters['id']}')),
      ),
    ],
  );
  return MaterialApp.router(
    theme: AppTheme.light,
    routerConfig: router,
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(context)
          .copyWith(textScaler: TextScaler.linear(textScale)),
      child: Directionality(textDirection: TextDirection.rtl, child: child!),
    ),
  );
}

void main() {
  testWidgets('tapping the card opens the salon profile', (tester) async {
    await tester.pumpWidget(_app(MapProviderCard(provider: _provider(), now: _today)));

    await tester.tap(find.text('سالن آرایش و زیبایی مغان'));
    await tester.pumpAndSettle();

    expect(find.text('provider-detail-m1'), findsOneWidget);
  });

  testWidgets('ends in a forward chevron and has no «مشاهده پروفایل» button',
      (tester) async {
    await tester.pumpWidget(_app(MapProviderCard(provider: _provider(), now: _today)));

    expect(find.byType(ForwardChevron), findsOneWidget);
    expect(find.text(AppStrings.viewProfile), findsNothing);
    expect(find.byType(OutlinedButton), findsNothing);
  });

  testWidgets('is announced as a button named after the salon', (tester) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
        _app(MapProviderCard(provider: _provider(), selected: true, now: _today)));

    final node = tester.getSemantics(find.byType(MapProviderCard));
    expect(node, isSemantics(isButton: true, isSelected: true));
    // Named first and once, then what the meta line says — never the «·».
    expect(node.label, startsWith('سالن آرایش و زیبایی مغان'));
    expect('سالن آرایش و زیبایی مغان'.allMatches(node.label), hasLength(1));
    expect(node.label, isNot(contains('·')));
    semantics.dispose();
  });

  testWidgets('shows free times when they are known', (tester) async {
    await tester.pumpWidget(_app(MapProviderCard(
      provider: _provider(nextFreeDate: _today, freeSlotCount: 4),
      now: _today,
    )));

    expect(
      find.text(ProviderMetaLine.freeSlotsLabel(_today, 4, _today)!),
      findsOneWidget,
    );
  });

  testWidgets('draws its star in the one display colour', (tester) async {
    await tester.pumpWidget(_app(MapProviderCard(provider: _provider(), now: _today)));

    final star = tester.widget<Icon>(find.byIcon(Icons.star_rounded));
    // Was AppColors.warning (1.52:1 on white); the one star colour is now AppColors.star (>= 3:1).
    expect(star.color, AppColors.star);
  });

  testWidgets('fits within the carousel band height at 1.3x text',
      (tester) async {
    await tester.pumpWidget(_app(
      MapProviderCard(
        provider: _provider(
          nextFreeDate: _today,
          freeSlotCount: 4,
          addressLine: 'پارس‌آباد، خیابان امام',
        ),
        now: _today,
      ),
      textScale: 1.3,
    ));

    expect(tester.takeException(), isNull);
    expect(
      tester.getSize(find.byType(MapProviderCard)).height,
      lessThanOrEqualTo(MapProviderCard.bandHeight(const TextScaler.linear(1.3))),
    );
  });
}
