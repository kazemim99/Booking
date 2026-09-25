import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/widgets/app_viewport_frame.dart';
import 'package:booksy_customer_app/main.dart' show buildAppShell;
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// On a desktop browser the app is a phone-shaped column in the middle of the window, so screens designed for a
/// phone (the nav pill, full-width buttons, bottom sheets) do not stretch across 1440 px. Phones are untouched.
void main() {
  Future<void> setViewport(WidgetTester tester, Size size) async {
    tester.view.physicalSize = size;
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
  }

  /// The app as main.dart composes it: MaterialApp with the shell builder, a home page that records what it sees.
  Widget app({required void Function(Size mediaSize, BoxConstraints constraints) onBuild}) => MaterialApp(
        theme: AppTheme.light,
        builder: buildAppShell,
        home: Scaffold(
          body: Builder(
            builder: (context) => LayoutBuilder(builder: (context, constraints) {
              onBuild(MediaQuery.sizeOf(context), constraints);
              return const SizedBox.expand(key: Key('page'));
            }),
          ),
        ),
      );

  group('wide viewport (desktop browser)', () {
    testWidgets('renders the app in a centred column of bounded width', (tester) async {
      await setViewport(tester, const Size(1440, 900));
      late Size media;
      late BoxConstraints constraints;
      await tester.pumpWidget(app(onBuild: (m, c) {
        media = m;
        constraints = c;
      }));

      const w = AppViewportFrame.maxContentWidth;
      final page = tester.getRect(find.byKey(const Key('page')));
      expect(page.width, w);
      expect(page.height, 900);
      expect(page.left, (1440 - w) / 2, reason: 'the column is centred');

      // Layout code that reads MediaQuery (nav pill, dialogs, sheets) sizes to the column, not the window.
      expect(media, const Size(w, 900));
      expect(constraints.maxWidth, w);
    });

    testWidgets('dialogs and bottom sheets open inside the column', (tester) async {
      await setViewport(tester, const Size(1440, 900));
      await tester.pumpWidget(app(onBuild: (_, __) {}));
      final context = tester.element(find.byKey(const Key('page')));

      showModalBottomSheet<void>(
        context: context,
        builder: (_) => const SizedBox(key: Key('sheet'), height: 200, width: double.infinity),
      );
      await tester.pumpAndSettle();
      final sheet = tester.getRect(find.byKey(const Key('sheet')));
      const left = (1440 - AppViewportFrame.maxContentWidth) / 2;
      expect(sheet.left, greaterThanOrEqualTo(left));
      expect(sheet.right, lessThanOrEqualTo(left + AppViewportFrame.maxContentWidth));
    });

    testWidgets('paints a quiet backdrop outside the column and keeps the app right-to-left', (tester) async {
      await setViewport(tester, const Size(1440, 900));
      await tester.pumpWidget(app(onBuild: (_, __) {}));

      final backdrop = tester.widget<ColoredBox>(find.byKey(AppViewportFrame.backdropKey));
      final theme = Theme.of(tester.element(find.byKey(const Key('page'))));
      expect(backdrop.color, isNot(theme.scaffoldBackgroundColor), reason: 'the column edge must be visible');
      expect(Directionality.of(tester.element(find.byKey(const Key('page')))), TextDirection.rtl);
    });
  });

  group('phone viewport', () {
    for (final size in const [Size(360, 640), Size(390, 844)]) {
      testWidgets('${size.width.toInt()}x${size.height.toInt()} is unaffected: full width, no backdrop, RTL',
          (tester) async {
        await setViewport(tester, size);
        late Size media;
        await tester.pumpWidget(app(onBuild: (m, _) => media = m));

        expect(tester.getRect(find.byKey(const Key('page'))), Offset.zero & size);
        expect(media, size);
        expect(find.byKey(AppViewportFrame.backdropKey), findsNothing);
        expect(Directionality.of(tester.element(find.byKey(const Key('page')))), TextDirection.rtl);
      });
    }
  });

  /// The frame as it runs on a given platform: [isWeb] stands in for kIsWeb, which is always false under test.
  Widget framed({required bool isWeb, required void Function(Size mediaSize) onBuild}) => MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => Directionality(
          textDirection: TextDirection.rtl,
          child: AppViewportFrame(isWeb: isWeb, child: child!),
        ),
        home: Scaffold(
          body: Builder(builder: (context) {
            onBuild(MediaQuery.sizeOf(context));
            return const SizedBox.expand(key: Key('page'));
          }),
        ),
      );

  group('platform: the column is for browsers and large screens, never for a phone', () {
    testWidgets('web, wide window: a centred column', (tester) async {
      await setViewport(tester, const Size(1440, 900));
      late Size media;
      await tester.pumpWidget(framed(isWeb: true, onBuild: (m) => media = m));
      expect(find.byKey(AppViewportFrame.backdropKey), findsOneWidget);
      expect(media.width, AppViewportFrame.maxContentWidth);
    });

    testWidgets('web, a phone browser turned to landscape: a centred column', (tester) async {
      await setViewport(tester, const Size(844, 390));
      late Size media;
      await tester.pumpWidget(framed(isWeb: true, onBuild: (m) => media = m));
      expect(find.byKey(AppViewportFrame.backdropKey), findsOneWidget);
      expect(media.width, AppViewportFrame.maxContentWidth);
    });

    for (final size in const [Size(844, 390), Size(640, 360), Size(915, 412)]) {
      testWidgets('native phone in landscape ${size.width.toInt()}x${size.height.toInt()}: full width, no backdrop',
          (tester) async {
        await setViewport(tester, size);
        late Size media;
        await tester.pumpWidget(framed(isWeb: false, onBuild: (m) => media = m));
        expect(find.byKey(AppViewportFrame.backdropKey), findsNothing);
        expect(tester.getRect(find.byKey(const Key('page'))), Offset.zero & size);
        expect(media, size);
      });
    }

    testWidgets('native phone in portrait: full width, no backdrop', (tester) async {
      await setViewport(tester, const Size(390, 844));
      await tester.pumpWidget(framed(isWeb: false, onBuild: (_) {}));
      expect(find.byKey(AppViewportFrame.backdropKey), findsNothing);
    });

    for (final size in const [Size(1024, 768), Size(768, 1024)]) {
      testWidgets('native tablet ${size.width.toInt()}x${size.height.toInt()}: a centred column', (tester) async {
        await setViewport(tester, size);
        late Size media;
        await tester.pumpWidget(framed(isWeb: false, onBuild: (m) => media = m));
        expect(find.byKey(AppViewportFrame.backdropKey), findsOneWidget);
        expect(media, Size(AppViewportFrame.maxContentWidth, size.height));
      });
    }

    testWidgets('a large screen no wider than the column is left alone', (tester) async {
      // A tablet in split screen can be tall and narrow: shortestSide below 600, nothing to centre anyway.
      await setViewport(tester, const Size(AppViewportFrame.maxContentWidth, 1000));
      await tester.pumpWidget(framed(isWeb: false, onBuild: (_) {}));
      expect(find.byKey(AppViewportFrame.backdropKey), findsNothing);
    });
  });

  testWidgets('a viewport exactly the column width is treated as a phone', (tester) async {
    await setViewport(tester, const Size(AppViewportFrame.maxContentWidth, 800));
    await tester.pumpWidget(app(onBuild: (_, __) {}));
    expect(find.byKey(AppViewportFrame.backdropKey), findsNothing);
  });
}
