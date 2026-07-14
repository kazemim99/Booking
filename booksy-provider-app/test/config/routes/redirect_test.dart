import 'package:booksy_provider_app/config/routes/app_router.dart';
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
}
