import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/domain/entities/provider_status.dart';
import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/auth/presentation/bloc/auth_state.dart';
import '../../features/auth/presentation/pages/account_blocked_page.dart';
import '../../features/auth/presentation/pages/onboarding_required_page.dart';
import '../../features/auth/presentation/pages/otp_verification_page.dart';
import '../../features/auth/presentation/pages/provider_login_page.dart';
import '../../features/auth/presentation/pages/provider_dashboard_page.dart';
import '../../features/auth/presentation/pages/splash_page.dart';

/// Route paths.
class Routes {
  Routes._();
  static const String splash = '/splash';
  static const String login = '/login';
  static const String otp = '/otp';
  static const String dashboard = '/dashboard';
  static const String onboarding = '/onboarding';
  static const String blocked = '/blocked';
}

/// Resolved auth flow status for routing.
enum AuthFlowStatus { unresolved, unauthenticated, needsOnboarding, blocked, authenticated }

/// Latches [AuthBloc] state into router-friendly flags.
class AuthNotifier extends ChangeNotifier {
  AuthFlowStatus _status = AuthFlowStatus.unresolved;
  ProviderStatus? _blockedStatus;
  StreamSubscription<AuthState>? _sub;

  AuthFlowStatus get status => _status;
  ProviderStatus? get blockedStatus => _blockedStatus;
  bool get sessionResolved => _status != AuthFlowStatus.unresolved;

  AuthNotifier(AuthBloc bloc) {
    apply(bloc.state);
    _sub = bloc.stream.listen(apply);
  }

  @visibleForTesting
  AuthNotifier.detached();

  @visibleForTesting
  void apply(AuthState state) {
    final previous = _status;
    final previousBlocked = _blockedStatus;

    if (state is Authenticated) {
      _status = AuthFlowStatus.authenticated;
      _blockedStatus = null;
    } else if (state is NeedsOnboarding) {
      _status = AuthFlowStatus.needsOnboarding;
      _blockedStatus = null;
    } else if (state is AccountBlocked) {
      _status = AuthFlowStatus.blocked;
      _blockedStatus = state.status;
    } else if (state is Unauthenticated || state is LoggedOut) {
      _status = AuthFlowStatus.unauthenticated;
      _blockedStatus = null;
    } else {
      // AuthInitial / AuthLoading / OtpSent / OtpResent / AuthError are
      // transient and must NOT change the resolved status.
      return;
    }

    if (_status != previous || _blockedStatus != previousBlocked) {
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

  static bool _isAuthScreen(String location) =>
      location == Routes.login || location == Routes.otp;

  /// Pure redirect decision — unit-tested directly.
  @visibleForTesting
  static String? redirectFor({
    required String location,
    required Uri uri,
    required AuthFlowStatus status,
  }) {
    // Hold on splash until the stored session resolves.
    if (status == AuthFlowStatus.unresolved) {
      return location == Routes.splash ? null : Routes.splash;
    }

    switch (status) {
      case AuthFlowStatus.unauthenticated:
        if (_isAuthScreen(location)) return null;
        // Bounce to login, preserving return-to-intent for non-auth targets.
        if (location == Routes.splash) return Routes.login;
        final target = Uri.encodeComponent(uri.toString());
        return '${Routes.login}?redirect=$target';

      case AuthFlowStatus.blocked:
        return location == Routes.blocked ? null : Routes.blocked;

      case AuthFlowStatus.needsOnboarding:
        return location == Routes.onboarding ? null : Routes.onboarding;

      case AuthFlowStatus.authenticated:
        if (location == Routes.splash ||
            location == Routes.onboarding ||
            location == Routes.blocked) {
          return Routes.dashboard;
        }
        if (_isAuthScreen(location)) {
          final target = uri.queryParameters['redirect'];
          return (target != null && target.isNotEmpty)
              ? Uri.decodeComponent(target)
              : Routes.dashboard;
        }
        return null;

      case AuthFlowStatus.unresolved:
        return Routes.splash;
    }
  }

  static GoRouter create(AuthBloc authBloc) {
    final auth = AuthNotifier(authBloc);
    return GoRouter(
      initialLocation: Routes.splash,
      refreshListenable: auth,
      redirect: (context, state) => redirectFor(
        location: state.matchedLocation,
        uri: state.uri,
        status: auth.status,
      ),
      routes: [
        GoRoute(
          path: Routes.splash,
          builder: (_, _) => const SplashPage(),
        ),
        GoRoute(
          path: Routes.login,
          builder: (_, state) => ProviderLoginPage(
            redirect: state.uri.queryParameters['redirect'],
          ),
        ),
        GoRoute(
          path: Routes.otp,
          builder: (_, state) => OtpVerificationPage(
            phoneNumber: state.uri.queryParameters['phone'] ?? '',
            redirect: state.uri.queryParameters['redirect'],
          ),
        ),
        GoRoute(
          path: Routes.dashboard,
          builder: (_, _) => const ProviderDashboardPage(),
        ),
        GoRoute(
          path: Routes.onboarding,
          builder: (_, _) => const OnboardingRequiredPage(),
        ),
        GoRoute(
          path: Routes.blocked,
          builder: (_, _) =>
              AccountBlockedPage(status: auth.blockedStatus),
        ),
      ],
    );
  }
}
