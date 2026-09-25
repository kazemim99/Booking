import 'dart:io';

import 'package:booksy_customer_app/core/utils/price_formatter.dart';
import 'package:flutter_test/flutter_test.dart';

/// Money in this app is Toman, written in Persian digits and grouped in threes
/// (openspec/changes/customer-app-discovery-pass). Screens used to interpolate the raw
/// `currency` field from the API instead, which printed «USD ۱۵۰۰۰۰۰» — the amount was
/// Toman all along and the code was simply wrong.
void main() {
  test('a price reads as Toman, grouped in threes', () {
    expect(PriceFormatter.format(1500000), '۱٬۵۰۰٬۰۰۰ تومان');
  });

  test('no screen prints the API currency code next to a price', () {
    final offenders = <String>[];
    for (final file in Directory('lib')
        .listSync(recursive: true)
        .whereType<File>()
        .where((f) => f.path.endsWith('.dart'))) {
      final source = file.readAsStringSync();
      for (final line in source.split('\n')) {
        final printsCurrency = line.contains(r'${service.currency}') ||
            line.contains(r'${state.currency}') ||
            line.contains(r'${booking.currency}') ||
            line.contains("'USD'") ||
            line.contains('"USD"');
        if (printsCurrency) offenders.add('${file.path}: ${line.trim()}');
      }
    }
    expect(offenders, isEmpty,
        reason: 'prices belong to PriceFormatter, which writes them in Toman');
  });
}
