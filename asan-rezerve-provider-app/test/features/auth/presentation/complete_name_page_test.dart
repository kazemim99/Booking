import 'package:bloc_test/bloc_test.dart';
import 'package:asan_rezerve_provider_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/core/errors/failures.dart';
import 'package:asan_rezerve_provider_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/pages/complete_name_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';

class _MockAuthRepository extends Mock implements AuthRepository {}

class _MockAuthBloc extends MockBloc<AuthEvent, AuthState> implements AuthBloc {}

/// Asking the salon owner for a name, once, right after OTP — the way the customer app does (production QA
/// 2026-09-23: nothing ever asked, so the app showed «09123135143» as their name). Worth asking; never worth
/// blocking the salon's day over, so it can be skipped.
void main() {
  late _MockAuthRepository repository;
  late _MockAuthBloc authBloc;
  late String landedOn;

  setUpAll(() => registerFallbackValue(const ProviderStatusRefreshRequested()));

  setUp(() {
    repository = _MockAuthRepository();
    authBloc = _MockAuthBloc();
    whenListen(authBloc, const Stream<AuthState>.empty(), initialState: const AuthInitial());
    landedOn = '';
  });

  Future<void> pump(WidgetTester tester, {String? redirect}) async {
    final router = GoRouter(
      initialLocation: '/profile/name',
      routes: [
        GoRoute(
          path: '/profile/name',
          builder: (_, _) => CompleteNamePage(redirect: redirect, repository: repository),
        ),
        GoRoute(
          path: '/dashboard',
          builder: (_, _) {
            landedOn = '/dashboard';
            return const Scaffold(body: Text('dashboard'));
          },
        ),
        GoRoute(
          path: '/calendar',
          builder: (_, _) {
            landedOn = '/calendar';
            return const Scaffold(body: Text('calendar'));
          },
        ),
      ],
    );
    addTearDown(router.dispose);
    await tester.pumpWidget(BlocProvider<AuthBloc>.value(
      value: authBloc,
      child: MaterialApp.router(
        theme: AppTheme.light,
        routerConfig: router,
        builder: (context, child) => Directionality(textDirection: TextDirection.rtl, child: child!),
      ),
    ));
    await tester.pumpAndSettle();
  }

  testWidgets('saving sends the name, refreshes the session and carries on to where they were going',
      (tester) async {
    when(() => repository.updateMyName(firstName: 'مصطفی', lastName: 'کاظمی'))
        .thenAnswer((_) async => const Right(null));
    await pump(tester, redirect: Uri.encodeComponent('/calendar'));

    await tester.enterText(find.byKey(const Key('complete-name-first')), ' مصطفی ');
    await tester.enterText(find.byKey(const Key('complete-name-last')), 'کاظمی');
    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pumpAndSettle();

    verify(() => repository.updateMyName(firstName: 'مصطفی', lastName: 'کاظمی')).called(1);
    verify(() => authBloc.add(const ProviderStatusRefreshRequested())).called(1);
    expect(landedOn, '/calendar');
  });

  testWidgets('skipping carries on to the dashboard and saves nothing', (tester) async {
    await pump(tester);

    await tester.tap(find.byKey(const Key('complete-name-skip')));
    await tester.pumpAndSettle();

    verifyNever(() => repository.updateMyName(
        firstName: any(named: 'firstName'), lastName: any(named: 'lastName')));
    expect(landedOn, '/dashboard');
  });

  testWidgets('a first name is required; nothing is sent without one', (tester) async {
    await pump(tester);

    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.fieldRequired), findsOneWidget);
    verifyNever(() => repository.updateMyName(
        firstName: any(named: 'firstName'), lastName: any(named: 'lastName')));
    expect(landedOn, isEmpty);
  });

  testWidgets('a failed save says so and keeps what they typed', (tester) async {
    when(() => repository.updateMyName(firstName: 'مصطفی', lastName: ''))
        .thenAnswer((_) async => const Left(ServerFailure('ذخیره نام ناموفق بود')));
    await pump(tester);

    await tester.enterText(find.byKey(const Key('complete-name-first')), 'مصطفی');
    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('complete-name-error')), findsOneWidget);
    expect(find.text('مصطفی'), findsOneWidget);
    expect(landedOn, isEmpty);
  });

  testWidgets('says why it asks: people should know them by name, not by number', (tester) async {
    await pump(tester);

    expect(find.text(AppStrings.completeNameSubtitle), findsOneWidget);
    expect(find.text(AppStrings.completeNameSkip), findsOneWidget);
  });
}
