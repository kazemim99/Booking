import 'package:booksy_provider_app/core/utils/person_name.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PersonName.split — one full-name field, backend still needs first + last', () {
    test('two words: first and last', () {
      final n = PersonName.split('علی رضایی')!;
      expect(n.first, 'علی');
      expect(n.last, 'رضایی');
    });

    test('compound first name written with a space stays in the FIRST name', () {
      // Splitting on the first space would wrongly give last = «علی رضایی».
      final n = PersonName.split('محمد علی رضایی')!;
      expect(n.first, 'محمد علی');
      expect(n.last, 'رضایی');
    });

    test('ZWNJ-joined compound surname is NOT split (U+200C is not whitespace)', () {
      const zwnj = '‌'; // ZERO WIDTH NON-JOINER (نیم‌فاصله)
      final n = PersonName.split('علی حسینی$zwnjنژاد')!;
      expect(n.first, 'علی');
      expect(n.last, 'حسینی$zwnjنژاد',
          reason: 'the half-space must keep «حسینی‌نژاد» one surname');
    });

    test('extra spaces and surrounding whitespace are ignored', () {
      final n = PersonName.split('   سارا    احمدی   ')!;
      expect(n.first, 'سارا');
      expect(n.last, 'احمدی');
    });

    test('a single word is rejected — there is no last name to send', () {
      expect(PersonName.split('علی'), isNull);
    });

    test('empty and blank input is rejected', () {
      expect(PersonName.split(''), isNull);
      expect(PersonName.split('    '), isNull);
    });

    test('splitting then joining reproduces the name exactly', () {
      for (final name in ['علی رضایی', 'محمد علی رضایی', 'علی حسینی‌نژاد']) {
        final n = PersonName.split(name)!;
        expect(PersonName.join(n.first, n.last), name,
            reason: 'the app shows first + " " + last, so a round trip must be lossless');
      }
    });
  });

  group('PersonName.join — rebuilding the field from a saved draft', () {
    test('joins with a single space', () {
      expect(PersonName.join('محمد علی', 'رضایی'), 'محمد علی رضایی');
    });

    test('tolerates a missing part without a stray space', () {
      expect(PersonName.join('علی', ''), 'علی');
      expect(PersonName.join('', 'رضایی'), 'رضایی');
      expect(PersonName.join('', ''), '');
    });
  });
}
