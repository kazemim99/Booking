import 'package:booksy_customer_app/core/widgets/provider_meta_line.dart';
import 'package:flutter_test/flutter_test.dart';

/// "How soon can I book here?" on a card
/// (openspec/changes/customer-app-discovery-pass).
void main() {
  final today = DateTime(2026, 9, 20);

  test('today and tomorrow are named, not dated', () {
    expect(ProviderMetaLine.freeSlotsLabel(DateTime(2026, 9, 20), 5, today),
        'امروز ۵ وقت خالی');
    expect(ProviderMetaLine.freeSlotsLabel(DateTime(2026, 9, 21), 8, today),
        'فردا ۸ وقت خالی');
  });

  test('further out, the weekday says it', () {
    final label = ProviderMetaLine.freeSlotsLabel(DateTime(2026, 9, 24), 3, today);
    expect(label, isNotNull);
    expect(label, contains('۳ وقت خالی'));
    expect(label, isNot(contains('امروز')));
  });

  test('nothing free says nothing at all', () {
    expect(ProviderMetaLine.freeSlotsLabel(null, 0, today), isNull);
    expect(ProviderMetaLine.freeSlotsLabel(DateTime(2026, 9, 21), 0, today), isNull);
  });
}
