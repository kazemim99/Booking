import 'package:asan_rezerve_provider_app/core/utils/persian_digits.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

/// Prices in this app are six and seven digits (۲۵۰,۰۰۰ … ۸,۰۰۰,۰۰۰). Without
/// grouping they are hard to read back and easy to mistype by a factor of ten.
void main() {
  const formatter = ThousandsSeparatorInputFormatter();

  TextEditingValue type(String text, {String previous = ''}) => formatter.formatEditUpdate(
        TextEditingValue(text: previous),
        TextEditingValue(
          text: text,
          selection: TextSelection.collapsed(offset: text.length),
        ),
      );

  group('typing', () {
    test('groups digits in threes as they are typed', () {
      expect(type('250000').text, '250,000');
      expect(type('8000000').text, '8,000,000');
    });

    test('leaves short numbers alone', () {
      expect(type('').text, '');
      expect(type('999').text, '999');
    });

    test('accepts Persian digits from the phone keyboard', () {
      expect(type('۲۵۰۰۰۰').text, '250,000');
    });

    test('ignores anything that is not a digit', () {
      expect(type('250,000').text, '250,000');
      expect(type('25a0 00.0').text, '250,000');
    });

    test('keeps the caret at the end of what was typed', () {
      final value = type('250000');
      expect(value.selection.baseOffset, value.text.length);
    });
  });

  group('reading the value back', () {
    test('parses a grouped string to a number', () {
      expect(PriceText.parse('250,000'), 250000);
      expect(PriceText.parse('۸,۰۰۰,۰۰۰'), 8000000);
      expect(PriceText.parse(''), isNull);
      expect(PriceText.parse('—'), isNull);
    });

    test('formats a number for display', () {
      expect(PriceText.format(250000), '250,000');
      expect(PriceText.format(0), '0');
    });
  });
}
