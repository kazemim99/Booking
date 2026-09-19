import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/widgets/app_empty_state.dart';
import 'package:booksy_provider_app/core/uploads/upload_progress_widgets.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_draft.dart';
import 'package:booksy_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/steps/gallery_step.dart';
import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart' show CancelToken;
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

const _png = <int>[
  0x89,
  0x50,
  0x4E,
  0x47,
  0x0D,
  0x0A,
  0x1A,
  0x0A,
  0x00,
  0x00,
  0x00,
  0x0D,
  0x49,
  0x48,
  0x44,
  0x52,
  0x00,
  0x00,
  0x00,
  0x01,
  0x00,
  0x00,
  0x00,
  0x01,
  0x08,
  0x06,
  0x00,
  0x00,
  0x00,
  0x1F,
  0x15,
  0xC4,
  0x89,
  0x00,
  0x00,
  0x00,
  0x0A,
  0x49,
  0x44,
  0x41,
  0x54,
  0x78,
  0x9C,
  0x63,
  0x00,
  0x01,
  0x00,
  0x00,
  0x05,
  0x00,
  0x01,
  0x0D,
  0x0A,
  0x2D,
  0xB4,
  0x00,
  0x00,
  0x00,
  0x00,
  0x49,
  0x45,
  0x4E,
  0x44,
  0xAE,
  0x42,
  0x60,
  0x82,
];

class _MockRepo extends Mock implements OnboardingRepository {}

void main() {
  setUpAll(() {
    registerFallbackValue(const GalleryImageUpload(name: 'x.jpg', bytes: []));
    registerFallbackValue(CancelToken());
  });

  testWidgets('gallery step shows an add-photos action and the empty state', (
    tester,
  ) async {
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
      expect(GalleryStep.mainFirst(['a', 'b', 'c', 'd'], 2), [
        'c',
        'a',
        'b',
        'd',
      ]);
    });

    test('index 0 (or empty/out-of-range) leaves order unchanged', () {
      expect(GalleryStep.mainFirst(['a', 'b'], 0), ['a', 'b']);
      expect(GalleryStep.mainFirst(<String>[], 0), <String>[]);
      expect(GalleryStep.mainFirst(['a', 'b'], 9), ['a', 'b']);
    });
  });

  testWidgets(
    'Next sends each photo on its own, cover first, with progress, then moves on',
    (tester) async {
      final repo = _MockRepo();
      // A draft already exists (created at the address step); resume at gallery.
      when(() => repo.getDraft()).thenAnswer(
        (_) async => const Right(
          OnboardingDraft(
            providerId: 'p-1',
            registrationStep: 6,
            data: OnboardingData(),
          ),
        ),
      );
      final sent = <String>[];
      when(
        () => repo.uploadGalleryImage(
          any(),
          any(),
          onProgress: any(named: 'onProgress'),
          cancelToken: any(named: 'cancelToken'),
        ),
      ).thenAnswer((inv) async {
        sent.add((inv.positionalArguments[1] as GalleryImageUpload).name);
        return const Right(null);
      });
      final cubit = OnboardingCubit(repo);
      await cubit.init(phoneNumber: '09120000000');
      expect(cubit.state.step, 6);

      await tester.pumpWidget(
        MaterialApp(
          home: Directionality(
            textDirection: TextDirection.rtl,
            child: BlocProvider.value(
              value: cubit,
              child: Scaffold(
                body: GalleryStep(
                  pickPhotos: (_) async => const [
                    GalleryImageUpload(name: 'first.jpg', bytes: _png),
                    GalleryImageUpload(name: 'cover.jpg', bytes: _png),
                  ],
                ),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.byKey(const Key('gallery-add')));
      await tester.pump();
      // Make the second photo the cover.
      await tester.tap(find.byKey(const Key('gallery-main-1')));
      await tester.pump();

      await tester.tap(find.text(AppStrings.next));
      await tester.pump();
      // While they go up, each photo shows as its own tile.
      expect(find.byType(UploadTile), findsNWidgets(2));

      await tester.pumpAndSettle();

      expect(sent, [
        'cover.jpg',
        'first.jpg',
      ], reason: 'one request per photo, the chosen cover numbered first');
      verifyNever(() => repo.uploadGallery(any(), any()));
      expect(cubit.state.step, 7, reason: 'advances once every photo landed');
    },
  );
}
