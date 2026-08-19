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

    test(
        'outlined (secondary role) buttons carry a 2px primary border '
        'and primary label', () {
      final style = theme.outlinedButtonTheme.style!;
      expect(style.foregroundColor?.resolve({}), AppColors.primary);
      final side = style.side!.resolve({})!;
      expect(side.color, AppColors.primary);
      expect(side.width, AppDimens.secondaryButtonBorderWidth);
    });

    test('disabled outlined buttons swap border and label to muted grey', () {
      final style = theme.outlinedButtonTheme.style!;
      expect(style.foregroundColor?.resolve({WidgetState.disabled}),
          AppColors.disabled);
      expect(style.side?.resolve({WidgetState.disabled})?.color,
          AppColors.disabled);
    });

    test('text buttons use navy ink', () {
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

    test('error borders use the production input-error red, not danger', () {
      final error =
          theme.inputDecorationTheme.errorBorder! as OutlineInputBorder;
      expect(error.borderSide.color, AppColors.inputError);
      expect(error.borderSide.color, const Color(0xFFE74A3B));
      final focusedError = theme.inputDecorationTheme.focusedErrorBorder!
          as OutlineInputBorder;
      expect(focusedError.borderSide.color, AppColors.inputError);
      expect(focusedError.borderSide.width, AppDimens.inputFocusBorderWidth);
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

  group('tabs', () {
    test('active label and 2px bottom indicator are green over navy', () {
      final tabs = theme.tabBarTheme;
      expect(tabs.labelColor, AppColors.success);
      expect(tabs.unselectedLabelColor, AppColors.ink);
      expect(tabs.labelStyle?.fontSize, AppDimens.tabFontSize);
      expect(tabs.labelStyle?.fontWeight, FontWeight.w700);
      expect(tabs.dividerColor, AppColors.border);
      expect(tabs.indicatorSize, TabBarIndicatorSize.tab);
      final indicator = tabs.indicator! as BoxDecoration;
      expect(indicator.border!.bottom.color, AppColors.success);
      expect(indicator.border!.bottom.width, AppDimens.tabIndicatorWidth);
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
      expect(AppColors.loadingOverlay, const Color(0x22000000));
    });

    test('structure greys from the Coliride production palette', () {
      expect(AppColors.menuBorder, const Color(0xFFE5E8EB));
      expect(AppColors.dividerSoft, const Color(0xFFE8EDF4));
      expect(AppColors.readLabel, const Color(0xFFB8C1D1));
      expect(AppColors.subtitle, const Color(0xFF7F8696));
      expect(AppColors.avatarBorder, const Color(0xFFF0F0F0));
      expect(AppColors.checkOff, const Color(0xFFC7CFDE));
      expect(AppColors.surface, const Color(0xFFF5F5F5));
    });

    test('choice-chip pastel family', () {
      expect(AppColors.chipPink, const Color(0xFFFFE0FC));
      expect(AppColors.chipPeriwinkle, const Color(0xFFD3DCFF));
      expect(AppColors.chipMint, const Color(0xFFCBFAEA));
      expect(AppColors.chipRose, const Color(0xFFFFD3D4));
      expect(AppColors.chipButter, const Color(0xFFFBECBB));
    });

    test('button size ramp dimensions', () {
      expect(AppDimens.buttonHeight, 46);
      expect(AppDimens.buttonFontSize, 17);
      expect(AppDimens.buttonMediumFontSize, 16);
      expect(AppDimens.buttonDialogHeight, 40);
      expect(AppDimens.buttonDialogFontSize, 15.5);
      expect(AppDimens.buttonSmallHeight, 30);
      expect(AppDimens.buttonSmallFontSize, 14);
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

    testWidgets('is brand blue on the secondary (outlined) variant',
        (tester) async {
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: const Scaffold(
          body: AppButton.secondary(label: 'انصراف', loading: true),
        ),
      ));
      final spinner = tester.widget<CircularProgressIndicator>(
          find.byType(CircularProgressIndicator));
      expect(spinner.color, AppTheme.light.colorScheme.primary);
    });

    testWidgets('is white on the destructive variant', (tester) async {
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: const Scaffold(
          body: AppButton.destructive(label: 'حذف', loading: true),
        ),
      ));
      final spinner = tester.widget<CircularProgressIndicator>(
          find.byType(CircularProgressIndicator));
      expect(spinner.color, AppTheme.light.colorScheme.onPrimary);
    });
  });
}
