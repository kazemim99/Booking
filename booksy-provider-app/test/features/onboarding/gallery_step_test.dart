import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/widgets/app_empty_state.dart';
import 'package:booksy_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/steps/gallery_step.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

void main() {
  testWidgets('gallery step shows an add-photos action and the empty state',
      (tester) async {
    final repo = _MockRepo();
    when(() => repo.getDraft()).thenAnswer((_) async => const Right(null));
    final cubit = OnboardingCubit(repo);
    await cubit.init(phoneNumber: '09120000000');

    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: BlocProvider.value(
            value: cubit,
            child: const Scaffold(body: GalleryStep()),
          ),
        ),
      ),
    );

    // An add-photos action is now available (upload is wired).
    expect(find.byKey(const Key('gallery-add')), findsOneWidget);
    // With nothing picked yet, the empty state is shown.
    expect(find.byType(AppEmptyState), findsOneWidget);
    expect(find.byIcon(Icons.photo_library_outlined), findsOneWidget);
    expect(find.text(AppStrings.galleryEmptyCaption), findsOneWidget);
  });

  group('GalleryStep.mainFirst (cover = uploaded first)', () {
    test('moves the chosen main image to the front, keeping order', () {
      expect(GalleryStep.mainFirst(['a', 'b', 'c', 'd'], 2),
          ['c', 'a', 'b', 'd']);
    });

    test('index 0 (or empty/out-of-range) leaves order unchanged', () {
      expect(GalleryStep.mainFirst(['a', 'b'], 0), ['a', 'b']);
      expect(GalleryStep.mainFirst(<String>[], 0), <String>[]);
      expect(GalleryStep.mainFirst(['a', 'b'], 9), ['a', 'b']);
    });
  });
}
