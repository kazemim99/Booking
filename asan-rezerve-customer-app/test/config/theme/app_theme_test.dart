import 'package:asan_rezerve_customer_app/config/theme/app_colors.dart';
import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/config/theme/app_tokens.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/widgets/app_bottom_bar.dart';
import 'package:asan_rezerve_customer_app/core/widgets/app_button.dart';
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/contrast.dart';

double _contrast(Color a, Color b) => contrastRatio(a, b);

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

  // customer-app-ux-review-fixes A.1–A.4: every colour pair the theme defines is guarded here, so a
  // palette tweak that drops a pair below WCAG AA fails a test instead of shipping. Text needs 4.5:1,
  // icons and other UI parts 3:1. Values are read from the theme (or the widget that draws them), not
  // from AppColors, so the guard follows what reaches the screen.
  group('Contrast guard — every text/icon pair the theme defines', () {
    final cs = theme.colorScheme;
    final input = theme.inputDecorationTheme;

    void aa(Color fg, Color bg, String what) => expect(
          contrastRatio(fg, bg),
          greaterThanOrEqualTo(kAaText),
          reason: '$what: text needs 4.5:1',
        );
    void ui(Color fg, Color bg, String what) => expect(
          contrastRatio(fg, bg),
          greaterThanOrEqualTo(kAaNonText),
          reason: '$what: icons/UI need 3:1',
        );

    test('colour scheme text pairs', () {
      aa(cs.onPrimary, cs.primary, 'onPrimary on primary');
      aa(cs.onSurface, cs.surface, 'onSurface on surface');
      aa(cs.onSurfaceVariant, cs.surface, 'onSurfaceVariant on surface');
      aa(cs.error, cs.surface, 'error text on surface');
      aa(cs.onError, cs.error, 'onError on error');
      aa(cs.onSecondaryContainer, cs.secondaryContainer,
          'onSecondaryContainer on secondaryContainer');
    });

    test('secondaryContainer is its own light tint, never the green accent', () {
      // Flutter falls back to `secondary` when secondaryContainer is unset, which painted the
      // selected segment and the map notice in the green accent (white-on-green is 2.38:1).
      expect(cs.secondaryContainer, isNot(cs.secondary));
      expect(relativeLuminance(cs.secondaryContainer),
          greaterThan(relativeLuminance(cs.onSecondaryContainer)),
          reason: 'a light container with dark text');
      expect(cs.onSecondaryContainer, isNot(cs.onSecondary));
    });

    test('the error colour is an AA red; the coral stays for fills, the field border keeps its own', () {
      expect(cs.error, AppColors.errorText);
      expect(AppColors.error, const Color(0xFFFF6171));
      expect((input.errorBorder! as OutlineInputBorder).borderSide.color,
          AppColors.inputErrorBorder);
    });

    test('input hint text and field icons on white and on the soft pill fill', () {
      for (final bg in [cs.surface, AppColors.surfaceSoft]) {
        aa(input.hintStyle!.color!, bg, 'hint on $bg');
        ui(input.prefixIconColor!, bg, 'prefix icon on $bg');
        ui(input.suffixIconColor!, bg, 'suffix icon on $bg');
        ui(input.iconColor!, bg, 'field icon on $bg');
      }
      aa(input.labelStyle!.color!, cs.surface, 'field label');
      aa(input.errorStyle!.color!, cs.surface, 'field error text');
    });

    test('app bar foreground on the app bar', () {
      final bar = theme.appBarTheme;
      aa(bar.foregroundColor!, bar.backgroundColor!, 'app bar foreground');
      aa(bar.titleTextStyle!.color!, bar.backgroundColor!, 'app bar title');
      ui(bar.iconTheme!.color!, bar.backgroundColor!, 'app bar icons');
      ui(bar.actionsIconTheme!.color!, bar.backgroundColor!, 'app bar actions');
    });

    test('snack bar content and action on the snack bar', () {
      final snack = theme.snackBarTheme;
      aa(snack.contentTextStyle!.color!, snack.backgroundColor!, 'snack bar text');
      aa(snack.actionTextColor!, snack.backgroundColor!, 'snack bar action');
    });

    test('tab labels and indicator on the surface', () {
      final tabs = theme.tabBarTheme;
      aa(tabs.labelColor!, cs.surface, 'selected tab label');
      aa(tabs.unselectedLabelColor!, cs.surface, 'unselected tab label');
      final indicator = (tabs.indicator! as BoxDecoration).border!.bottom.color;
      ui(indicator, cs.surface, 'tab indicator');
    });

    test('filled, outlined and text buttons', () {
      final filled = theme.elevatedButtonTheme.style!;
      aa(filled.foregroundColor!.resolve({})!,
          filled.backgroundColor!.resolve({})!, 'filled button');
      aa(theme.outlinedButtonTheme.style!.foregroundColor!.resolve({})!,
          cs.surface, 'outlined button');
      aa(theme.textButtonTheme.style!.foregroundColor!.resolve({})!,
          cs.surface, 'text button');
    });

    test('selection controls: the check and the thumb stand out from the fill', () {
      final selected = {WidgetState.selected};
      final fill = theme.checkboxTheme.fillColor!.resolve(selected)!;
      ui(fill, cs.surface, 'checked box on the surface');
      ui(Colors.white, fill, 'check mark on the checked box');
      final track = theme.switchTheme.trackColor!.resolve(selected)!;
      ui(theme.switchTheme.thumbColor!.resolve(selected)!, track, 'switch thumb on track');
    });

    test('Material navigation bar labels and icons on the bar', () {
      final nav = theme.navigationBarTheme;
      for (final states in [<WidgetState>{}, {WidgetState.selected}]) {
        aa(nav.labelTextStyle!.resolve(states)!.color!, nav.backgroundColor!,
            'nav label $states');
        ui(nav.iconTheme!.resolve(states)!.color!, nav.backgroundColor!,
            'nav icon $states');
      }
    });
  });

  // Review of the merged branch: filled stars were the yellow warning fill (#FFCB33, 1.52:1 on white) — a rating
  // a customer cannot see. One star colour, used by every star, that is a graphic at 3:1 and still reads as gold.
  group('Contrast guard — the star colour', () {
    test('a filled star is at least 3:1 on white and on the soft fill', () {
      expect(_contrast(AppColors.star, AppColors.surface), greaterThanOrEqualTo(kAaNonText));
      expect(_contrast(AppColors.star, AppColors.surfaceSoft), greaterThanOrEqualTo(kAaNonText));
    });

    test('it is a warm gold, not the yellow fill or a red', () {
      final hsl = HSLColor.fromColor(AppColors.star);
      expect(AppColors.star, isNot(AppColors.warning));
      expect(hsl.hue, inInclusiveRange(30, 50), reason: 'amber/gold hues');
      expect(hsl.saturation, greaterThan(0.6));
    });
  });

  // The destructive button and the floating nav pill draw their own colours; pump them and read
  // what they paint.
  group('Contrast guard — components that paint their own colours', () {
    Widget host(Widget child) => MaterialApp(
          theme: theme,
          home: Directionality(
            textDirection: TextDirection.rtl,
            child: Scaffold(body: Center(child: child)),
          ),
        );

    testWidgets('destructive button: white on an AA red', (tester) async {
      await tester.pumpWidget(
          host(AppButton.destructive(label: 'لغو نوبت', onPressed: () {})));
      final style = tester.widget<ElevatedButton>(find.byType(ElevatedButton)).style!;
      final bg = style.backgroundColor!.resolve({})!;
      final fg = style.foregroundColor!.resolve({})!;
      expect(bg, AppColors.errorText);
      expect(contrastRatio(fg, bg), greaterThanOrEqualTo(kAaText));
    });

    // Review of the merged branch: the count badge was white on the coral (2.92:1), which app_colors.dart itself
    // reserves for fills that carry no text.
    testWidgets('bottom navigation count badge: its number is 4.5:1 on the badge', (tester) async {
      await tester.pumpWidget(host(const AppBottomBar(activeIndex: 0, items: [
        AppBottomBarItem(
          icon: Icons.home_outlined,
          selectedIcon: Icons.home,
          semanticLabel: AppStrings.tabHome,
          badgeCount: 3,
        ),
      ])));

      final badge = tester.widget<Badge>(find.byType(Badge));
      expect(contrastRatio(badge.textColor!, badge.backgroundColor!), greaterThanOrEqualTo(kAaText));
    });

    testWidgets('bottom navigation: labels 4.5:1, icons 3:1 on the bar', (tester) async {
      await tester.pumpWidget(host(const AppBottomBar(activeIndex: 0, items: [
        AppBottomBarItem(
          icon: Icons.home_outlined,
          selectedIcon: Icons.home,
          semanticLabel: AppStrings.tabHome,
        ),
        AppBottomBarItem(
          icon: Icons.search_outlined,
          selectedIcon: Icons.search,
          semanticLabel: AppStrings.tabExplore,
        ),
      ])));

      const bar = AppColors.appBar;
      for (final label in [AppStrings.tabHome, AppStrings.tabExplore]) {
        final paragraph = tester.renderObject<RenderParagraph>(find.text(label));
        expect(contrastRatio(paragraph.text.style!.color!, bar),
            greaterThanOrEqualTo(kAaText),
            reason: '$label label on the bar');
      }
      final inactive = tester.widget<Icon>(find.byIcon(Icons.search_outlined));
      expect(contrastRatio(inactive.color!, bar), greaterThanOrEqualTo(kAaNonText),
          reason: 'inactive icon on the bar');
      final active = tester.widget<Icon>(find.byIcon(Icons.home));
      final underActive = Color.alphaBlend(AppColors.navIndicator, bar);
      expect(contrastRatio(active.color!, underActive),
          greaterThanOrEqualTo(kAaNonText),
          reason: 'active icon on its indicator');
    });
  });
}
