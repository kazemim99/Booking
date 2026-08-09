import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/feature_flags.dart';
import 'package:booksy_customer_app/config/routes/app_router.dart';
import 'package:booksy_customer_app/features/auth/domain/entities/user.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_state.dart';

void main() {
  group('AppRouter.redirectFor', () {
    test('holds every location on splash until session resolves', () {
      for (final loc in ['/home', '/login', '/appointments/42']) {
        expect(
          AppRouter.redirectFor(
            location: loc,
            uri: Uri.parse(loc),
            sessionResolved: false,
            isAuthenticated: false,
          ),
          Routes.splash,
        );
      }
      expect(
        AppRouter.redirectFor(
          location: Routes.splash,
          uri: Uri.parse(Routes.splash),
          sessionResolved: false,
          isAuthenticated: false,
        ),
        isNull,
      );
    });

    test('leaves splash for home once resolved (guest and authenticated)', () {
      for (final authed in [true, false]) {
        expect(
          AppRouter.redirectFor(
            location: Routes.splash,
            uri: Uri.parse(Routes.splash),
            sessionResolved: true,
            isAuthenticated: authed,
          ),
          Routes.home,
        );
      }
    });

    test('guest browsing home/explore/provider detail is not redirected', () {
      for (final loc in ['/home', '/explore', '/providers/7']) {
        expect(
          AppRouter.redirectFor(
            location: loc,
            uri: Uri.parse(loc),
            sessionResolved: true,
            isAuthenticated: false,
          ),
          isNull,
        );
      }
    });

    test('guest hitting gated routes is sent to login with return-to-intent',
        () {
      final result = AppRouter.redirectFor(
        location: '/appointments/42',
        uri: Uri.parse('/appointments/42'),
        sessionResolved: true,
        isAuthenticated: false,
      );
      expect(result, startsWith('${Routes.login}?redirect='));
      expect(
        Uri.decodeComponent(result!.split('redirect=').last),
        '/appointments/42',
      );
    });

    test('guest at booking confirmation is gated, earlier steps are not', () {
      expect(
        AppRouter.redirectFor(
          location: '/providers/7/book/confirm',
          uri: Uri.parse('/providers/7/book/confirm'),
          sessionResolved: true,
          isAuthenticated: false,
        ),
        startsWith('${Routes.login}?redirect='),
      );
      expect(
        AppRouter.redirectFor(
          location: '/providers/7/book',
          uri: Uri.parse('/providers/7/book'),
          sessionResolved: true,
          isAuthenticated: false,
        ),
        isNull,
      );
    });

    test('guest hitting checkout is gated and returns there after login', () {
      // Paying is a signed-in action, and the customer must land back on checkout — not home — after logging in,
      // otherwise an unpaid booking is silently abandoned.
      final result = AppRouter.redirectFor(
        location: '/checkout/b1',
        uri: Uri.parse('/checkout/b1?providerId=p1'),
        sessionResolved: true,
        isAuthenticated: false,
      );
      expect(result, startsWith('${Routes.login}?redirect='));
      expect(
        Uri.decodeComponent(result!.split('redirect=').last),
        '/checkout/b1?providerId=p1',
      );
    });

    test('authenticated user reaches checkout without redirection', () {
      expect(
        AppRouter.redirectFor(
          location: '/checkout/b1',
          uri: Uri.parse('/checkout/b1?providerId=p1'),
          sessionResolved: true,
          isAuthenticated: true,
        ),
        isNull,
      );
    });

    test('authenticated user on login/otp continues to intended route', () {
      final target = Uri.encodeComponent('/appointments/42');
      expect(
        AppRouter.redirectFor(
          location: Routes.login,
          uri: Uri.parse('${Routes.login}?redirect=$target'),
          sessionResolved: true,
          isAuthenticated: true,
        ),
        '/appointments/42',
      );
      expect(
        AppRouter.redirectFor(
          location: Routes.otp,
          uri: Uri.parse(Routes.otp),
          sessionResolved: true,
          isAuthenticated: true,
        ),
        Routes.home,
      );
    });
  });

  group('AuthNotifier', () {
    test('latches resolution and ignores transient states', () {
      final notifier = AuthNotifier.detached();
      expect(notifier.sessionResolved, isFalse);

      // Transient states never resolve or flip authentication.
      notifier.apply(const AuthLoading());
      notifier.apply(const AuthError('boom'));
      notifier.apply(const OtpSentSuccess(message: 'm', phoneNumber: 'p'));
      expect(notifier.sessionResolved, isFalse);
      expect(notifier.isAuthenticated, isFalse);

      notifier.apply(const Unauthenticated());
      expect(notifier.sessionResolved, isTrue);
      expect(notifier.isAuthenticated, isFalse);

      // OTP-send loading while on login must not bounce auth state.
      notifier.apply(const AuthLoading());
      expect(notifier.sessionResolved, isTrue);
      expect(notifier.isAuthenticated, isFalse);

      notifier.apply(
        Authenticated(AuthSession(
          accessToken: 't',
          refreshToken: 'r',
          expiresIn: 3600,
          user: User(id: 'u1', phoneNumber: '0912', createdAt: DateTime(2026)),
        )),
      );
      expect(notifier.isAuthenticated, isTrue);

      notifier.apply(const LoggedOut());
      expect(notifier.isAuthenticated, isFalse);
      expect(notifier.sessionResolved, isTrue);
    });
  });

  group('booking → checkout coupling', () {
    test('checkoutFor builds the location the booking flow navigates to', () {
      expect(Routes.checkoutFor('b1', 'p1'), '/checkout/b1?providerId=p1');
    });

    test('checkout is OFF by default so the journey stays dark until its gates pass', () {
      // Release gate: the flag must default to false. Turning it on is a deliberate, explicit act
      // (`--dart-define=CHECKOUT_ENABLED=true`) after web E2E and the remaining reviews.
      expect(FeatureFlags.checkoutEnabled, isFalse);
    });

    test('existing gated and public routes are unaffected by the checkout route', () {
      // Regression guard: adding checkout must not change any pre-existing navigation decision.
      expect(
        AppRouter.redirectFor(
          location: Routes.home,
          uri: Uri.parse(Routes.home),
          sessionResolved: true,
          isAuthenticated: false,
        ),
        isNull,
      );
      expect(
        AppRouter.redirectFor(
          location: '/appointments/42',
          uri: Uri.parse('/appointments/42'),
          sessionResolved: true,
          isAuthenticated: false,
        ),
        startsWith('${Routes.login}?redirect='),
      );
    });
  });
}
