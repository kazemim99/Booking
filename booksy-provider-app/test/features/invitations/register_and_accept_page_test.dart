import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/widgets/app_loading.dart';
import 'package:booksy_provider_app/features/invitations/domain/invitation_repository.dart';
import 'package:booksy_provider_app/features/invitations/domain/invitation_summary.dart';
import 'package:booksy_provider_app/features/invitations/presentation/register_and_accept_cubit.dart';
import 'package:booksy_provider_app/features/invitations/presentation/register_and_accept_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements InvitationRepository {}

void main() {
  late _MockRepo repo;

  const summary = InvitationSummary(
    invitationId: 'inv-1',
    organizationId: 'org-1',
    organizationName: 'سالن رُز',
    maskedPhone: '••••••1234',
    status: 'Pending',
    isValid: true,
  );

  setUp(() {
    repo = _MockRepo();
    registerFallbackValue('');
  });

  Future<void> pump(WidgetTester tester, RegisterAndAcceptCubit cubit) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: BlocProvider.value(
            value: cubit,
            child: const RegisterAndAcceptView(),
          ),
        ),
      ),
    );
  }

  testWidgets('renders the loading state, then not-found for an unknown invitation',
      (tester) async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(null));
    final cubit = RegisterAndAcceptCubit(repo, 'inv-1');

    await pump(tester, cubit);
    expect(find.byType(AppLoading), findsOneWidget);

    cubit.load();
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.acceptInvitationNotFound), findsOneWidget);
    await cubit.close();
  });

  testWidgets(
      'renders the name form for a valid invitation, then validates before sending',
      (tester) async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    final cubit = RegisterAndAcceptCubit(repo, 'inv-1');

    await pump(tester, cubit);
    cubit.load();
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('register-accept-first-name')), findsOneWidget);
    expect(find.byKey(const Key('register-accept-last-name')), findsOneWidget);
    expect(find.text(AppStrings.acceptInvitationInvitedTo('سالن رُز')),
        findsOneWidget);

    // Empty fields: tapping send must not call the repository at all.
    await tester.tap(find.byKey(const Key('register-accept-send-code')));
    await tester.pump();
    verifyNever(() => repo.sendOtp(any()));

    await cubit.close();
  });

  testWidgets('sends the OTP and shows the masked phone on the verify step',
      (tester) async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.sendOtp('inv-1'))
        .thenAnswer((_) async => const Right('••••••1234'));
    final cubit = RegisterAndAcceptCubit(repo, 'inv-1');

    await pump(tester, cubit);
    cubit.load();
    await tester.pumpAndSettle();

    await tester.enterText(
        find.byKey(const Key('register-accept-first-name')), 'Ali');
    await tester.enterText(
        find.byKey(const Key('register-accept-last-name')), 'Rezai');
    await tester.tap(find.byKey(const Key('register-accept-send-code')));
    // Pinput (inside OtpInput) runs a cursor-blink animation that never
    // settles, so a bounded pump is used instead of pumpAndSettle here.
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.byKey(const Key('register-accept-otp')), findsOneWidget);
    expect(find.textContaining('••••••1234'), findsOneWidget);
    await cubit.close();
  });
}
