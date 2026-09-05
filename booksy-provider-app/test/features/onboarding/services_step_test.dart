import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/widgets/app_list_row.dart';
import 'package:booksy_provider_app/core/widgets/app_section_header.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:booksy_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/steps/services_step.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

Future<OnboardingCubit> _readyCubit(_MockRepo repo) async {
  when(() => repo.getDraft()).thenAnswer((_) async => const Right(null));
  final cubit = OnboardingCubit(repo);
  await cubit.init(phoneNumber: '09120000000');
  return cubit;
}

Widget _harness(OnboardingCubit cubit) => MaterialApp(
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: BlocProvider.value(
          value: cubit,
          child: const Scaffold(body: ServicesStep()),
        ),
      ),
    );

void main() {
  testWidgets('empty state: section header with add action, no rows',
      (tester) async {
    final cubit = await _readyCubit(_MockRepo());

    await tester.pumpWidget(_harness(cubit));

    expect(find.byType(AppSectionHeader), findsOneWidget);
    expect(find.text(AppStrings.servicesTitle), findsWidgets);
    expect(find.byKey(const Key('onboarding-add-service')), findsOneWidget);
    expect(find.byType(AppListRow), findsNothing);
    expect(find.text(AppStrings.noServicesYet), findsOneWidget);
  });

  testWidgets('renders one AppListRow per service with name and subtitle',
      (tester) async {
    final cubit = await _readyCubit(_MockRepo());
    cubit.setServices(const [
      ServiceDraft(
        name: 'کوتاهی مو',
        durationHours: 0,
        durationMinutes: 30,
        price: 150000,
      ),
      ServiceDraft(
        name: 'رنگ مو',
        durationHours: 1,
        durationMinutes: 0,
        price: 500000,
      ),
    ]);

    await tester.pumpWidget(_harness(cubit));

    expect(find.byType(AppListRow), findsNWidgets(2));
    expect(find.text('کوتاهی مو'), findsOneWidget);
    expect(find.text('رنگ مو'), findsOneWidget);
    expect(find.textContaining('30 دقیقه'), findsOneWidget);
    expect(find.text(AppStrings.noServicesYet), findsNothing);
  });

  testWidgets('deleting a row removes it from cubit state', (tester) async {
    final cubit = await _readyCubit(_MockRepo());
    cubit.setServices(const [
      ServiceDraft(
        name: 'کوتاهی مو',
        durationHours: 0,
        durationMinutes: 30,
        price: 150000,
      ),
    ]);

    await tester.pumpWidget(_harness(cubit));
    expect(find.byType(AppListRow), findsOneWidget);

    await tester.tap(find.byIcon(Icons.delete_outline));
    await tester.pump();

    expect(cubit.state.data.services, isEmpty);
    expect(find.byType(AppListRow), findsNothing);
  });
}
