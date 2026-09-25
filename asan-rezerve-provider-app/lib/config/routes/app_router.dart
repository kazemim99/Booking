import 'package:flutter_bloc/flutter_bloc.dart';
import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/domain/entities/provider_status.dart';
import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/auth/presentation/bloc/auth_state.dart';
import '../../features/auth/presentation/pages/account_blocked_page.dart';
import '../../features/auth/presentation/pages/complete_name_page.dart';
import '../../features/auth/presentation/pages/otp_verification_page.dart';
import '../../features/onboarding/presentation/pages/onboarding_wizard_page.dart';
import '../../features/auth/presentation/pages/provider_login_page.dart';
import '../../features/auth/presentation/pages/splash_page.dart';
import '../../features/home/presentation/pages/booking_composer_page.dart';
import '../../features/home/presentation/pages/calendar_page.dart';
import '../../features/home/presentation/pages/clients_page.dart';
import '../../features/home/presentation/pages/gallery_page.dart';
import '../../features/home/presentation/pages/more_page.dart';
import '../../features/home/presentation/pages/more_sub_pages.dart';
import '../../features/invitations/presentation/accept_invitation_page.dart';
import '../../features/invitations/presentation/register_and_accept_page.dart';
import '../../core/di/injection.dart';
import '../../features/home/presentation/pages/home_page.dart';
import '../../features/notifications/presentation/inbox_cubit.dart';
import '../../features/notifications/presentation/inbox_page.dart';
import '../../features/reviews/presentation/reviews_cubit.dart';
import '../../features/reviews/presentation/reviews_page.dart';
import '../../core/push/push_open_route.dart';

/// Route paths.
class Routes {
  Routes._();
  static const String splash = '/splash';
  static const String login = '/login';
  static const String otp = '/otp';
  static const String dashboard = '/dashboard';
  static const String calendar = '/calendar';

  /// The calendar opened on one booking (a tapped notification lands here).
  static String calendarBooking(String bookingId) =>
      Uri(path: calendar, queryParameters: {'booking': bookingId}).toString();
  static const String clients = '/clients';
  static const String more = '/more';
  static const String notifications = '/notifications';
  static const String reviews = '/reviews';
  static const String moreBusiness = '/more/business';
  static const String moreHours = '/more/hours';
  static const String moreHolidays = '/more/holidays';
  static const String moreGallery = '/more/gallery';
  static const String moreInsights = '/more/insights';
  static const String moreServices = '/more/services';
  static const String moreStaff = '/more/staff';
  static const String moreMemberships = '/more/memberships';
  static const String acceptInvitation = '/invite'; // + /:invitationId
  static const String newBooking = '/booking/new';

  /// Composer route pre-filled with a client's identity (book-again).
  static String newBookingFor({
    required String client,
    required String phone,
    String? customerId,
  }) =>
      Uri(path: newBooking, queryParameters: {
        if (client.isNotEmpty) 'client': client,
        if (phone.isNotEmpty) 'phone': phone,
        'customer': ?customerId,
      }).toString();

  /// Composer route pre-set to [date] (spec: calendar-initiated creation).
  static String newBookingOn(DateTime date) =>
      '$newBooking?date=${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
  static const String onboarding = '/onboarding';

  /// Where a tapped notification opens the app, with the push's data as query parameters
  /// (web/push/firebase-messaging-sw.js builds it). Never shown: the redirect sends it on to [pushOpenRoute].
  static const String pushOpen = '/push-open';
  static const String blocked = '/blocked';

  /// Asked once, right after OTP, when the account has no real name.
  static const String completeName = '/profile/name';

  /// [completeName], then on to [target].
  static String completeNameThen(String target) =>
      '$completeName?redirect=${Uri.encodeComponent(target)}';
}

/// Resolved auth flow status for routing.
enum AuthFlowStatus { unresolved, unauthenticated, needsOnboarding, blocked, authenticated }

/// Latches [AuthBloc] state into router-friendly flags.
class AuthNotifier extends ChangeNotifier {
  AuthFlowStatus _status = AuthFlowStatus.unresolved;
  ProviderStatus? _blockedStatus;
  String? _phoneNumber;
  bool _nameMissing = false;
  StreamSubscription<AuthState>? _sub;

  AuthFlowStatus get status => _status;
  ProviderStatus? get blockedStatus => _blockedStatus;

  /// Owner phone from the resolved session (pre-fills onboarding step 1).
  String? get phoneNumber => _phoneNumber;
  bool get sessionResolved => _status != AuthFlowStatus.unresolved;

  /// The signed-in account has no real name yet — only the sign-in placeholder, or nothing.
  bool get nameMissing => _nameMissing;

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
      _phoneNumber = state.session.user.phoneNumber;
      _nameMissing = state.session.user.realName == null;
    } else if (state is NeedsOnboarding) {
      _status = AuthFlowStatus.needsOnboarding;
      _blockedStatus = null;
      _phoneNumber = state.session.user.phoneNumber;
    } else if (state is AccountBlocked) {
      _status = AuthFlowStatus.blocked;
      _blockedStatus = state.status;
      _phoneNumber = state.session.user.phoneNumber;
    } else if (state is Unauthenticated || state is LoggedOut) {
      _status = AuthFlowStatus.unauthenticated;
      _blockedStatus = null;
      _phoneNumber = null;
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
    bool nameMissing = false,
  }) {
    // Hold on splash until the stored session resolves.
    if (status == AuthFlowStatus.unresolved) {
      if (location == Routes.splash) return null;
      // A tapped notification is the one cold start that must keep its target through splash: it is the only way a
      // closed app is opened on a booking. Other deep links still start where they always have.
      if (location == Routes.pushOpen) return '${Routes.splash}?redirect=${Uri.encodeComponent(uri.toString())}';
      return Routes.splash;
    }

    // Set only by a tapped notification that waited on splash (above).
    final waiting = location == Routes.splash ? uri.queryParameters['redirect'] : null;
    final hasWaiting = waiting != null && waiting.isNotEmpty;

    switch (status) {
      case AuthFlowStatus.unauthenticated:
        if (_isAuthScreen(location)) return null;
        // Bounce to login, preserving return-to-intent for non-auth targets.
        if (location == Routes.splash) {
          return hasWaiting ? '${Routes.login}?redirect=${Uri.encodeComponent(waiting)}' : Routes.login;
        }
        final target = Uri.encodeComponent(uri.toString());
        return '${Routes.login}?redirect=$target';

      case AuthFlowStatus.blocked:
        return location == Routes.blocked ? null : Routes.blocked;

      case AuthFlowStatus.needsOnboarding:
        return location == Routes.onboarding ? null : Routes.onboarding;

      case AuthFlowStatus.authenticated:
        if (hasWaiting) return waiting;
        if (location == Routes.splash ||
            location == Routes.onboarding ||
            location == Routes.blocked) {
          return Routes.dashboard;
        }
        // The same mapping a tapped push uses on Android.
        if (location == Routes.pushOpen) return pushOpenRoute(uri.queryParameters);
        if (_isAuthScreen(location)) {
          final target = uri.queryParameters['redirect'];
          final destination = (target != null && target.isNotEmpty)
              ? Uri.decodeComponent(target)
              : Routes.dashboard;
          // Phone sign-in leaves «ارائه‌دهنده <digits>» as the name, and nothing ever asked for a real one — so
          // the app showed the owner as their number (production QA 2026-09-23). Asked here, once: only on the
          // way out of the auth screens, never at a cold start or mid-app, and a skip is not undone.
          return nameMissing ? Routes.completeNameThen(destination) : destination;
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
        nameMissing: auth.nameMissing,
      ),
      routes: [
        GoRoute(
          path: Routes.splash,
          builder: (_, _) => const SplashPage(),
        ),
        // Matched so a tapped notification's address is a known location; [redirectFor] moves it on.
        GoRoute(
          path: Routes.pushOpen,
          redirect: (_, state) => pushOpenRoute(state.uri.queryParameters),
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
          // The adaptive Home ("Today" workspace). The retired placeholder
          // (ProviderDashboardPage) is kept one release for trivial rollback.
          builder: (_, _) => const HomePage(),
        ),
        GoRoute(
          path: Routes.calendar,
          builder: (_, state) =>
              CalendarPage(focusBookingId: state.uri.queryParameters['booking']),
        ),
        GoRoute(
          path: Routes.clients,
          builder: (_, _) => const ClientsPage(),
        ),
        GoRoute(
          path: Routes.more,
          builder: (_, _) => const MorePage(),
        ),
        GoRoute(
          path: Routes.notifications,
          // The same cubit the bell reads, so opening the list and returning leaves the badge in agreement.
          builder: (_, _) => BlocProvider<InboxCubit>.value(
            value: getIt<InboxCubit>(),
            child: const InboxPage(),
          ),
        ),
        GoRoute(
          path: Routes.reviews,
          builder: (_, _) => BlocProvider<ReviewsCubit>(
            create: (_) => getIt<ReviewsCubit>()..load(),
            child: const ReviewsPage(),
          ),
        ),
        GoRoute(
          path: Routes.moreBusiness,
          builder: (_, _) => const BusinessProfilePage(),
        ),
        GoRoute(
          path: Routes.moreHours,
          builder: (_, _) => const BusinessHoursPage(),
        ),
        GoRoute(
          path: Routes.moreHolidays,
          builder: (_, _) => const HolidaysPage(),
        ),
        GoRoute(
          path: Routes.moreGallery,
          builder: (_, _) => const GalleryPage(),
        ),
        GoRoute(
          path: Routes.moreInsights,
          builder: (_, _) => const InsightsPage(),
        ),
        GoRoute(
          path: Routes.moreServices,
          builder: (_, _) => const ServicesPage(),
        ),
        GoRoute(
          path: Routes.moreStaff,
          builder: (_, _) => const StaffPage(),
        ),
        GoRoute(
          path: Routes.moreMemberships,
          builder: (_, _) => const MyMembershipsPage(),
        ),
        GoRoute(
          path: '${Routes.acceptInvitation}/:invitationId',
          builder: (_, state) => AcceptInvitationPage(
            invitationId: state.pathParameters['invitationId'] ?? '',
          ),
        ),
        GoRoute(
          path: '${Routes.acceptInvitation}/:invitationId/register',
          builder: (_, state) => RegisterAndAcceptPage(
            invitationId: state.pathParameters['invitationId'] ?? '',
          ),
        ),
        GoRoute(
          path: Routes.newBooking,
          builder: (_, state) => BookingComposerPage(
            initialDate:
                DateTime.tryParse(state.uri.queryParameters['date'] ?? ''),
            initialClientName: state.uri.queryParameters['client'],
            initialClientPhone: state.uri.queryParameters['phone'],
            initialCustomerId: state.uri.queryParameters['customer'],
          ),
        ),
        GoRoute(
          path: Routes.onboarding,
          builder: (_, _) =>
              OnboardingWizardPage(phoneNumber: auth.phoneNumber),
        ),
        GoRoute(
          path: Routes.completeName,
          builder: (_, state) =>
              CompleteNamePage(redirect: state.uri.queryParameters['redirect']),
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
