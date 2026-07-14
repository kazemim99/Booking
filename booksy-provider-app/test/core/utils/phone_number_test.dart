import 'package:booksy_provider_app/core/utils/phone_number.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PhoneNumber (canonical 09121234567)', () {
    test('accepts a valid Iranian mobile', () {
      expect(PhoneNumber.isValid('09121234567'), isTrue);
      expect(PhoneNumber.parse('09121234567').value, '09121234567');
    });

    test('normalizes Persian digits', () {
      expect(PhoneNumber.tryParse('۰۹۱۲۱۲۳۴۵۶۷')?.value, '09121234567');
    });

    test('normalizes +98 / 0098 / 98 / 9xxxxxxxxx variants to canonical', () {
      expect(PhoneNumber.tryParse('+989121234567')?.value, '09121234567');
      expect(PhoneNumber.tryParse('00989121234567')?.value, '09121234567');
      expect(PhoneNumber.tryParse('989121234567')?.value, '09121234567');
      expect(PhoneNumber.tryParse('9121234567')?.value, '09121234567');
    });

    test('strips spaces and dashes', () {
      expect(PhoneNumber.tryParse('0912-123 4567')?.value, '09121234567');
    });

    test('rejects invalid numbers', () {
      expect(PhoneNumber.isValid(''), isFalse);
      expect(PhoneNumber.isValid('0812123456'), isFalse); // wrong prefix
      expect(PhoneNumber.isValid('091212345'), isFalse); // too short
      expect(PhoneNumber.isValid('091212345678'), isFalse); // too long
      expect(PhoneNumber.isValid('abcd'), isFalse);
    });

    test('tryParse returns null on invalid; parse throws', () {
      expect(PhoneNumber.tryParse('bad'), isNull);
      expect(() => PhoneNumber.parse('bad'), throwsFormatException);
    });

    test('masked hides the middle digits', () {
      expect(PhoneNumber.parse('09121234567').masked, '0912***4567');
    });

    test('value equality', () {
      expect(PhoneNumber.parse('09121234567'), PhoneNumber.parse('09121234567'));
    });
  });
}
