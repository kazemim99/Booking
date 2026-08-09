import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/provider_result_card.dart';

Widget _wrap(Widget child) => MaterialApp(
      theme: AppTheme.light,
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: Scaffold(body: child),
      ),
    );

ProviderSummary _provider({double? distance}) => ProviderSummary(
      id: '1',
      name: 'سالن زیبایی رز',
      rating: 4.7,
      reviewCount: 23,
      distance: distance,
      startingPrice: 100,
      isOpen: true,
    );

void main() {
  group('ProviderResultCard', () {
    testWidgets('renders name, rating and review count', (tester) async {
      await tester.pumpWidget(_wrap(ProviderResultCard(provider: _provider())));

      expect(find.text('سالن زیبایی رز'), findsOneWidget);
      expect(find.text('4.7'), findsOneWidget);
      expect(find.text('(23)'), findsOneWidget);
    });

    testWidgets('shows distance only when the response provides it',
        (tester) async {
      await tester.pumpWidget(
        _wrap(ProviderResultCard(provider: _provider(distance: 1.5))),
      );
      expect(find.textContaining('کیلومتر'), findsOneWidget);

      await tester.pumpWidget(
        _wrap(ProviderResultCard(provider: _provider())),
      );
      expect(find.textContaining('کیلومتر'), findsNothing);
    });
  });
}
