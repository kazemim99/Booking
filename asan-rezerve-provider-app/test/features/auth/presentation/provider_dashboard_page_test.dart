import 'package:bloc_test/bloc_test.dart';
import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:asan_rezerve_provider_app/features/auth/presentation/pages/provider_dashboard_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockAuthBloc extends MockBloc<AuthEvent, AuthState> implements AuthBloc {}

void main() {
  late _MockAuthBloc bloc;

  setUpAll(() {
    registerFallbackValue(const AuthStatusChecked());
  });

  setUp(() {
    bloc = _MockAuthBloc();
    whenListen(
      bloc,
      const Stream<AuthState>.empty(),
      initialState: const AuthInitial(),
    );
  });

  Future<void> pump(WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<AuthBloc>.value(
          value: bloc,
          child: const ProviderDashboardPage(),
        ),
      ),
    );
  }

  testWidgets('tapping logout shows a confirmation dialog, does not log out yet',
      (tester) async {
    await pump(tester);

    await tester.tap(find.byTooltip(AppStrings.logout));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.logoutConfirmTitle), findsOneWidget);
    expect(find.text(AppStrings.logoutConfirmBody), findsOneWidget);
    verifyNever(() => bloc.add(const LogoutRequested()));
  });

  testWidgets('cancelling the dialog does not log out', (tester) async {
    await pump(tester);

    await tester.tap(find.byTooltip(AppStrings.logout));
    await tester.pumpAndSettle();
    await tester.tap(find.text(AppStrings.cancel));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.logoutConfirmTitle), findsNothing);
    verifyNever(() => bloc.add(const LogoutRequested()));
  });

  testWidgets('confirming the dialog dispatches LogoutRequested',
      (tester) async {
    await pump(tester);

    await tester.tap(find.byTooltip(AppStrings.logout));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('confirm-dialog-confirm')));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.logoutConfirmTitle), findsNothing);
    verify(() => bloc.add(const LogoutRequested())).called(1);
  });
}
