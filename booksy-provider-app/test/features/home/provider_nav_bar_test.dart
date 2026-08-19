import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/config/theme/app_tokens.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/widgets/app_bottom_bar.dart';
import 'package:booksy_provider_app/features/home/presentation/widgets/provider_nav_bar.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

/// The shared bottom navigation on the floating blue pill (spec:
/// DESIGN_LANGUAGE.md §5.11) with the mid-pill ⊕ create disc that replaced
/// the center-docked FAB.
void main() {
  late List<String> visited;

  Future<void> pump(
    WidgetTester tester, {
    NavTab active = NavTab.home,
    VoidCallback? onCreate,
    Key? createKey,
  }) async {
    visited = [];
    final router = GoRouter(
      initialLocation: '/host',
      routes: [
        GoRoute(
          path: '/host',
          builder: (context, state) => Scaffold(
            bottomNavigationBar: ProviderNavBar(
              active: active,
              onCreate: onCreate,
              createKey: createKey,
            ),
          ),
        ),
        for (final path in const [
          '/dashboard',
          '/calendar',
          '/clients',
          '/more',
        ])
          GoRoute(
            path: path,
            builder: (context, state) {
              visited.add(path);
              return const Scaffold();
            },
          ),
      ],
    );
    await tester.pumpWidget(MaterialApp.router(
      theme: AppTheme.light,
      routerConfig: router,
    ));
  }

  testWidgets('renders the four destinations on the blue pill',
      (tester) async {
    await pump(tester);
    expect(find.byType(AppBottomBar), findsOneWidget);
    expect(find.byIcon(Icons.home_outlined), findsOneWidget);
    expect(find.byIcon(Icons.calendar_month_outlined), findsOneWidget);
    expect(find.byIcon(Icons.people_outline), findsOneWidget);
    expect(find.byIcon(Icons.more_horiz), findsOneWidget);
  });

  testWidgets('without onCreate there is no ⊕ disc', (tester) async {
    await pump(tester);
    expect(find.byIcon(Icons.add), findsNothing);
  });

  testWidgets('onCreate shows the white ⊕ disc under the given key and fires',
      (tester) async {
    var created = false;
    const key = Key('home-create-action');
    await pump(
      tester,
      onCreate: () => created = true,
      createKey: key,
    );
    expect(find.byKey(key), findsOneWidget);
    final disc = tester.widget<Material>(find.byKey(key));
    expect(disc.color, Colors.white);
    final icon = tester.widget<Icon>(find.byIcon(Icons.add));
    expect(icon.color, AppColors.primary);
    expect(
      tester.widget<Tooltip>(find.byType(Tooltip)).message,
      AppStrings.homeCreateTitle,
    );

    await tester.tap(find.byKey(key));
    expect(created, isTrue);
  });

  testWidgets('tapping an inactive destination routes to it', (tester) async {
    await pump(tester, active: NavTab.home);
    await tester.tap(find.byIcon(Icons.calendar_month_outlined));
    await tester.pumpAndSettle();
    expect(visited, ['/calendar']);
  });

  testWidgets('the active destination does not navigate', (tester) async {
    await pump(tester, active: NavTab.home);
    await tester.tap(find.byIcon(Icons.home_outlined));
    await tester.pumpAndSettle();
    expect(visited, isEmpty);
  });
}
