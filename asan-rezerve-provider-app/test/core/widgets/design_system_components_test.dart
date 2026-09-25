import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/config/theme/app_tokens.dart';
import 'package:booksy_provider_app/core/widgets/app_bottom_bar.dart';
import 'package:booksy_provider_app/core/widgets/app_button.dart';
import 'package:booksy_provider_app/core/widgets/app_choice_chip.dart';
import 'package:booksy_provider_app/core/widgets/app_dialog_header.dart';
import 'package:booksy_provider_app/core/widgets/app_sheet.dart';
import 'package:booksy_provider_app/core/widgets/app_text_tabs.dart';
import 'package:booksy_provider_app/core/widgets/profile_header.dart';
import 'package:booksy_provider_app/core/widgets/sticky_action_bar.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Design-system components from DESIGN_LANGUAGE.md §5: the button ramp,
/// sticky action footer, text tabs, pastel choice chips, identity header,
/// and the floating nav pill.
void main() {
  Future<void> pump(WidgetTester tester, Widget child) {
    return tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: child),
        ),
      ),
    );
  }

  group('AppButton roles', () {
    testWidgets('primary renders a FilledButton', (tester) async {
      await pump(tester, const AppButton(label: 'ذخیره'));
      expect(find.byType(FilledButton), findsOneWidget);
    });

    testWidgets('secondary renders an OutlinedButton', (tester) async {
      await pump(tester, const AppButton.secondary(label: 'انصراف'));
      expect(find.byType(OutlinedButton), findsOneWidget);
    });

    testWidgets('destructive fills coral and disables to grey',
        (tester) async {
      await pump(
          tester, AppButton.destructive(label: 'حذف', onPressed: () {}));
      final style =
          tester.widget<FilledButton>(find.byType(FilledButton)).style!;
      expect(style.backgroundColor!.resolve({}), AppColors.danger);
      expect(style.backgroundColor!.resolve({WidgetState.disabled}),
          AppColors.disabled);
    });

    testWidgets('loading disables the button', (tester) async {
      var tapped = false;
      await pump(
        tester,
        AppButton(label: 'ذخیره', loading: true, onPressed: () => tapped = true),
      );
      final button = tester.widget<FilledButton>(find.byType(FilledButton));
      expect(button.onPressed, isNull);
      expect(tapped, isFalse);
    });
  });

  group('AppButton size ramp', () {
    Future<ButtonStyle> styleFor(WidgetTester tester, AppButtonSize size) async {
      await pump(tester, AppButton(label: 'ثبت', size: size));
      return tester.widget<FilledButton>(find.byType(FilledButton)).style!;
    }

    testWidgets('big: 46dp / 17 bold', (tester) async {
      final style = await styleFor(tester, AppButtonSize.big);
      expect(style.minimumSize!.resolve({}),
          const Size.fromHeight(AppDimens.buttonHeight));
      expect(style.textStyle!.resolve({})!.fontSize, AppDimens.buttonFontSize);
    });

    testWidgets('medium: 46dp / 16', (tester) async {
      final style = await styleFor(tester, AppButtonSize.medium);
      expect(style.minimumSize!.resolve({}),
          const Size.fromHeight(AppDimens.buttonHeight));
      expect(style.textStyle!.resolve({})!.fontSize,
          AppDimens.buttonMediumFontSize);
    });

    testWidgets('dialog: 40dp / 15.5', (tester) async {
      final style = await styleFor(tester, AppButtonSize.dialog);
      expect(style.minimumSize!.resolve({}),
          const Size.fromHeight(AppDimens.buttonDialogHeight));
      expect(style.textStyle!.resolve({})!.fontSize,
          AppDimens.buttonDialogFontSize);
    });

    testWidgets('small: 30dp / 14', (tester) async {
      final style = await styleFor(tester, AppButtonSize.small);
      expect(style.minimumSize!.resolve({}),
          const Size.fromHeight(AppDimens.buttonSmallHeight));
      expect(style.textStyle!.resolve({})!.fontSize,
          AppDimens.buttonSmallFontSize);
    });
  });

  group('StickyActionBar', () {
    testWidgets('paired layout puts both actions in Expanded halves',
        (tester) async {
      await pump(
        tester,
        const StickyActionBar(
          primary: AppButton(label: 'ذخیره'),
          secondary: AppButton.secondary(label: 'انصراف'),
        ),
      );
      expect(
        find.descendant(
          of: find.byType(StickyActionBar),
          matching: find.byType(Expanded),
        ),
        findsNWidgets(2),
      );
      expect(find.byType(FilledButton), findsOneWidget);
      expect(find.byType(OutlinedButton), findsOneWidget);
    });

    testWidgets('stacked layout keeps actions full-width in a Column',
        (tester) async {
      await pump(
        tester,
        const StickyActionBar(
          stacked: true,
          primary: AppButton(label: 'ذخیره'),
          secondary: AppButton.secondary(label: 'انصراف'),
        ),
      );
      expect(
        find.descendant(
          of: find.byType(StickyActionBar),
          matching: find.byType(Expanded),
        ),
        findsNothing,
      );
      final primaryBox = tester.getRect(find.byType(FilledButton));
      final secondaryBox = tester.getRect(find.byType(OutlinedButton));
      expect(primaryBox.top, lessThan(secondaryBox.top));
      expect(primaryBox.width, secondaryBox.width);
    });

    testWidgets('carries the hairline top border and 12px padding',
        (tester) async {
      await pump(
        tester,
        const StickyActionBar(primary: AppButton(label: 'ذخیره')),
      );
      final container = tester.widget<Container>(
        find.descendant(
          of: find.byType(StickyActionBar),
          matching: find.byType(Container),
        ).first,
      );
      final decoration = container.decoration! as BoxDecoration;
      expect(decoration.border!.top.color, AppColors.menuBorder);
      expect(container.padding, const EdgeInsets.all(AppSpacing.card));
    });
  });

  group('AppTextTabs', () {
    Widget tabsHost(List<String> tabs) {
      return DefaultTabController(
        length: tabs.length,
        child: AppTextTabs(tabs: tabs),
      );
    }

    testWidgets('renders one Tab per label', (tester) async {
      await pump(tester, tabsHost(const ['مشخصات', 'آدرس']));
      expect(find.byType(Tab), findsNWidgets(2));
      expect(find.text('مشخصات'), findsOneWidget);
    });

    testWidgets('three or fewer tabs stay fixed; more become scrollable',
        (tester) async {
      await pump(tester, tabsHost(const ['۱', '۲', '۳']));
      expect(tester.widget<TabBar>(find.byType(TabBar)).isScrollable, isFalse);

      await pump(tester, tabsHost(const ['۱', '۲', '۳', '۴']));
      expect(tester.widget<TabBar>(find.byType(TabBar)).isScrollable, isTrue);
    });
  });

  group('AppChoiceChip', () {
    testWidgets('unselected: pastel fill, accent icon, ink text',
        (tester) async {
      await pump(
        tester,
        const AppChoiceChip(
          label: 'اصلاح مو',
          icon: Icons.cut,
          style: AppChipStyle.mint,
        ),
      );
      final material = tester.widget<Material>(
        find.ancestor(
          of: find.byType(InkWell),
          matching: find.byType(Material),
        ).first,
      );
      expect(material.color, AppColors.chipMint);
      final icon = tester.widget<Icon>(find.byIcon(Icons.cut));
      expect(icon.color, AppChipStyle.mint.accent);
      final text = tester.widget<Text>(find.text('اصلاح مو'));
      expect(text.style?.color, AppColors.ink);
    });

    testWidgets('selected: solid green fill with white icon and text',
        (tester) async {
      await pump(
        tester,
        const AppChoiceChip(
          label: 'اصلاح مو',
          icon: Icons.cut,
          selected: true,
        ),
      );
      final material = tester.widget<Material>(
        find.ancestor(
          of: find.byType(InkWell),
          matching: find.byType(Material),
        ).first,
      );
      expect(material.color, AppColors.success);
      expect(tester.widget<Icon>(find.byIcon(Icons.cut)).color, Colors.white);
      expect(
          tester.widget<Text>(find.text('اصلاح مو')).style?.color, Colors.white);
    });

    testWidgets('taps fire and selection is exposed to semantics',
        (tester) async {
      var tapped = false;
      await pump(
        tester,
        AppChoiceChip(
          label: 'اصلاح مو',
          selected: true,
          onTap: () => tapped = true,
        ),
      );
      await tester.tap(find.byType(AppChoiceChip));
      expect(tapped, isTrue);
      expect(
        tester.getSemantics(find.byType(InkWell).first),
        isSemantics(isSelected: true),
      );
    });
  });

  group('ProfileHeader', () {
    testWidgets('shows name and subtitle in white on the blue chrome',
        (tester) async {
      await pump(
        tester,
        const ProfileHeader(name: 'آرایشگاه نمونه', subtitle: 'تهران'),
      );
      expect(tester.widget<Text>(find.text('آرایشگاه نمونه')).style?.color,
          Colors.white);
      expect(find.text('تهران'), findsOneWidget);
      final chrome = tester.widget<ColoredBox>(
        find
            .descendant(
              of: find.byType(ProfileHeader),
              matching: find.byType(ColoredBox),
            )
            .first,
      );
      expect(chrome.color, AppColors.appBar);
    });

    testWidgets('completion below 100% draws the ring and the percent pill',
        (tester) async {
      await pump(
        tester,
        const ProfileHeader(name: 'آرایشگاه', completion: 0.75),
      );
      expect(find.text('٪75'), findsOneWidget);
      expect(
        find.descendant(
          of: find.byType(ProfileHeader),
          matching: find.byType(CustomPaint),
        ),
        findsWidgets,
      );
    });

    testWidgets('completion of 100% hides the pill', (tester) async {
      await pump(
        tester,
        const ProfileHeader(name: 'آرایشگاه', completion: 1),
      );
      expect(find.textContaining('٪'), findsNothing);
    });

    testWidgets('camera badge appears only with onEditAvatar and fires it',
        (tester) async {
      await pump(tester, const ProfileHeader(name: 'آرایشگاه'));
      expect(find.byIcon(Icons.photo_camera_outlined), findsNothing);

      var edited = false;
      await pump(
        tester,
        ProfileHeader(name: 'آرایشگاه', onEditAvatar: () => edited = true),
      );
      await tester.tap(find.byIcon(Icons.photo_camera_outlined));
      expect(edited, isTrue);
    });
  });

  group('AppBottomBar', () {
    const items = [
      AppBottomBarItem(icon: Icons.today, semanticLabel: 'امروز'),
      AppBottomBarItem(icon: Icons.calendar_month, badgeCount: 3),
      AppBottomBarItem(icon: Icons.people),
    ];

    testWidgets('renders the blue pill with an item per destination',
        (tester) async {
      await pump(
        tester,
        const AppBottomBar(items: items, activeIndex: 0),
      );
      final pill = tester.widget<Container>(
        find.descendant(
          of: find.byType(AppBottomBar),
          matching: find.byType(Container),
        ).first,
      );
      final decoration = pill.decoration! as BoxDecoration;
      expect(decoration.color, AppColors.appBar);
      expect(decoration.borderRadius,
          BorderRadius.circular(AppRadius.panel));
      expect(find.byType(InkWell), findsNWidgets(3));
    });

    testWidgets('badge shows the count in coral', (tester) async {
      await pump(
        tester,
        const AppBottomBar(items: items, activeIndex: 0),
      );
      final badge = tester.widget<Badge>(find.byType(Badge));
      expect(badge.backgroundColor, AppColors.danger);
      expect(find.text('3'), findsOneWidget);
    });

    testWidgets('counts above 99 cap at ۹۹+', (tester) async {
      await pump(
        tester,
        const AppBottomBar(
          items: [AppBottomBarItem(icon: Icons.today, badgeCount: 120)],
          activeIndex: 0,
        ),
      );
      expect(find.text('۹۹+'), findsOneWidget);
    });

    testWidgets('tapping an inactive item reports its index; active is inert',
        (tester) async {
      int? tappedIndex;
      await pump(
        tester,
        AppBottomBar(
          items: items,
          activeIndex: 0,
          onTap: (i) => tappedIndex = i,
        ),
      );
      await tester.tap(find.byIcon(Icons.people));
      expect(tappedIndex, 2);

      tappedIndex = null;
      await tester.tap(find.byIcon(Icons.today));
      expect(tappedIndex, isNull);
    });

    testWidgets('center slot splits the destinations around it',
        (tester) async {
      await pump(
        tester,
        const AppBottomBar(
          items: [
            AppBottomBarItem(icon: Icons.today),
            AppBottomBarItem(icon: Icons.calendar_month),
            AppBottomBarItem(icon: Icons.people),
            AppBottomBarItem(icon: Icons.more_horiz),
          ],
          activeIndex: 0,
          center: Icon(Icons.add, key: Key('center-action')),
        ),
      );
      expect(find.byKey(const Key('center-action')), findsOneWidget);
      // In RTL the first two destinations sit to the RIGHT of the center
      // action and the last two to its LEFT.
      final centerX = tester.getCenter(find.byKey(const Key('center-action'))).dx;
      expect(tester.getCenter(find.byIcon(Icons.today)).dx, greaterThan(centerX));
      expect(tester.getCenter(find.byIcon(Icons.calendar_month)).dx,
          greaterThan(centerX));
      expect(tester.getCenter(find.byIcon(Icons.people)).dx, lessThan(centerX));
      expect(tester.getCenter(find.byIcon(Icons.more_horiz)).dx,
          lessThan(centerX));
    });
  });

  group('AppSheetScaffold', () {
    testWidgets('modal variant: leading bold ink title and grey close disc',
        (tester) async {
      var closed = false;
      await pump(
        tester,
        AppSheetScaffold(
          title: 'انتخاب خدمت',
          onClose: () => closed = true,
          child: const SizedBox(height: 100),
        ),
      );
      final title = tester.widget<Text>(find.text('انتخاب خدمت'));
      expect(title.style?.color, AppColors.ink);
      expect(title.style?.fontSize, 18);
      expect(title.style?.fontWeight, FontWeight.bold);

      await tester.tap(find.byIcon(Icons.close));
      expect(closed, isTrue);
    });

    testWidgets('picker variant: drag handle and centered title',
        (tester) async {
      await pump(
        tester,
        const AppSheetScaffold.picker(
          title: 'انتخاب شهر',
          child: SizedBox(height: 100),
        ),
      );
      expect(find.byIcon(Icons.close), findsNothing);
      final handle = tester.widget<Container>(
        find
            .descendant(
              of: find.byType(AppSheetScaffold),
              matching: find.byType(Container),
            )
            .first,
      );
      expect(
        handle.constraints,
        BoxConstraints.tightFor(width: 40, height: 5),
      );
      expect(find.text('انتخاب شهر'), findsOneWidget);
      // Picker default shows the accent divider under the title.
      expect(find.byType(Divider), findsOneWidget);
    });
  });

  group('AppDialogHeader', () {
    testWidgets('centers the bold ink title in a 40px band with a close disc',
        (tester) async {
      var closed = false;
      await pump(
        tester,
        AppDialogHeader(title: 'تغییر سالن', onClose: () => closed = true),
      );
      final title = tester.widget<Text>(find.text('تغییر سالن'));
      expect(title.style?.color, AppColors.ink);
      expect(title.style?.fontWeight, FontWeight.bold);

      await tester.tap(find.byIcon(Icons.close));
      expect(closed, isTrue);
    });

    testWidgets('divider is opt-in', (tester) async {
      await pump(tester, const AppDialogHeader(title: 'عنوان'));
      expect(find.byType(Divider), findsNothing);

      await pump(
          tester, const AppDialogHeader(title: 'عنوان', showDivider: true));
      expect(find.byType(Divider), findsOneWidget);
    });
  });
}
