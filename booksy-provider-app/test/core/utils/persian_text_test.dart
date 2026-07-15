import 'package:booksy_provider_app/core/utils/persian_text.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PersianText.normalize', () {
    test('unifies Arabic and Persian kaf', () {
      // Arabic kaf (U+0643) vs Persian kaf (U+06A9).
      expect(PersianText.normalize('كاشان'), PersianText.normalize('کاشان'));
    });

    test('unifies Arabic yeh / alef maksura and Persian yeh', () {
      // Arabic yeh (U+064A), alef maksura (U+0649), Persian yeh (U+06CC).
      expect(PersianText.normalize('اصفهاني'), PersianText.normalize('اصفهانی'));
      expect(PersianText.normalize('موسى'), PersianText.normalize('موسی'));
    });

    test('treats spaced / ZWNJ / joined compound names as equal', () {
      // `علی آباد` (space), `علی‌آباد` (ZWNJ) and `علیآباد` (joined) all match.
      final joined = PersianText.normalize('علیآباد');
      expect(PersianText.normalize('علی‌آباد'), joined);
      expect(PersianText.normalize('علی آباد'), joined);
      expect(PersianText.normalize('  تهران  '), 'تهران');
    });
  });

  group('PersianText.contains (search matching)', () {
    test('matches across kaf variants — the mobile-keyboard bug', () {
      // City stored with Arabic kaf; user types Persian kaf.
      expect(PersianText.contains('كاشان (اصفهان)', 'کاشان'), isTrue);
      // And the reverse.
      expect(PersianText.contains('کرمان', 'كرمان'), isTrue);
    });

    test('still matches plain substrings and rejects non-matches', () {
      expect(PersianText.contains('تهران (تهران)', 'تهران'), isTrue);
      expect(PersianText.contains('شیراز (فارس)', 'مشهد'), isFalse);
    });
  });
}
