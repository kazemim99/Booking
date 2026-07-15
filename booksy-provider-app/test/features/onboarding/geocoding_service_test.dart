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
}
