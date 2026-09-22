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
}
