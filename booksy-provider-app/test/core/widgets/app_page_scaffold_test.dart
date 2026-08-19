import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/config/theme/app_tokens.dart';
import 'package:booksy_provider_app/core/widgets/app_page_scaffold.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The standard screen anatomy (DESIGN_LANGUAGE.md §1, §4.1): blue chrome
/// carrying the title, white sheet with a rounded top edge carrying content.
/// Guards against pages drifting back to hand-rolled white app bars.
void main() {
  Future<void> pump(WidgetTester tester, Widget child) {
    return tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(textDirection: TextDirection.rtl, child: child),
      ),
    );
  }

  testWidgets('paints blue chrome behind a white sheet', (tester) async {
    await pump(
      tester,
      const AppPageScaffold(title: 'خدمات', body: SizedBox()),
    );

    final scaffold = tester.widget<Scaffold>(find.byType(Scaffold));
    expect(scaffold.backgroundColor, AppColors.appBar);

    final sheet = tester.widget<Material>(find.descendant(
      of: find.byType(AppPageScaffold),
      matching: find.byWidgetPredicate(
        (w) => w is Material && w.color == Colors.white,
      ),
    ));
    expect(sheet.borderRadius,
        const BorderRadius.vertical(top: Radius.circular(AppRadius.panel)));
  });

  testWidgets('title is white 17/w700 so it reads on the chrome',
      (tester) async {
    await pump(
      tester,
      const AppPageScaffold(title: 'خدمات', body: SizedBox()),
    );

    final title = tester.widget<Text>(find.text('خدمات'));
    expect(title.style?.color, Colors.white);
    expect(title.style?.fontSize, 17);
    expect(title.style?.fontWeight, FontWeight.w700);
  });

  testWidgets('titleWidget replaces the plain title', (tester) async {
    await pump(
      tester,
      const AppPageScaffold(
        titleWidget: Text('سفارشی', key: Key('custom-title')),
        body: SizedBox(),
      ),
    );

    expect(find.byKey(const Key('custom-title')), findsOneWidget);
  });

  testWidgets('the sheet is a Material so list ink still paints',
      (tester) async {
    // Regression: a coloured Container swallowed every ListTile ripple.
    await pump(
      tester,
      const AppPageScaffold(
        title: 'عنوان',
        body: ListTile(key: Key('row'), title: Text('ردیف')),
      ),
    );

    expect(tester.takeException(), isNull);
    expect(find.byKey(const Key('row')), findsOneWidget);
  });

  testWidgets('chromeFooter renders above the sheet, on the blue',
      (tester) async {
    await pump(
      tester,
      const AppPageScaffold(
        title: 'تقویم',
        chromeFooter: SizedBox(height: 40, key: Key('week-strip')),
        body: SizedBox(key: Key('body')),
      ),
    );

    final footerY = tester.getTopLeft(find.byKey(const Key('week-strip'))).dy;
    final sheetY = tester
        .getTopLeft(find.byWidgetPredicate(
          (w) => w is Material && w.color == Colors.white,
        ))
        .dy;
    expect(footerY, lessThan(sheetY),
        reason: 'the chrome footer sits above the sheet edge');
  });

  testWidgets('passes through nav bar, FAB and actions', (tester) async {
    await pump(
      tester,
      AppPageScaffold(
        title: 'عنوان',
        actions: [
          IconButton(
            key: const Key('add'),
            icon: const Icon(Icons.add_circle, color: AppColors.success),
            onPressed: () {},
          ),
        ],
        bottomNavigationBar: const SizedBox(key: Key('nav'), height: 60),
        floatingActionButton: const SizedBox(key: Key('fab')),
        body: const SizedBox(),
      ),
    );

    expect(find.byKey(const Key('add')), findsOneWidget);
    expect(find.byKey(const Key('nav')), findsOneWidget);
    expect(find.byKey(const Key('fab')), findsOneWidget);
  });
}
