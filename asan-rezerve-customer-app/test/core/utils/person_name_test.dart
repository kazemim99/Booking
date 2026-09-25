import 'package:asan_rezerve_customer_app/core/utils/person_name.dart';
import 'package:flutter_test/flutter_test.dart';

/// Who counts as named (QA walkthrough 2026-09-22: a customer's profile read «ارائه‌دهنده 9384444636», and
/// nothing ever asked them for a real name).
void main() {
  test('a real name is first and last together', () {
    expect(realNameOrNull('سارا', 'احمدی'), 'سارا احمدی');
    expect(realNameOrNull(' سارا ', ' احمدی '), 'سارا احمدی');
    expect(isPlaceholderName('سارا', 'احمدی'), isFalse);
  });

  test('a full name needs both parts, and neither may be the placeholder', () {
    expect(hasFullName('سارا', 'احمدی'), isTrue);
    expect(hasFullName('سارا', null), isFalse);
    expect(hasFullName('سارا', ''), isFalse);
    expect(hasFullName(null, 'احمدی'), isFalse);
    expect(hasFullName('مشتری', '9384444636'), isFalse);
    expect(hasFullName('سارا', '9384444636'), isFalse);
  });

  test('half a name is still a name', () {
    expect(realNameOrNull('سارا', null), 'سارا');
    expect(realNameOrNull(null, 'احمدی'), 'احمدی');
  });

  test('the OTP placeholder is not a name', () {
    expect(realNameOrNull('مشتری', '9384444636'), isNull);
    expect(realNameOrNull('ارائه‌دهنده', '9384444636'), isNull);
    expect(isPlaceholderName('ارائه‌دهنده', '9384444636'), isTrue);
    expect(isPlaceholderName(null, null), isTrue);
  });

  test('a number is never a surname', () {
    expect(realNameOrNull('سارا', '9384444636'), 'سارا');
  });

  // QA recording 2026-09-23 #8: the API sends a staff member's name as one string (`staffName`), and a member
  // who signed up by OTP is stored as «ارائه‌دهنده 9123135143» — the confirm step showed exactly that.
  group('a name sent whole', () {
    test('a real one is kept as it is', () {
      expect(realFullNameOrNull('مریم احمدی'), 'مریم احمدی');
      expect(realFullNameOrNull('  مریم  '), 'مریم');
      expect(realFullNameOrNull('مریم سادات حسینی'), 'مریم سادات حسینی');
    });

    test('nothing, or only spaces, is no name', () {
      expect(realFullNameOrNull(null), isNull);
      expect(realFullNameOrNull(''), isNull);
      expect(realFullNameOrNull('   '), isNull);
    });

    test('the OTP placeholder is no name, however the word is spaced', () {
      expect(realFullNameOrNull('ارائه‌دهنده 9123135143'), isNull);
      expect(realFullNameOrNull('ارائه دهنده 9123135143'), isNull);
      expect(realFullNameOrNull('مشتری 9384444636'), isNull);
      expect(realFullNameOrNull('مشتری'), isNull);
    });

    test('a phone number where the name should be is no name', () {
      expect(realFullNameOrNull('09123135143'), isNull);
      expect(realFullNameOrNull('+989123135143'), isNull);
      expect(realFullNameOrNull('۰۹۱۲۳۱۳۵۱۴۳'), isNull);
      expect(realFullNameOrNull('0912 313 5143'), isNull);
    });
  });

  // Production QA 2026-09-23: the confirm step read «ارائه‌دهنده 9123135143» — "the number must never be written
  // anywhere". Mirrors PersonName on the server.
  test('a phone in any spelling is never a surname', () {
    expect(realNameOrNull('سارا', '+989123135143'), 'سارا');
    expect(realNameOrNull('سارا', '0912 313 5143'), 'سارا');
    expect(realNameOrNull('سارا', '۰۹۱۲۳۱۳۵۱۴۳'), 'سارا');
  });

  test('a phone, or the whole placeholder, in the first-name field is no name', () {
    expect(realNameOrNull('09123135143', null), isNull);
    expect(realNameOrNull('+98 912 313 5143', ''), isNull);
    expect(realNameOrNull('ارائه‌دهنده 9123135143', null), isNull);
    expect(realNameOrNull('مشتری', '۹۳۸۴۴۴۴۶۳۶'), isNull);
  });

  test('a one-string name that is a placeholder or a phone is no name', () {
    for (final name in [
      'ارائه‌دهنده 9123135143',
      'ارائه دهنده 9123135143',
      'مشتری 9384444636',
      'ارائه‌دهنده',
      '09123135143',
      '+98 912 313 5143',
      '   ',
      null,
    ]) {
      expect(personNameOrNull(name), isNull, reason: '$name');
    }
  });

  test('a real one-string name keeps everything but a phone number', () {
    expect(personNameOrNull('مریم'), 'مریم');
    expect(personNameOrNull(' مریم رضایی '), 'مریم رضایی');
    // Short numbers are part of names people choose; seven digits or more is a phone number.
    expect(personNameOrNull('سالن ۲۴ ساعته'), 'سالن ۲۴ ساعته');
    expect(personNameOrNull('مریم 09123135143'), 'مریم');
  });

  test('the parts of a placeholder are blank, so an edit form never shows the number', () {
    expect(realNameParts('ارائه‌دهنده', '9123135143'), (first: '', last: ''));
    expect(realNameParts('مشتری', '9384444636'), (first: '', last: ''));
    expect(realNameParts('سارا', '9123135143'), (first: 'سارا', last: ''));
    expect(realNameParts(' سارا ', ' احمدی '), (first: 'سارا', last: 'احمدی'));
  });
}
