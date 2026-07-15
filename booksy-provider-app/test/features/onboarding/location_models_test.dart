import 'package:booksy_provider_app/features/onboarding/data/models/location_models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('CityOption.listFromHierarchy (flatten all cities)', () {
    test('flattens every city across provinces and sorts by name', () {
      final hierarchy = [
        {
          'id': 1,
          'name': 'تهران',
          'cities': [
            {'id': 11, 'name': 'ری'},
            {'id': 12, 'name': 'تهران'},
          ],
        },
        {
          'id': 2,
          'name': 'اصفهان',
          'cities': [
            {'id': 21, 'name': 'کاشان'},
          ],
        },
      ];

      final cities = CityOption.listFromHierarchy(hierarchy);

      // Every city from every province appears in one flat list.
      expect(cities.length, 3);
      // Sorted alphabetically by city name.
      expect(cities.map((c) => c.name).toList(), ['تهران', 'ری', 'کاشان']);
      // Province is carried through for derivation + display.
      final kashan = cities.firstWhere((c) => c.name == 'کاشان');
      expect(kashan.provinceName, 'اصفهان');
      expect(kashan.label, 'کاشان (اصفهان)');
    });

    test('skips malformed entries defensively', () {
      final hierarchy = [
        {'id': 1, 'name': 'تهران'}, // no cities key
        {
          'id': 2,
          'name': 'فارس',
          'cities': [
            {'id': 21, 'name': 'شیراز'},
            {'name': 'بدون‌شناسه'}, // missing id
            {'id': 22, 'name': ''}, // empty name
            'not-a-map',
          ],
        },
      ];

      final cities = CityOption.listFromHierarchy(hierarchy);

      expect(cities.length, 1);
      expect(cities.single.name, 'شیراز');
      expect(cities.single.provinceName, 'فارس');
    });

    test('unwraps the live API envelope ({success, statusCode, data: [...]})',
        () {
      // Regression: the monolith wraps the hierarchy array in an envelope.
      // Passing the raw envelope used to yield an empty city list, which made
      // the city search silently match nothing.
      final envelope = {
        'success': true,
        'statusCode': 200,
        'message': 'Request completed successfully',
        'data': [
          {
            'id': 55,
            'name': 'آذربایجان شرقی',
            'provinceCode': 3,
            'cities': [
              {'id': 69, 'name': 'آذرشهر', 'cityCode': 21},
            ],
          },
        ],
      };

      final cities = CityOption.listFromHierarchy(envelope);

      expect(cities.length, 1);
      expect(cities.single.name, 'آذرشهر');
      expect(cities.single.provinceName, 'آذربایجان شرقی');
    });

    test('returns empty list for non-list input', () {
      expect(CityOption.listFromHierarchy(null), isEmpty);
      expect(CityOption.listFromHierarchy(<String, dynamic>{}), isEmpty);
      // An envelope whose data is not a list is also rejected.
      expect(CityOption.listFromHierarchy({'data': 'oops'}), isEmpty);
    });
  });
}
