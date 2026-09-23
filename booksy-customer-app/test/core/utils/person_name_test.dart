import 'package:booksy_customer_app/core/utils/person_name.dart';
import 'package:flutter_test/flutter_test.dart';

/// Who counts as named (QA walkthrough 2026-09-22: a customer's profile read «ارائه‌دهنده 9384444636», and
/// nothing ever asked them for a real name).
void main() {
  test('a real name is first and last together', () {
    expect(realNameOrNull('سارا', 'احمدی'), 'سارا احمدی');
    expect(realNameOrNull(' سارا ', ' احمدی '), 'سارا احمدی');
    expect(isPlaceholderName('سارا', 'احمدی'), isFalse);
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
}
