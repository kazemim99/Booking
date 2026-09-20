import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/provider_location_card.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Getting there is part of choosing a salon
/// (openspec/changes/customer-app-discovery-pass): the profile shows the street
/// address, not only the city, and offers to hand the point to a navigation app.
void main() {
  Future<void> pump(
    WidgetTester tester, {
    double? latitude,
    double? longitude,
    String? address,
    List<String> opened = const [],
  }) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(
            body: ProviderLocationCard(
              businessName: 'سالن نهال',
              address: address,
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
    await pump(tester, address: 'شهرک پناهی، کوچه بلور ۳');
    expect(find.byKey(const Key('provider-location-card')), findsNothing);
  });

  testWidgets('the card names the place and offers directions', (tester) async {
    await pump(
      tester,
      address: 'شهرک پناهی، کوچه بلور ۳',
      latitude: 39.643089,
      longitude: 47.897802,
    );

    expect(find.byKey(const Key('provider-location-card')), findsOneWidget);
    expect(find.text('شهرک پناهی، کوچه بلور ۳'), findsOneWidget);
    expect(find.byKey(const Key('provider-directions')), findsOneWidget);
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
