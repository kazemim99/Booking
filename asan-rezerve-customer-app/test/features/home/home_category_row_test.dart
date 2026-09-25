import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/features/home/domain/entities/category.dart';
import 'package:asan_rezerve_customer_app/features/home/presentation/widgets/home_category_row.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A tile for a category no salon offers leads to an empty result page, so the
/// row shows only what can actually be booked
/// (openspec/changes/customer-app-discovery-pass).
void main() {
  Future<void> pump(WidgetTester tester, List<Category> available) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: HomeCategoryRow(available: available)),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  const barbershop = Category(
    id: 'barbershop',
    name: 'آرایشگاه مردانه',
    providerCount: 1,
  );
  const emptySpa = Category(id: 'spa', name: 'اسپا', providerCount: 0);

  testWidgets('only categories with salons get a tile', (tester) async {
    await pump(tester, const [barbershop, emptySpa]);

    expect(find.byKey(const Key('home-category-Barbershop')), findsOneWidget);
    expect(find.byKey(const Key('home-category-Spa')), findsNothing);
    expect(find.byKey(const Key('home-category-HairSalon')), findsNothing);
  });

  testWidgets('the more tile stays, so the rest is one tap away',
      (tester) async {
    await pump(tester, const [barbershop]);

    expect(find.byKey(const Key('home-category-more')), findsOneWidget);
    expect(find.text(AppStrings.categoryBarbershop), findsOneWidget);
  });

  testWidgets('with nothing known yet, the full row is shown rather than none',
      (tester) async {
    // The categories call can fail or still be in flight; an empty row would
    // read as "this app has no categories".
    await pump(tester, const []);

    expect(find.byKey(const Key('home-category-Barbershop')), findsOneWidget);
    expect(find.byKey(const Key('home-category-HairSalon')), findsOneWidget);
  });
}
