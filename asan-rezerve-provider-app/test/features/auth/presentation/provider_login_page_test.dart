import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:booksy_provider_app/features/auth/presentation/pages/provider_login_page.dart';
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
          child: const ProviderLoginPage(),
        ),
      ),
    );
  }

  testWidgets('shows required error when phone is empty', (tester) async {
    await pump(tester);
    await tester.tap(find.widgetWithText(FilledButton, AppStrings.sendOtp));
    await tester.pump();

    expect(find.text(AppStrings.phoneRequired), findsOneWidget);
    verifyNever(() => bloc.add(any()));
  });

  testWidgets('shows invalid error for a bad number', (tester) async {
    await pump(tester);
    await tester.enterText(find.byType(TextField), '0812');
    await tester.tap(find.widgetWithText(FilledButton, AppStrings.sendOtp));
    await tester.pump();

    expect(find.text(AppStrings.phoneInvalid), findsOneWidget);
    verifyNever(() => bloc.add(any()));
  });

  testWidgets('dispatches SendVerificationCodeRequested with canonical phone',
      (tester) async {
    await pump(tester);
    await tester.enterText(find.byType(TextField), '09121234567');
    await tester.tap(find.widgetWithText(FilledButton, AppStrings.sendOtp));
    await tester.pump();

    verify(() => bloc.add(const SendVerificationCodeRequested('09121234567')))
        .called(1);
  });
}
