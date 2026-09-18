import 'package:booksy_provider_app/features/onboarding/data/datasources/geocoding_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('GeocodingService.shortenAddress', () {
    test('drops the administrative tail, postcode and country', () {
      const displayName =
          'خانه سینما ساختمان شماره ۲, 28, وصال شیرازی, دانشگاه تهران, ناحیه ۲, '
          'منطقه ۶ شهر تهران, شهرداری منطقه شش ناحیه سه, تهران, بخش مرکزی تهران, '
          'شهرستان تهران, استان تهران, 14168-54528, ایران';

      final short = GeocodingService.shortenAddress(
        displayName,
        city: 'تهران',
        state: 'استان تهران',
      );

      expect(
        short,
        'خانه سینما ساختمان شماره ۲، 28، وصال شیرازی، دانشگاه تهران، شهرداری منطقه شش ناحیه سه',
      );
      // The noisy administrative parts are gone.
      expect(short.contains('استان'), isFalse);
      expect(short.contains('شهرستان'), isFalse);
      expect(short.contains('ایران'), isFalse);
      expect(short.contains('14168'), isFalse);
    });

    test('keeps a short address unchanged (minus the country)', () {
      final short = GeocodingService.shortenAddress(
        'پلاک ۱۲, خیابان ولیعصر, ایران',
        city: '',
        state: '',
      );
      expect(short, 'پلاک ۱۲، خیابان ولیعصر');
    });

    test('caps the number of kept parts', () {
      final short = GeocodingService.shortenAddress(
        'الف, ب, پ, ت, ث, ج, چ, ح',
        maxParts: 3,
      );
      expect(short, 'الف، ب، پ');
    });
  });

  group(
    'GeocodingService.formatAddress (Iranian order: area -> street -> number)',
    () {
      test("the user's reported pin: neighbourhood first, then the alley", () {
        // Nominatim's structured `address` for a pin in Parsabad (2026-09-19).
        const addr = {
          'road': 'کوچه ۵ سهند',
          'neighbourhood': 'محله طالقانی',
          'town': 'شهر پارس آباد',
          'district': 'بخش مرکزی',
          'county': 'شهرستان پارس آباد',
          'state': 'استان اردبیل',
          'postcode': '56919-44471',
          'country': 'ایران',
        };

        expect(
          GeocodingService.formatAddress(addr),
          'محله طالقانی، کوچه ۵ سهند',
        );
      });

      test('adds the plate number and the place name after the street', () {
        const addr = {
          'amenity': 'خانه سینما',
          'house_number': '28',
          'road': 'خیابان وصال شیرازی',
          'suburb': 'دانشگاه تهران',
          'city': 'تهران',
        };

        expect(
          GeocodingService.formatAddress(addr),
          'دانشگاه تهران، خیابان وصال شیرازی، پلاک 28، خانه سینما',
        );
      });

      test('a street alone is still an address', () {
        expect(
          GeocodingService.formatAddress(const {'road': 'خیابان ولیعصر'}),
          'خیابان ولیعصر',
        );
      });

      test('never repeats the city, which the city field already holds', () {
        const addr = {
          'neighbourhood': 'شهر پارس آباد',
          'road': 'کوچه ۲',
          'town': 'شهر پارس آباد',
        };
        expect(GeocodingService.formatAddress(addr), 'کوچه ۲');
      });

      test(
        'without structured parts, falls back to display_name, general to specific',
        () {
          final formatted = GeocodingService.formatAddress(
            const {},
            displayName: 'پلاک ۱۲, خیابان ولیعصر, ایران',
          );
          expect(formatted, 'خیابان ولیعصر، پلاک ۱۲');
        },
      );
    },
  );
}
