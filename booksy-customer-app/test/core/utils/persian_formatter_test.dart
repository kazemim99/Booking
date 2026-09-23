import 'package:booksy_customer_app/core/utils/persian_formatter.dart';
import 'package:booksy_customer_app/core/utils/price_formatter.dart';
import 'package:flutter_test/flutter_test.dart';

/// customer-app-ux-review-fixes A.6: numbers are grouped with the Arabic/Persian thousands separator
/// U+066C «٬», never a Latin comma (U+002C) or the Arabic comma (U+060C, a punctuation comma).
///
/// Escapes are spelled out here on purpose: the separator and a comma look alike in most editors,
/// which is how a wrong one would slip past review.
void main() {
  const sep = '٬';

  test('formatNumber groups in threes with U+066C and Persian digits', () {
    expect(PersianFormatter.formatNumber(1234567), '۱$sep۲۳۴$sep۵۶۷');
    expect(PersianFormatter.formatNumber(250000), '۲۵۰$sep۰۰۰');
    expect(PersianFormatter.formatNumber(999), '۹۹۹');
    expect(PersianFormatter.formatNumber(0), '۰');
  });

  test('no Latin comma and no Arabic comma in a formatted number or price', () {
    for (final s in [
      PersianFormatter.formatNumber(1500000),
      PriceFormatter.format(1500000),
      PriceFormatter.formatFrom(80000),
      PriceFormatter.formatRange(100000, 500000),
    ]) {
      expect(s, isNot(contains(',')), reason: s);
      expect(s, isNot(contains('،')), reason: s);
      expect(s, contains(sep), reason: s);
    }
  });

  test('a price is the grouped number followed by تومان', () {
    expect(PriceFormatter.format(1500000), '۱$sep۵۰۰$sep۰۰۰ تومان');
  });
}
