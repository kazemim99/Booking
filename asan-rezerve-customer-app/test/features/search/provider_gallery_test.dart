import 'package:booksy_customer_app/features/search/presentation/widgets/provider_gallery.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The salon's photos are swiped through at the top of its profile
/// (openspec/changes/customer-app-discovery-pass).
void main() {
  Future<void> pump(WidgetTester tester, List<String> images,
      {String? fallback}) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ProviderGallery(images: images, fallbackImageUrl: fallback),
        ),
      ),
    );
    await tester.pump();
  }

  testWidgets('several photos can be paged through', (tester) async {
    await pump(tester, const ['https://x/1.webp', 'https://x/2.webp']);

    expect(find.byKey(const Key('provider-gallery')), findsOneWidget);
    expect(find.byKey(const Key('provider-gallery-0')), findsOneWidget);
  });

  testWidgets('one photo is just the header, with no dots', (tester) async {
    await pump(tester, const ['https://x/1.webp']);

    expect(find.byKey(const Key('provider-gallery')), findsNothing);
    expect(find.byKey(const Key('provider-hero-image')), findsOneWidget);
  });

  testWidgets('no photos keeps the header, using whatever single picture exists',
      (tester) async {
    await pump(tester, const [], fallback: 'https://x/logo.webp');

    expect(find.byKey(const Key('provider-hero-image')), findsOneWidget);
  });
}
