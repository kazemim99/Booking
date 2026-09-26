import 'dart:io';

import 'package:asan_rezerve_provider_app/core/widgets/brand_mark.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/pages/splash_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The splash shows the brand, not a stock Material icon, and the painted mark is the designer's mark.
void main() {
  group('BrandMark', () {
    // Tests run from the app folder; the designer's files live in the repository's branding/ folder.
    final svg = File('../branding/symbol-color.svg').readAsStringSync();
    double n(String s) => double.parse(s);

    test('draws the «A» of branding/symbol-color.svg', () {
      final lines = RegExp(r'<line x1="([\d.]+)" y1="([\d.]+)" x2="([\d.]+)" y2="([\d.]+)"[^>]*stroke-width="([\d.]+)"')
          .allMatches(svg)
          .toList();
      expect(lines, hasLength(2));
      for (final (i, m) in lines.indexed) {
        expect(Offset(n(m[1]!), n(m[2]!)), BrandMark.letter[0], reason: 'line $i starts at the apex');
        expect(Offset(n(m[3]!), n(m[4]!)), BrandMark.letter[i + 1], reason: 'line $i ends at a foot');
        expect(n(m[5]!), BrandMark.strokeWidth);
      }
    });

    test('draws the check mark of branding/symbol-color.svg', () {
      final points = RegExp(r'<polyline points="([^"]+)"').firstMatch(svg)![1]!.split(' ').map((p) {
        final xy = p.split(',').map(n).toList();
        return Offset(xy[0], xy[1]);
      }).toList();
      expect(points, BrandMark.check);
    });

    testWidgets('is decorative unless given a label', (tester) async {
      final semantics = tester.ensureSemantics();
      await tester.pumpWidget(const MaterialApp(home: Row(children: [BrandMark(), BrandMark(semanticLabel: 'آسان رزرو')])));
      expect(find.bySemanticsLabel('آسان رزرو'), findsOneWidget);
      expect(tester.getSize(find.byType(CustomPaint).last), const Size.square(96));
      semantics.dispose();
    });
  });

  testWidgets('SplashPage shows the brand mark instead of a Material icon', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: SplashPage()));
    expect(find.byType(BrandMark), findsOneWidget);
    expect(find.byType(Icon), findsNothing);
  });
}
