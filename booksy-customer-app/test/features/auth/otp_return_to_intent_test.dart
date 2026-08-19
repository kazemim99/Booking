import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/auth/domain/entities/user.dart';
import 'package:booksy_customer_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:booksy_customer_app/features/auth/domain/usecases/complete_authentication_usecase.dart';
import 'package:booksy_customer_app/features/auth/domain/usecases/send_verification_code_usecase.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:dartz/dartz.dart';
import 'package:booksy_customer_app/features/auth/presentation/pages/otp_verification_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

/// Regression cover for the point-of-need login dead end.
///
/// A customer who picked a service and a time, then signed in to confirm, saw «ورود موفق» and was left sitting
/// on the OTP screen. Nothing moved. That is the single worst place in the journey to strand someone — the
/// booking is fully chosen and one tap from done.
///
/// The cause was an integration gap, not a logic error. `AppRouter.redirectFor` is correct and was well
/// covered, but only as a *pure function*. In the real app the auth screens are reached with `context.push`
/// (`/book/...` → push `/login?redirect=…` → push `/otp?phone=…&redirect=…`), and GoRouter's top-level
/// `redirect` governs route matching rather than re-driving an imperatively pushed page when
/// `refreshListenable` fires. So the unit tests passed while the journey was broken.
///
/// These tests therefore drive a REAL `GoRouter` through the real push chain, which is the only level at which
/// this defect is visible.
/// The page only ever reads state, so the repository never has to do anything — but `AuthBloc` is concrete and
/// `BlocProvider<AuthBloc>` demands the real type, hence a real bloc over an inert repository.
class _InertAuthRepository implements AuthRepository {
  @override
  Future<Either<Failure, String>> sendVerificationCode({
    required String phoneNumber,
    String countryCode = '+98',
  }) async =>
      const Right('sent');

  @override
  Future<Either<Failure, AuthSession>> completeAuthentication({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  }) async =>
      Right(_session);

  @override
  Future<Either<Failure, String>> resendOtp({required String phoneNumber}) async =>
      const Right('resent');

  @override
  Future<Either<Failure, AuthSession>> refreshToken() async => Right(_session);

  @override
  Future<Either<Failure, void>> logout() async => const Right(null);

  @override
  Future<bool> isLoggedIn() async => false;

  @override
  Future<Either<Failure, AuthSession?>> getCurrentSession() async =>
      const Right(null);
}

final _session = AuthSession(
  accessToken: 'token',
  refreshToken: 'refresh',
  user: User(
    id: 'user-1',
    phoneNumber: '+989121234567',
    createdAt: DateTime(2026, 1, 1),
  ),
  expiresIn: 3600,
);

class _FakeAuthBloc extends AuthBloc {
  _FakeAuthBloc._(AuthRepository repo)
      : super(
          SendVerificationCodeUseCase(repo),
          CompleteAuthenticationUseCase(repo),
          repo,
        );

  factory _FakeAuthBloc() => _FakeAuthBloc._(_InertAuthRepository());

  /// Drives the exact transition the real flow produces on a correct OTP.
  void emitAuthenticated() => emit(Authenticated(_session));
}

/// `pumpAndSettle` cannot be used on this screen: the resend countdown is a `Timer.periodic`, so the frame
/// scheduler never goes idle and the pump times out. Pump a bounded number of frames instead.
Future<void> settle(WidgetTester tester) async {
  // Enough frames to carry a route transition to completion, without ever waiting for idle.
  for (var i = 0; i < 6; i++) {
    await tester.pump(const Duration(milliseconds: 200));
  }
}

void main() {
  late _FakeAuthBloc authBloc;

  setUp(() => authBloc = _FakeAuthBloc());
  tearDown(() => authBloc.close());

  /// Builds a router whose auth screens are reached by `push`, mirroring the real booking flow.
  GoRouter buildRouter() => GoRouter(
        initialLocation: '/book/provider-1',
        routes: [
          GoRoute(
            path: '/book/:providerId',
            builder: (context, state) => Scaffold(
              body: Center(
                child: ElevatedButton(
                  key: const Key('confirm'),
                  onPressed: () {
                    final target = Uri.encodeComponent('/book/provider-1');
                    context.push('/otp?phone=%2B989121234567&redirect=$target');
                  },
                  child: const Text('confirm'),
                ),
              ),
            ),
          ),
          GoRoute(
            path: '/otp',
            builder: (context, state) => OtpVerificationPage(
              phoneNumber: state.uri.queryParameters['phone'] ?? '',
              redirect: state.uri.queryParameters['redirect'],
            ),
          ),
          GoRoute(
            path: '/home',
            builder: (context, state) =>
                const Scaffold(body: Center(child: Text('HOME'))),
          ),
        ],
      );

  Future<void> pumpTo(WidgetTester tester, GoRouter router) async {
    await tester.pumpWidget(
      BlocProvider<AuthBloc>.value(
        value: authBloc as dynamic,
        child: MaterialApp.router(
          routerConfig: router,
          locale: const Locale('fa', 'IR'),
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child!,
          ),
        ),
      ),
    );
    await settle(tester);
  }

  testWidgets('returns to the booking it interrupted once the OTP is accepted',
      (tester) async {
    final router = buildRouter();
    await pumpTo(tester, router);

    // Reach OTP the way the booking flow does: by pushing.
    //
    // Assertions are on what is RENDERED, not on `currentConfiguration.uri`: an imperative `push` records an
    // ImperativeRouteMatch and leaves that uri at the base location, so it would report `/book/provider-1`
    // even while the OTP screen is on screen — and would have made this test pass against the bug.
    await tester.tap(find.byKey(const Key('confirm')));
    await settle(tester);
    expect(
      find.byType(OtpVerificationPage),
      findsOneWidget,
      reason: 'precondition: the customer is on the OTP screen',
    );

    authBloc.emitAuthenticated();
    await settle(tester);

    expect(
      find.byType(OtpVerificationPage),
      findsNothing,
      reason: 'the customer must not be stranded on the OTP screen after a correct code',
    );
    expect(
      find.byKey(const Key('confirm')),
      findsOneWidget,
      reason: 'they must land back on the booking they were completing',
    );
  });

  testWidgets('does not leave the OTP screen on the stack behind the destination',
      (tester) async {
    final router = buildRouter();
    await pumpTo(tester, router);

    await tester.tap(find.byKey(const Key('confirm')));
    await settle(tester);

    expect(find.byType(OtpVerificationPage), findsOneWidget);

    authBloc.emitAuthenticated();
    await settle(tester);

    // `go`, not `push`: pressing back must not walk a signed-in customer into the OTP screen again.
    expect(
      router.routerDelegate.currentConfiguration.matches
          .whereType<ImperativeRouteMatch>()
          .isEmpty,
      isTrue,
      reason: 'the pushed auth screens must be replaced, not left stacked under the destination',
    );
  });

  testWidgets('falls back to home when there is no return-to-intent target',
      (tester) async {
    final router = GoRouter(
      initialLocation: '/otp?phone=%2B989121234567',
      routes: [
        GoRoute(
          path: '/otp',
          builder: (context, state) => OtpVerificationPage(
            phoneNumber: state.uri.queryParameters['phone'] ?? '',
            redirect: state.uri.queryParameters['redirect'],
          ),
        ),
        GoRoute(
          path: '/home',
          builder: (context, state) =>
              const Scaffold(body: Center(child: Text('HOME'))),
        ),
      ],
    );
    await pumpTo(tester, router);

    authBloc.emitAuthenticated();
    await settle(tester);

    expect(find.text('HOME'), findsOneWidget,
        reason: 'signing in from a bare /otp should still move the customer somewhere');
  });
}
