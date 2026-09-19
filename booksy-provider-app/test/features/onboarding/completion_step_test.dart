import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/features/home/domain/entities/saved_customer.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_draft.dart';
import 'package:booksy_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/steps/completion_step.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fake_contact_picker.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

/// The optional "your customers" offer on the last onboarding screen
/// (spec: provider-customer-book K6).
void main() {
  late _MockRepo repo;
  late int done;

  setUpAll(() {
    registerFallbackValue(const CustomerDraft(firstName: '', phone: ''));
    registerFallbackValue(<CustomerDraft>[]);
  });

  setUp(() {
    repo = _MockRepo();
    done = 0;
    // A finished registration resumes straight onto the completion screen.
    when(() => repo.getDraft()).thenAnswer((_) async => const Right(
        OnboardingDraft(
            providerId: 'p1', registrationStep: 9, data: OnboardingData())));
  });

  Future<void> pump(WidgetTester tester, {FakeContactPicker? picker}) async {
    final cubit = OnboardingCubit(repo);
    addTearDown(cubit.close);
    await cubit.init(phoneNumber: '09120000000');
    await tester.binding.setSurfaceSize(const Size(1080, 2400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light, // real theme: the buttons sit in a Row
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: BlocProvider.value(
            value: cubit,
            child: Scaffold(
              body: CompletionStep(
                onDone: () => done++,
                contactPicker: picker ?? FakeContactPicker(isSupported: false),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('skipping is just going to the dashboard, as before', (tester) async {
    await pump(tester);

    expect(find.byKey(const Key('onboarding-customers')), findsOneWidget);
    await tester.tap(find.byKey(const Key('onboarding-go-to-dashboard')));
    await tester.pumpAndSettle();

    expect(done, 1);
    verifyNever(() => repo.addCustomer(any(), any()));
    verifyNever(() => repo.importCustomers(any(), any()));
  });

  testWidgets('a customer typed in is saved to the new salon', (tester) async {
    when(() => repo.addCustomer(any(), any()))
        .thenAnswer((_) async => const Right(null));
    await pump(tester);

    await tester.tap(find.byKey(const Key('onboarding-customer-add')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('customer-first-name')), 'سارا');
    await tester.enterText(find.byKey(const Key('customer-phone')), '09351112233');
    await tester.tap(find.byKey(const Key('customer-save')));
    await tester.pumpAndSettle();

    verify(() => repo.addCustomer(
        'p1', const CustomerDraft(firstName: 'سارا', phone: '09351112233'))).called(1);
    expect(find.text(AppStrings.onboardingCustomersAdded(1)), findsWidgets);
  });

  testWidgets('ticked contacts are imported where the phone has a picker',
      (tester) async {
    const ticked = [
      CustomerDraft(firstName: 'مرتضی', lastName: 'کاظمی', phone: '09123135143'),
      CustomerDraft(firstName: 'سارا', phone: '09351112233'),
    ];
    when(() => repo.importCustomers(any(), any()))
        .thenAnswer((_) async => const Right(2));
    await pump(tester, picker: FakeContactPicker(picked: ticked));

    await tester.tap(find.byKey(const Key('onboarding-customer-import')));
    await tester.pumpAndSettle();

    verify(() => repo.importCustomers('p1', ticked)).called(1);
    expect(find.byKey(const Key('onboarding-customers-added')), findsOneWidget);
  });

  testWidgets('no import button where the phone has no picker', (tester) async {
    await pump(tester);
    expect(find.byKey(const Key('onboarding-customer-import')), findsNothing);
    expect(find.byKey(const Key('onboarding-customer-add')), findsOneWidget);
  });
}
