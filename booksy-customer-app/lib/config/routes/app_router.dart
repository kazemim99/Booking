import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/constants/app_strings.dart';
import '../../core/di/injection.dart';
import '../../core/network/connectivity_service.dart';
import '../../core/widgets/widgets.dart';
import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/auth/presentation/bloc/auth_state.dart';
import '../../features/auth/presentation/pages/login_page.dart';
import '../../features/auth/presentation/pages/otp_verification_page.dart';
import '../../features/checkout/presentation/bloc/checkout_bloc.dart';
import '../../features/checkout/presentation/pages/checkout_page.dart';
import '../../features/auth/presentation/pages/splash_page.dart';
import '../../features/booking/presentation/pages/booking_flow_page.dart';
import '../../features/bookings/presentation/pages/appointment_detail_page.dart';
import '../../features/bookings/presentation/pages/appointments_page.dart';
import '../../features/home/presentation/pages/home_page.dart';
import '../../features/profile/presentation/pages/profile_tab_page.dart';
import '../../features/search/presentation/pages/explore_page.dart';
import '../../features/search/presentation/pages/map_discovery_page.dart';
import '../../features/search/presentation/pages/provider_detail_page.dart';

/// Route paths. All primary destinations are addressable (deep-linkable).
class Routes {
  Routes._();

  static const String splash = '/splash';
  static const String login = '/login';
  static const String otp = '/otp';

  static const String home = '/home';
  static const String explore = '/explore';

  /// Map + carousel discovery. Replaces the former `/explore/nearby` and
  /// `/explore/area` destinations: the map's own search field covers area
  /// search, and its floating action button covers "near me".
  static const String exploreMap = '/explore/map';
  static const String appointments = '/appointments';
  static const String profile = '/profile';

  /// Deposit checkout for a booking. Focused (outside the tab shell) and auth-required.
  static const String checkout = '/checkout';

  /// Location for the checkout of a specific booking.
  static String checkoutFor(String bookingId, String providerId) =>
      '$checkout/$bookingId?providerId=$providerId';

  static String providerDetail(String id) => '/providers/$id';
  static String bookingFlow(String providerId) => '/providers/$providerId/book';
  static String appointmentDetail(String id) => '/appointments/$id';
}

/// Latches the AuthBloc stream into router-friendly flags.
///
/// Transient states (AuthLoading during OTP send, OtpSentSuccess, AuthError)
/// deliberately do NOT change authentication status — only the three
/// terminal states do. `sessionResolved` flips once at cold start so the
/// app never bounces back to splash.
class AuthNotifier extends ChangeNotifier {
  bool _sessionResolved = false;
  bool _isAuthenticated = false;
  StreamSubscription<AuthState>? _sub;

  bool get sessionResolved => _sessionResolved;
  bool get isAuthenticated => _isAuthenticated;

  AuthNotifier(AuthBloc bloc) {
    apply(bloc.state);
    _sub = bloc.stream.listen(apply);
  }

  /// Test-only constructor: state is fed manually via [apply].
  @visibleForTesting
  AuthNotifier.detached();

  @visibleForTesting
  void apply(AuthState state) {
    if (state is Authenticated) {
      _sessionResolved = true;
      _isAuthenticated = true;
      notifyListeners();
    } else if (state is Unauthenticated || state is LoggedOut) {
      _sessionResolved = true;
      _isAuthenticated = false;
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}

class AppRouter {
  AppRouter._();

  /// Paths that demand authentication via redirect (return-to-intent).
  /// The appointments/profile *tabs* are not listed: they render their
  /// guest login prompt in place instead.
  static bool _requiresAuth(String location) {
    if (location.startsWith('${Routes.appointments}/')) return true;
    if (location.contains('/book/confirm')) return true;
    // Paying for a booking is inherently a signed-in action; return-to-intent brings the customer back here.
    if (location.startsWith('${Routes.checkout}/')) return true;
    return false;
  }

  /// Pure redirect decision — unit-tested directly.
  @visibleForTesting
  static String? redirectFor({
    required String location,
    required Uri uri,
    required bool sessionResolved,
    required bool isAuthenticated,
  }) {
    // Hold on splash until the stored session is restored — never
    // flash login/home prematurely.
    if (!sessionResolved) {
      return location == Routes.splash ? null : Routes.splash;
    }
    if (location == Routes.splash) return Routes.home;

    if (_requiresAuth(location) && !isAuthenticated) {
      final target = Uri.encodeComponent(uri.toString());
      return '${Routes.login}?redirect=$target';
    }

    // Leaving auth screens once logged in: honor return-to-intent.
    if (isAuthenticated &&
        (location == Routes.login || location == Routes.otp)) {
      final target = uri.queryParameters['redirect'];
      return target != null ? Uri.decodeComponent(target) : Routes.home;
    }

    return null;
  }

  static GoRouter create(AuthBloc authBloc) {
    final auth = AuthNotifier(authBloc);

    return GoRouter(
      initialLocation: Routes.splash,
      refreshListenable: auth,
      redirect: (context, state) => redirectFor(
        location: state.matchedLocation,
        uri: state.uri,
        sessionResolved: auth.sessionResolved,
        isAuthenticated: auth.isAuthenticated,
      ),
      routes: [
        GoRoute(
          path: Routes.splash,
          builder: (context, state) => const SplashPage(),
        ),
        GoRoute(
          path: Routes.login,
          builder: (context, state) => LoginPage(
            redirect: state.uri.queryParameters['redirect'],
          ),
        ),
        GoRoute(
          path: Routes.otp,
          builder: (context, state) => OtpVerificationPage(
            phoneNumber: state.uri.queryParameters['phone'] ?? '',
            redirect: state.uri.queryParameters['redirect'],
          ),
        ),
        // Focused checkout, outside the tab shell: paying is a single-purpose task, and the customer leaves for the
        // bank's browser page mid-way. The page itself asks the server what (if anything) is due.
        GoRoute(
          path: '${Routes.checkout}/:bookingId',
          builder: (context, state) => CheckoutPage(
            bookingId: state.pathParameters['bookingId']!,
            providerId: state.uri.queryParameters['providerId'] ?? '',
            // Resolved here (a fresh factory instance per navigation) so the page itself stays DI-free.
            bloc: getIt<CheckoutBloc>(),
          ),
        ),
        StatefulShellRoute.indexedStack(
          builder: (context, state, navigationShell) =>
              AppShell(navigationShell: navigationShell),
          branches: [
            StatefulShellBranch(routes: [
              GoRoute(
                path: Routes.home,
                builder: (context, state) => const HomePage(),
              ),
              GoRoute(
                path: '/providers/:id',
                builder: (context, state) => ProviderDetailPage(
                  providerId: state.pathParameters['id']!,
                ),
                routes: [
                  GoRoute(
                    path: 'book',
                    builder: (context, state) => BookingFlowPage(
                      providerId: state.pathParameters['id']!,
                    ),
                  ),
                ],
              ),
            ]),
            StatefulShellBranch(routes: [
              GoRoute(
                path: Routes.explore,
                builder: (context, state) => const ExplorePage(),
                routes: [
                  GoRoute(
                    path: 'map',
                    builder: (context, state) => const MapDiscoveryPage(),
                  ),
                ],
              ),
            ]),
            StatefulShellBranch(routes: [
              GoRoute(
                path: Routes.appointments,
                builder: (context, state) => const AppointmentsPage(),
                routes: [
                  GoRoute(
                    path: ':id',
                    builder: (context, state) => AppointmentDetailPage(
                      bookingId: state.pathParameters['id']!,
                    ),
                  ),
                ],
              ),
            ]),
            StatefulShellBranch(routes: [
              GoRoute(
                path: Routes.profile,
                builder: (context, state) => const ProfileTabPage(),
              ),
            ]),
          ],
        ),
      ],
    );
  }
}

/// The bottom-navigation shell. Each tab keeps its own stack; the Android
/// back gesture pops the tab stack first, falls back to the home tab from
/// other tab roots, and exits only from home's root.
class AppShell extends StatelessWidget {
  final StatefulNavigationShell navigationShell;

  const AppShell({super.key, required this.navigationShell});

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: navigationShell.currentIndex == 0,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) {
          navigationShell.goBranch(0);
        }
      },
      child: Scaffold(
        body: OfflineBanner(
          connectivity: getIt<ConnectivityService>(),
          child: navigationShell,
        ),
        bottomNavigationBar: AppBottomBar(
          activeIndex: navigationShell.currentIndex,
          onTap: (index) => navigationShell.goBranch(
            index,
            initialLocation: index == navigationShell.currentIndex,
          ),
          items: const [
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
            AppBottomBarItem(
              icon: Icons.calendar_today_outlined,
              selectedIcon: Icons.calendar_today,
              semanticLabel: AppStrings.tabAppointments,
            ),
            AppBottomBarItem(
              icon: Icons.person_outline,
              selectedIcon: Icons.person,
              semanticLabel: AppStrings.tabProfile,
            ),
          ],
        ),
      ),
    );
  }
}
