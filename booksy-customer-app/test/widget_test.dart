import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/theme/app_colors.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';

void main() {
  group('AppTheme', () {
    test('uses the dark-blue brand palette, not purple', () {
      final theme = AppTheme.light;
      expect(theme.colorScheme.primary, AppColors.primary);
      expect(theme.colorScheme.primary, const Color(0xFF1A365D));
      expect(theme.useMaterial3, isTrue);
    });

    test('uses Vazir as the app font', () {
      final theme = AppTheme.light;
      expect(theme.textTheme.bodyLarge?.fontFamily, 'Vazir');
      expect(theme.textTheme.labelLarge?.fontFamily, 'Vazir');
    });

    testWidgets('themed ElevatedButton meets minimum touch target',
        (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          home: Scaffold(
            body: Center(
              child: ElevatedButton(onPressed: () {}, child: const Text('ok')),
            ),
          ),
        ),
      );
      final size = tester.getSize(find.byType(ElevatedButton));
      expect(size.height, greaterThanOrEqualTo(48));
    });
  });
}
