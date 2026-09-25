import 'package:asan_rezerve_provider_app/config/theme/app_tokens.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_card.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_dashed_divider.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_icon_button.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_info_card.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_list_row.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_section_header.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_status_badge.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Direct component tests for the Group 3 structural widgets (spec:
/// shared-ui-components + design-system-foundations). These widgets shipped
/// pre-existing (commit df1433c5) with no dedicated tests of their own —
/// only indirect exercise through feature screens that consume them
/// (services_step_test.dart, preview_step, booking_card_test.dart) — so the
/// component-level scenarios below had never actually been asserted.
void main() {
  Widget wrap(Widget child) => MaterialApp(home: Material(child: child));

  group('AppCard (spec: Card components)', () {
    testWidgets('renders flat with a white surface, r15 border, no shadow',
        (tester) async {
      await tester.pumpWidget(wrap(const AppCard(child: Text('body'))));

      final container = tester.widget<Container>(find.byType(Container));
      final decoration = container.decoration as BoxDecoration;
      expect(decoration.color, Colors.white);
      expect(decoration.border, isA<Border>());
      expect(
        (decoration.border as Border).top.color,
        AppColors.border,
      );
      expect(
        (decoration.borderRadius as BorderRadius).topLeft.x,
        AppRadius.card,
      );
      expect(decoration.boxShadow, isNull);
    });

    test('interior padding defaults to the 12dp intra-card token', () {
      // Asserted on the widget's own field rather than the rendered tree:
      // Container(decoration: BoxDecoration(border: Border.all(...)))
      // inserts its own implicit 1dp Padding for the border inset, nested
      // above AppCard's, so "exactly one Padding" isn't a safe structural
      // assumption to build a widget test on.
      const card = AppCard(child: Text('body'));
      expect(card.padding, const EdgeInsets.all(AppSpacing.card));
    });
  });

  group('AppInfoCard (spec: Card components)', () {
    testWidgets('renders the tag strip label/icon and the tinted container',
        (tester) async {
      await tester.pumpWidget(
        wrap(
          const AppInfoCard(
            tagText: 'اطلاعات کسب‌وکار',
            icon: Icons.storefront_outlined,
            child: Text('محتوا'),
          ),
        ),
      );

      expect(find.text('اطلاعات کسب‌وکار'), findsOneWidget);
      expect(find.text('محتوا'), findsOneWidget);
      // Icon appears twice: once in the tag strip, once in the 40x40 tint.
      expect(find.byIcon(Icons.storefront_outlined), findsNWidgets(2));
      expect(find.byType(AppCard), findsOneWidget);
    });

    testWidgets('renders an optional trailing widget', (tester) async {
      await tester.pumpWidget(
        wrap(
          AppInfoCard(
            tagText: 'خدمات',
            icon: Icons.design_services_outlined,
            trailing: const Text('ویرایش'),
            child: const Text('لیست خدمات'),
          ),
        ),
      );

      expect(find.text('ویرایش'), findsOneWidget);
    });
  });

  group('AppListRow (spec: List row component)', () {
    testWidgets('row anatomy: icon grey, title navy, subtitle muted, r10 soft fill',
        (tester) async {
      await tester.pumpWidget(
        wrap(
          const AppListRow(
            leadingIcon: Icons.design_services_outlined,
            title: 'کوتاهی مو',
            subtitle: '۳۰ دقیقه',
          ),
        ),
      );

      final icon = tester.widget<Icon>(
        find.byIcon(Icons.design_services_outlined),
      );
      expect(icon.color, AppColors.icon);

      final title = tester.widget<Text>(find.text('کوتاهی مو'));
      expect(title.style?.color, AppColors.ink);

      final subtitle = tester.widget<Text>(find.text('۳۰ دقیقه'));
      expect(subtitle.style?.color, AppColors.muted);

      final material = tester.widget<Material>(
        find.descendant(
          of: find.byType(AppListRow),
          matching: find.byType(Material),
        ),
      );
      expect(material.color, AppColors.surfaceSoft);
      expect(
        (material.borderRadius as BorderRadius).topLeft.x,
        AppRadius.sm + 2,
      );

      final constrainedBox = tester.widget<Container>(
        find.descendant(
          of: find.byType(AppListRow),
          matching: find.byType(Container),
        ),
      );
      expect(
        (constrainedBox.constraints as BoxConstraints).minHeight,
        greaterThanOrEqualTo(48),
      );
    });

    testWidgets('tappable row shows a trailing chevron and ripples',
        (tester) async {
      var tapped = false;
      await tester.pumpWidget(
        wrap(
          AppListRow(
            title: 'ردیف قابل لمس',
            onTap: () => tapped = true,
          ),
        ),
      );

      expect(find.byIcon(Icons.arrow_forward_ios), findsOneWidget);
      expect(find.byType(InkWell), findsOneWidget);

      await tester.tap(find.byType(AppListRow));
      expect(tapped, isTrue);
    });

    testWidgets('a non-tappable row with no explicit trailing shows none',
        (tester) async {
      await tester.pumpWidget(wrap(const AppListRow(title: 'ردیف ساده')));

      expect(find.byIcon(Icons.arrow_forward_ios), findsNothing);
    });
  });

  group('AppSectionHeader (spec: Section header component)', () {
    testWidgets('header with action renders AppInlineAddButton in green',
        (tester) async {
      await tester.pumpWidget(
        wrap(
          AppSectionHeader(
            title: 'خدمات',
            action: AppInlineAddButton(
              label: 'افزودن خدمت',
              onPressed: () {},
            ),
          ),
        ),
      );

      expect(find.text('خدمات'), findsOneWidget);
      expect(find.byType(AppInlineAddButton), findsOneWidget);

      final label = tester.widget<Text>(find.text('افزودن خدمت'));
      expect(label.style?.color, AppColors.success);
    });

    testWidgets('header without action renders only the title',
        (tester) async {
      await tester.pumpWidget(
        wrap(const AppSectionHeader(title: 'فقط عنوان')),
      );

      expect(find.text('فقط عنوان'), findsOneWidget);
      expect(find.byType(AppInlineAddButton), findsNothing);
    });
  });

  group('AppStatusBadge (spec: Status badge component)', () {
    testWidgets('success maps to the green background with white text',
        (tester) async {
      await tester.pumpWidget(
        wrap(const AppStatusBadge(label: 'فعال', status: AppBadgeStatus.success)),
      );

      final container = tester.widget<Container>(find.byType(Container));
      final decoration = container.decoration as BoxDecoration;
      expect(decoration.color, AppColors.success);
      final text = tester.widget<Text>(find.text('فعال'));
      expect(text.style?.color, Colors.white);
    });

    testWidgets('neutral variant keeps navy text on a border-grey background',
        (tester) async {
      await tester.pumpWidget(
        wrap(const AppStatusBadge(label: 'نامشخص')),
      );

      final container = tester.widget<Container>(find.byType(Container));
      final decoration = container.decoration as BoxDecoration;
      expect(decoration.color, AppColors.border);
      final text = tester.widget<Text>(find.text('نامشخص'));
      expect(text.style?.color, AppColors.ink);
      expect(text.style?.color, isNot(Colors.white));
    });

    testWidgets('danger maps to the red background with white text',
        (tester) async {
      await tester.pumpWidget(
        wrap(const AppStatusBadge(label: 'لغو شده', status: AppBadgeStatus.danger)),
      );

      final container = tester.widget<Container>(find.byType(Container));
      final decoration = container.decoration as BoxDecoration;
      expect(decoration.color, AppColors.danger);
      final text = tester.widget<Text>(find.text('لغو شده'));
      expect(text.style?.color, Colors.white);
    });
  });

  group('AppIconButton (spec: Icon action button component)', () {
    testWidgets('badge attaches without shifting the 44x44 layout',
        (tester) async {
      await tester.pumpWidget(
        wrap(
          AppIconButton(
            icon: Icons.notifications_outlined,
            badgeLabel: '3',
            onTap: () {},
          ),
        ),
      );

      final button = tester.widget<RawMaterialButton>(
        find.byType(RawMaterialButton),
      );
      expect(button.constraints.biggest, const Size(44, 44));
      final badge = tester.widget<Badge>(find.byType(Badge));
      expect(badge.isLabelVisible, isTrue);
      expect(find.text('3'), findsOneWidget);
    });

    testWidgets('press feedback is flat: no elevation at any state',
        (tester) async {
      await tester.pumpWidget(
        wrap(AppIconButton(icon: Icons.close, onTap: () {})),
      );

      final button = tester.widget<RawMaterialButton>(
        find.byType(RawMaterialButton),
      );
      expect(button.elevation, 0);
      expect(button.hoverElevation, 0);
      expect(button.highlightElevation, 0);
      expect(button.disabledElevation, 0);
    });

    testWidgets('without a badge, no badge is shown', (tester) async {
      await tester.pumpWidget(
        wrap(AppIconButton(icon: Icons.close, onTap: () {})),
      );

      final badge = tester.widget<Badge>(find.byType(Badge));
      expect(badge.isLabelVisible, isFalse);
    });
  });

  group('AppDashedDivider (spec: Divider styles)', () {
    testWidgets('renders dash segments in the given color', (tester) async {
      await tester.pumpWidget(
        wrap(
          const SizedBox(
            width: 200,
            child: AppDashedDivider(color: AppColors.divider),
          ),
        ),
      );

      final dashes = tester.widgetList<DecoratedBox>(
        find.descendant(
          of: find.byType(AppDashedDivider),
          matching: find.byType(DecoratedBox),
        ),
      );
      expect(dashes, isNotEmpty);
      for (final dash in dashes) {
        final decoration = dash.decoration as BoxDecoration;
        expect(decoration.color, AppColors.divider);
      }
    });
  });
}
