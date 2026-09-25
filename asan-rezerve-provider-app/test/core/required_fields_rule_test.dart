import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// A standing rule for every form in this app (product owner, 2026-09-19):
/// a required field is marked, and an empty one says so UNDER that field. A
/// single "please fill in all the fields" leaves the user hunting for which one
/// is empty — the exact complaint that produced this rule.
///
/// This is a source scan rather than a widget test on purpose: it covers forms
/// nobody has written a widget test for yet, including ones added later.
void main() {
  test('no screen answers with a generic "fill in all the fields"', () {
    final offenders = <String>[];
    final banned = RegExp('تمام فیلد|همه فیلد|همه‌ی فیلد');

    for (final entity in Directory('lib').listSync(recursive: true)) {
      if (entity is! File || !entity.path.endsWith('.dart')) continue;
      final lines = entity.readAsLinesSync();
      for (var i = 0; i < lines.length; i++) {
        if (banned.hasMatch(lines[i])) {
          offenders.add('${entity.path}:${i + 1}  ${lines[i].trim()}');
        }
      }
    }

    expect(
      offenders,
      isEmpty,
      reason: 'Name the field that is empty, under that field '
          '(AppTextField.isRequired + errorText):\n${offenders.join('\n')}',
    );
  });
}
