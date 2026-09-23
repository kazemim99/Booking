import 'package:booksy_provider_app/config/routes/app_router.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:flutter_test/flutter_test.dart';

String? redirect(String location, AuthFlowStatus status, {String? query}) {
  final full = query == null ? location : '$location?$query';
  return AppRouter.redirectFor(
    location: location,
    uri: Uri.parse(full),
    status: status,
  );
}

void main() {
  group('AppRouter.redirectFor', () {
    group('unresolved (cold start)', () {
      test('holds on splash', () {
        expect(redirect('/splash', AuthFlowStatus.unresolved), isNull);
      });
      test('bounces others to splash', () {
        expect(redirect('/dashboard', AuthFlowStatus.unresolved), '/splash');
      });
    });

    group('unauthenticated', () {
      test('allows login/otp', () {
        expect(redirect('/login', AuthFlowStatus.unauthenticated), isNull);
        expect(redirect('/otp', AuthFlowStatus.unauthenticated), isNull);
      });
      test('splash -> login', () {
        expect(redirect('/splash', AuthFlowStatus.unauthenticated), '/login');
      });
      test('protected route -> login with return-to-intent', () {
        final r = redirect('/dashboard', AuthFlowStatus.unauthenticated);
        expect(r, startsWith('/login?redirect='));
        expect(r, contains(Uri.encodeComponent('/dashboard')));
      });
    });

    group('authenticated', () {
      test('splash -> dashboard', () {
        expect(redirect('/splash', AuthFlowStatus.authenticated), '/dashboard');
      });
      test('onboarding/blocked -> dashboard', () {
        expect(redirect('/onboarding', AuthFlowStatus.authenticated), '/dashboard');
        expect(redirect('/blocked', AuthFlowStatus.authenticated), '/dashboard');
      });
      test('login honors return-to-intent', () {
        final target = Uri.encodeComponent('/dashboard');
        expect(
          redirect('/login', AuthFlowStatus.authenticated, query: 'redirect=$target'),
          '/dashboard',
        );
      });
      test('login without redirect -> dashboard', () {
        expect(redirect('/login', AuthFlowStatus.authenticated), '/dashboard');
      });
      test('stays on dashboard', () {
        expect(redirect('/dashboard', AuthFlowStatus.authenticated), isNull);
      });
    });

    group('needsOnboarding', () {
      test('everything -> onboarding', () {
        expect(redirect('/dashboard', AuthFlowStatus.needsOnboarding), '/onboarding');
        expect(redirect('/login', AuthFlowStatus.needsOnboarding), '/onboarding');
      });
      test('stays on onboarding', () {
        expect(redirect('/onboarding', AuthFlowStatus.needsOnboarding), isNull);
      });
    });

    group('blocked (E-14)', () {
      test('everything -> blocked', () {
        expect(redirect('/dashboard', AuthFlowStatus.blocked), '/blocked');
        expect(redirect('/onboarding', AuthFlowStatus.blocked), '/blocked');
      });
      test('stays on blocked', () {
        expect(redirect('/blocked', AuthFlowStatus.blocked), isNull);
      });
    });
  });

  // Production QA 2026-09-23: nothing ever asked the salon owner for a name, so the app showed the number.
  // Like the customer app: asked once, right after OTP; skippable; never at a cold start or mid-app.
  group('the name, right after OTP', () {
    String? after(String location, {String? query}) {
      final full = query == null ? location : '$location?$query';
      return AppRouter.redirectFor(
        location: location,
        uri: Uri.parse(full),
        status: AuthFlowStatus.authenticated,
        nameMissing: true,
      );
    }

    test('leaving OTP without a real name goes to the name page, then to the destination', () {
      expect(after('/otp'), '/profile/name?redirect=${Uri.encodeComponent('/dashboard')}');
      final target = Uri.encodeComponent('/calendar');
      expect(after('/login', query: 'redirect=$target'),
          '/profile/name?redirect=${Uri.encodeComponent('/calendar')}');
    });

    test('never anywhere else — a skip is not undone, a restart does not ask', () {
      expect(after('/profile/name'), isNull);
      expect(after('/dashboard'), isNull);
      expect(after('/splash'), '/dashboard');
    });

    test('a named account goes straight on', () {
      expect(redirect('/otp', AuthFlowStatus.authenticated), '/dashboard');
    });

    test('onboarding asks for the owner name itself, so it is never interrupted', () {
      expect(
        AppRouter.redirectFor(
          location: '/otp',
          uri: Uri.parse('/otp'),
          status: AuthFlowStatus.needsOnboarding,
          nameMissing: true,
        ),
        '/onboarding',
      );
    });

    test('the notifier latches it from the signed-in session', () {
      ProviderSession session(String fullName) => ProviderSession(
            accessToken: 'a',
            refreshToken: 'r',
            expiresIn: 1,
            user: ProviderUser(id: 'u', phoneNumber: '09123135143', fullName: fullName),
            providerId: 'p',
            providerStatus: ProviderStatus.active,
            isNewProvider: false,
            requiresOnboarding: false,
          );

      final notifier = AuthNotifier.detached()..apply(Authenticated(session('ارائه‌دهنده 9123135143')));
      expect(notifier.nameMissing, isTrue);
      notifier.apply(Authenticated(session('مصطفی کاظمی')));
      expect(notifier.nameMissing, isFalse);
    });
  });
}
