import 'dart:math' as math;

import 'package:booksy_customer_app/config/theme/app_colors.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/config/theme/app_tokens.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Relative luminance (WCAG 2.x) for a fully opaque color.
double _luminance(Color c) {
  double channel(double v) {
    final s = v / 255.0;
    return s <= 0.03928 ? s / 12.92 : math.pow((s + 0.055) / 1.055, 2.4) as double;
  }

  return 0.2126 * channel((c.r * 255).roundToDouble()) +
      0.7152 * channel((c.g * 255).roundToDouble()) +
      0.0722 * channel((c.b * 255).roundToDouble());
}

double _contrast(Color a, Color b) {
  final la = _luminance(a);
  final lb = _luminance(b);
  final hi = math.max(la, lb);
  final lo = math.min(la, lb);
  return (hi + 0.05) / (lo + 0.05);
}

void main() {
  final theme = AppTheme.light;

  group('Palette aligned to the Provider (Coliride) values', () {
    test('primary is Coliride blue and app bar is blue chrome', () {
      expect(AppColors.primary, const Color(0xFF3777BF));
      expect(AppColors.appBar, const Color(0xFF3777C0));
      expect(theme.colorScheme.primary, AppColors.primary);
    });

    test('semantic + structural colors match', () {
      expect(AppColors.success, const Color(0xFF0AC075));
      expect(AppColors.warning, const Color(0xFFFFCB33));
      expect(AppColors.error, const Color(0xFFFF6171));
      expect(AppColors.border, const Color(0xFFEBEEF3));
      expect(AppColors.divider, const Color(0xFFE5E9F2));
      expect(AppColors.textPrimary, const Color(0xFF4D5E80)); // navy ink
    });

    test('scaffold is white (flat look)', () {
      expect(theme.scaffoldBackgroundColor, AppColors.background);
      expect(AppColors.background, const Color(0xFFFFFFFF));
    });
  });

  group('Flat "borders over shadows" elevation policy', () {
    test('app bar is blue chrome, flat, white content', () {
      expect(theme.appBarTheme.backgroundColor, AppColors.appBar);
      expect(theme.appBarTheme.foregroundColor, Colors.white);
      expect(theme.appBarTheme.elevation, 0);
      expect(theme.appBarTheme.scrolledUnderElevation, 0);
      expect(theme.appBarTheme.centerTitle, isTrue);
    });

    test('cards, dialogs, sheets render without shadow', () {
      expect(theme.cardTheme.elevation, 0);
      expect(theme.dialogTheme.elevation, 0);
      expect(theme.bottomSheetTheme.elevation, 0);
    });

    test('card is separated by a border, not a shadow', () {
      final shape = theme.cardTheme.shape as RoundedRectangleBorder;
      expect(shape.side.color, AppColors.border);
    });

    test('AppElevation class is retained', () {
      expect(AppElevation.none, 0);
      expect(AppElevation.medium, 3);
    });
  });

  group('Component radii aligned', () {
    test('component radius tokens match the Provider scale', () {
      expect(AppRadius.button, 10);
      expect(AppRadius.field, 12);
      expect(AppRadius.card, 15);
      expect(AppRadius.bottomSheet, 14);
      expect(AppRadius.panel, 16);
      expect(AppRadius.snackbar, 12);
    });

    test('card and dialog use their aligned radii', () {
      final card = theme.cardTheme.shape as RoundedRectangleBorder;
      final dialog = theme.dialogTheme.shape as RoundedRectangleBorder;
      expect((card.borderRadius as BorderRadius).topLeft.x, AppRadius.card);
      expect((dialog.borderRadius as BorderRadius).topLeft.x, AppRadius.panel);
    });
  });

  group('Motion aligned', () {
    test('fast motion is 180ms with easeOutCubic emphasis', () {
      expect(AppMotion.fast, const Duration(milliseconds: 180));
      expect(AppMotion.emphasized, Curves.easeOutCubic);
    });
  });

  group('Accessibility (WCAG AA) preserved on aligned text tones', () {
    test('navy ink body/heading text meets AA on white', () {
      expect(_contrast(AppColors.textPrimary, Colors.white),
          greaterThanOrEqualTo(4.5));
    });

    test('secondary and tertiary text meet AA on white', () {
      expect(_contrast(AppColors.textSecondary, Colors.white),
          greaterThanOrEqualTo(4.5));
      expect(_contrast(AppColors.textTertiary, Colors.white),
          greaterThanOrEqualTo(4.5));
    });

    test('semantic text variants meet AA on white', () {
      for (final c in [
        AppColors.successText,
        AppColors.warningText,
        AppColors.errorText,
        AppColors.infoText,
      ]) {
        expect(_contrast(c, Colors.white), greaterThanOrEqualTo(4.5));
      }
    });
  });
}
