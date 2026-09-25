import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/service_catalog.dart';
import 'package:flutter_test/flutter_test.dart';

/// The catalogue is seed content the provider edits, so what matters is that it
/// is well formed and reachable from the category the provider picked.
void main() {
  final everyGroup = [...ServiceCatalog.menSalon, ...ServiceCatalog.womenSalon];

  test('each category the app offers has a catalogue', () {
    for (final category in BusinessCategory.all) {
      expect(ServiceCatalog.forCategory(category.id), isNotEmpty,
          reason: '${category.label} has no suggested services');
    }
    // An unknown category degrades to "add them by hand", never to an error.
    expect(ServiceCatalog.forCategory('something-else'), isEmpty);
    expect(ServiceCatalog.forCategory(null), isEmpty);
  });

  test('a barbershop gets the men\'s list and a salon the women\'s', () {
    final men = ServiceCatalog.forCategory('barbershop')
        .expand((g) => g.services)
        .map((s) => s.name);
    final women = ServiceCatalog.forCategory('hair_salon')
        .expand((g) => g.services)
        .map((s) => s.name);

    expect(men, contains('اصلاح ریش و سبیل'));
    expect(women, contains('میکاپ عروس'));
    expect(men, isNot(contains('میکاپ عروس')));
  });

  test('every preset is usable as it stands', () {
    for (final group in everyGroup) {
      expect(group.title.trim(), isNotEmpty);
      expect(group.services, isNotEmpty, reason: '${group.title} is empty');
      for (final service in group.services) {
        expect(service.name.trim(), isNotEmpty);
        expect(service.price, greaterThan(0), reason: service.name);
        expect(service.minutes, greaterThan(0), reason: service.name);
        // Chair time: anything beyond a working half-day is a typo.
        expect(service.minutes, lessThanOrEqualTo(240), reason: service.name);
      }
    }
  });

  test('no service is listed twice within a category', () {
    for (final catalogue in [ServiceCatalog.menSalon, ServiceCatalog.womenSalon]) {
      final names = catalogue.expand((g) => g.services).map((s) => s.name).toList();
      expect(names.toSet().length, names.length,
          reason: 'duplicate service names would produce duplicate rows');
    }
  });

  test('per-unit prices say so, so the number is not read as a visit price', () {
    final perUnit = everyGroup
        .expand((g) => g.services)
        .where((s) => s.unitNote != null)
        .map((s) => s.name);

    expect(perUnit, containsAll(['اکستنشن مو', 'طراحی ناخن']));
  });
}
