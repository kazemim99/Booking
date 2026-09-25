import 'package:asan_rezerve_provider_app/config/theme/app_tokens.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/steps/category_step.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

Future<OnboardingCubit> _readyCubit() async {
  final repo = _MockRepo();
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
          child: const Scaffold(body: CategoryStep()),
        ),
      ),
    );

Color _borderColor(WidgetTester tester, String categoryId) {
  final container = tester.widget<AnimatedContainer>(
    find.descendant(
      of: find.byKey(Key('category-$categoryId')),
      matching: find.byType(AnimatedContainer),
    ),
  );
  final decoration = container.decoration as BoxDecoration;
  return decoration.border!.top.color;
}

void main() {
  testWidgets('idle tiles render the grey border/content color',
      (tester) async {
    final cubit = await _readyCubit();
    await tester.pumpWidget(_harness(cubit));

    expect(_borderColor(tester, 'hair_salon'), AppColors.subtitle);
    expect(_borderColor(tester, 'barbershop'), AppColors.subtitle);
  });

  testWidgets(
      'tapping a tile turns it blue and animates the previous selection back to idle',
      (tester) async {
    final cubit = await _readyCubit();
    await tester.pumpWidget(_harness(cubit));

    await tester.tap(find.byKey(const Key('category-hair_salon')));
    await tester.pumpAndSettle();

    expect(_borderColor(tester, 'hair_salon'), AppColors.appBar);
    expect(_borderColor(tester, 'barbershop'), AppColors.subtitle);

    await tester.tap(find.byKey(const Key('category-barbershop')));
    await tester.pumpAndSettle();

    expect(_borderColor(tester, 'barbershop'), AppColors.appBar);
    expect(_borderColor(tester, 'hair_salon'), AppColors.subtitle);
  });

  testWidgets(
      'the tile and its label animate on the AppMotion.fast duration/curve',
      (tester) async {
    final cubit = await _readyCubit();
    await tester.pumpWidget(_harness(cubit));

    final container = tester.widget<AnimatedContainer>(
      find.descendant(
        of: find.byKey(const Key('category-hair_salon')),
        matching: find.byType(AnimatedContainer),
      ),
    );
    expect(container.duration, AppMotion.fast);
    expect(container.curve, AppMotion.curve);

    final label = tester.widget<AnimatedDefaultTextStyle>(
      find.descendant(
        of: find.byKey(const Key('category-hair_salon')),
        matching: find.byType(AnimatedDefaultTextStyle),
      ),
    );
    expect(label.duration, AppMotion.fast);
    expect(label.curve, AppMotion.curve);
  });
}
