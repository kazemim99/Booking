import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/config/theme/app_tokens.dart';
import 'package:booksy_provider_app/core/widgets/app_button.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Guards the Coliride visual language applied to the provider app:
/// blue chrome, navy ink, green success accent, flat surfaces with
/// borders-over-shadows, and the 10–16px radius scale.
void main() {
  final theme = AppTheme.light;

  group('brand palette', () {
    test('color scheme uses the Coliride brand colors', () {
      expect(theme.colorScheme.primary, const Color(0xFF3777BF));
      expect(theme.colorScheme.secondary, const Color(0xFF0AC075));
      expect(theme.colorScheme.error, const Color(0xFFFF6171));
      expect(theme.colorScheme.onSurface, const Color(0xFF4D5E80));
      expect(theme.colorScheme.onPrimary, Colors.white);
    });

    test('scaffold is white and typography is Vazir', () {
      expect(theme.scaffoldBackgroundColor, Colors.white);
      expect(theme.textTheme.bodyMedium?.fontFamily, 'Vazir');
    });
  });

  group('app bar chrome', () {
    test('is flat blue with white foreground', () {
      expect(theme.appBarTheme.backgroundColor, AppColors.appBar);
      expect(theme.appBarTheme.foregroundColor, Colors.white);
      expect(theme.appBarTheme.elevation, 0);
      expect(theme.appBarTheme.centerTitle, isTrue);
    });
  });

  group('buttons', () {
    test('filled buttons are flat, full-height 46, radius 10, bold 17', () {
      final style = theme.filledButtonTheme.style!;
      expect(style.minimumSize?.resolve({}),
          const Size.fromHeight(AppDimens.buttonHeight));
      expect(style.elevation?.resolve({}), 0);
      final shape = style.shape?.resolve({}) as RoundedRectangleBorder;
      expect(shape.borderRadius,
          BorderRadius.circular(AppRadius.button));
      final text = style.textStyle?.resolve({});
      expect(text?.fontSize, AppDimens.buttonFontSize);
      expect(text?.fontWeight, FontWeight.bold);
    });

    test('disabled filled buttons use the muted grey fill', () {
      final style = theme.filledButtonTheme.style!;
      expect(style.backgroundColor?.resolve({WidgetState.disabled}),
          AppColors.disabled);
    });

    test('outlined and text buttons use navy ink', () {
      expect(
          theme.outlinedButtonTheme.style!.foregroundColor?.resolve({}),
          AppColors.ink);
      expect(theme.textButtonTheme.style!.foregroundColor?.resolve({}),
          AppColors.ink);
    });
  });

  group('inputs', () {
    test('fields have radius 12 with the soft grey border pair', () {
      final enabled =
          theme.inputDecorationTheme.enabledBorder! as OutlineInputBorder;
      expect(enabled.borderRadius, BorderRadius.circular(AppRadius.field));
      expect(enabled.borderSide.color, AppColors.border);
      expect(enabled.borderSide.width, AppDimens.inputBorderWidth);

      final focused =
          theme.inputDecorationTheme.focusedBorder! as OutlineInputBorder;
      expect(focused.borderSide.color, AppColors.borderFocus);
      expect(focused.borderSide.width, AppDimens.inputFocusBorderWidth);
    });

    test('labels are bold navy, hints are soft grey', () {
      expect(theme.inputDecorationTheme.labelStyle?.color, AppColors.ink);
      expect(theme.inputDecorationTheme.labelStyle?.fontWeight,
          FontWeight.bold);
      expect(theme.inputDecorationTheme.hintStyle?.color, AppColors.hint);
    });
  });

  group('selection controls', () {
    test('checkbox fills green when selected, white otherwise', () {
      final fill = theme.checkboxTheme.fillColor!;
      expect(fill.resolve({WidgetState.selected}), AppColors.success);
      expect(fill.resolve({}), Colors.white);
    });

    test('switch track turns green when selected', () {
      expect(theme.switchTheme.trackColor?.resolve({WidgetState.selected}),
          AppColors.success);
    });
  });

  group('surfaces', () {
    test('cards are flat with a border instead of a shadow (radius 15)', () {
      expect(theme.cardTheme.elevation, 0);
      final shape = theme.cardTheme.shape! as RoundedRectangleBorder;
      expect(shape.borderRadius, BorderRadius.circular(AppRadius.card));
      expect(shape.side.color, AppColors.border);
    });

    test('bottom sheets round the top corners at 14', () {
      final shape = theme.bottomSheetTheme.shape! as RoundedRectangleBorder;
      expect(
          shape.borderRadius,
          const BorderRadius.vertical(
              top: Radius.circular(AppRadius.bottomSheet)));
    });

    test('snackbars float with radius 12', () {
      expect(theme.snackBarTheme.behavior, SnackBarBehavior.floating);
      final shape = theme.snackBarTheme.shape! as RoundedRectangleBorder;
      expect(shape.borderRadius, BorderRadius.circular(AppRadius.snackbar));
    });
  });

  group('structural tokens', () {
    test('motion tokens carry the Coliride timing', () {
      expect(AppMotion.fast, const Duration(milliseconds: 180));
      expect(AppMotion.medium, const Duration(milliseconds: 250));
      expect(AppMotion.curve, Curves.easeOutCubic);
      expect(AppMotion.reverseCurve, Curves.easeInCubic);
    });

    test('icon size ramp', () {
      expect(AppIconSize.sm, 16);
      expect(AppIconSize.md, 24);
      expect(AppIconSize.action, 20);
      expect(AppIconSize.hero, 72);
    });

    test('intra-card padding token', () {
      expect(AppSpacing.card, 12);
    });

    test('overlay barrier colors', () {
      expect(AppColors.dialogBarrier, const Color(0x24000000));
      expect(AppColors.sheetBarrier, const Color(0x47000000));
    });

    testWidgets('back icon is white via the global actionIconTheme',
        (tester) async {
      final builder = theme.actionIconTheme?.backButtonIconBuilder;
      expect(builder, isNotNull);
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: Builder(
          builder: (context) => Scaffold(body: builder!(context)),
        ),
      ));
      final icon = tester.widget<Icon>(find.byType(Icon));
      expect(icon.color, Colors.white);
      expect(icon.icon, Icons.arrow_back);
    });
  });

  group('AppButton loading spinner', () {
    testWidgets('is white on the filled (primary) variant', (tester) async {
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: const Scaffold(
          body: AppButton(label: 'ثبت', loading: true),
        ),
      ));
      final spinner = tester.widget<CircularProgressIndicator>(
          find.byType(CircularProgressIndicator));
      expect(spinner.color, AppTheme.light.colorScheme.onPrimary);
    });

    testWidgets('is brand blue on the text variant', (tester) async {
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: const Scaffold(
          body: AppButton.text(label: 'ثبت', loading: true),
        ),
      ));
      final spinner = tester.widget<CircularProgressIndicator>(
          find.byType(CircularProgressIndicator));
      expect(spinner.color, AppTheme.light.colorScheme.primary);
    });
  });
}
