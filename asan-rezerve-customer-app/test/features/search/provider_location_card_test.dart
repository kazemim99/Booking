import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/widgets/provider_location_card.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Getting there is part of choosing a salon
/// (openspec/changes/customer-app-discovery-pass): the profile offers to hand
/// the point to a navigation app.
///
/// Since the QA recording of 2026-09-23 (#7) the card is only the map and the
/// directions: it sits inside «تماس و موقعیت», whose address row already says
/// where the salon is, so it carries no heading or address of its own.
void main() {
  Future<void> pump(
    WidgetTester tester, {
    double? latitude,
    double? longitude,
    List<String> opened = const [],
  }) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(
            body: ProviderLocationCard(
              businessName: 'سالن نهال',
              latitude: latitude,
              longitude: longitude,
              openUrl: (url) async => opened.add(url),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('a salon with no coordinates shows nothing of the map',
      (tester) async {
    await pump(tester);
    expect(find.byKey(const Key('provider-location-card')), findsNothing);
  });

  // Was 'the card names the place and offers directions', which also expected the address inside the card. The
  // requirement changed (#7): the address belongs to the section's address row, once, and the card under it is
  // only the map and the directions.
  testWidgets('the card is the map and the directions, with no heading of its own',
      (tester) async {
    await pump(tester, latitude: 39.643089, longitude: 47.897802);

    expect(find.byKey(const Key('provider-location-card')), findsOneWidget);
    expect(find.byKey(const Key('provider-location-map')), findsOneWidget);
    expect(find.byKey(const Key('provider-directions')), findsOneWidget);
    expect(find.text('موقعیت روی نقشه'), findsNothing);
  });

  testWidgets('directions offer the apps people here actually use',
      (tester) async {
    final opened = <String>[];
    await pump(
      tester,
      latitude: 39.643089,
      longitude: 47.897802,
      opened: opened,
    );

    await tester.tap(find.byKey(const Key('provider-directions')));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.directionsNeshan), findsOneWidget);
    expect(find.text(AppStrings.directionsBalad), findsOneWidget);
    expect(find.text(AppStrings.directionsGoogleMaps), findsOneWidget);

    await tester.tap(find.text(AppStrings.directionsNeshan));
    await tester.pumpAndSettle();

    expect(opened, hasLength(1));
    expect(opened.single, contains('39.643089'));
    expect(opened.single, contains('47.897802'));
  });
}
