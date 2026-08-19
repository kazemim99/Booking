import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/config/theme/app_tokens.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/core/widgets/profile_header.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:booksy_provider_app/features/home/data/datasources/home_api_service.dart';
import 'package:booksy_provider_app/features/home/domain/entities/composer_models.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart'
    show BreakTime, ClockTime, DayHours, GalleryImageUpload;
import 'package:booksy_provider_app/features/home/domain/entities/more_models.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/more_cubits.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/gallery_page.dart';
import 'package:booksy_provider_app/features/home/presentation/widgets/block_time_sheet.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/more_page.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/more_sub_pages.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockHomeRepository extends Mock implements HomeRepository {}

class _MockAuthRepo extends Mock implements AuthRepository {}

class _MockAuthBloc extends MockBloc<AuthEvent, AuthState>
    implements AuthBloc {}

ProviderSession get _session => ProviderSession(
      accessToken: 'a',
      refreshToken: 'r',
      expiresIn: 900,
      user: const ProviderUser(
        id: 'u-1',
        phoneNumber: '09121234567',
        fullName: 'سالن رُز',
      ),
      providerId: 'p-1',
      providerStatus: ProviderStatus.active,
      isNewProvider: false,
      requiresOnboarding: false,
    );

AuthRepository _authRepo() {
  final auth = _MockAuthRepo();
  when(() => auth.switchActiveOrganization(providerId: any(named: 'providerId')))
      .thenAnswer((_) async => Right(_session));
  return auth;
}

void main() {
  late MockHomeRepository repository;

  setUp(() {
    repository = MockHomeRepository();
  });

  group('More cubits (shared load/retry shape)', () {
    test('InsightsCubit: ready on success, failed with message on error',
        () async {
      when(() => repository.fetchInsights()).thenAnswer(
        (_) async => const Right(InsightsSummary(totalBookings: 12)),
      );
      final cubit = InsightsCubit(repository);
      await cubit.load();
      expect(cubit.state.status, MoreStatus.ready);
      expect(cubit.state.data!.totalBookings, 12);

      when(() => repository.fetchInsights())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      await cubit.load();
      expect(cubit.state.status, MoreStatus.failed);
      expect(cubit.state.error, 'خطا');
      await cubit.close();
    });

    test('ServicesCubit and StaffCubit load their lists', () async {
      when(() => repository.fetchServices()).thenAnswer(
        (_) async => const Right([
          ComposerService(id: 's1', name: 'اصلاح', durationMinutes: 45),
        ]),
      );
      when(() => repository.fetchOrgMembers()).thenAnswer(
        (_) async => const Right([
          OrgMember(membershipId: 'mem-1', name: 'سارا', roles: ['StaffProvider'], status: 'Active'),
        ]),
      );

      final services = ServicesCubit(repository);
      final staff = StaffCubit(repository);
      await services.load();
      await staff.load();

      expect(services.state.data, hasLength(1));
      expect(staff.state.data!.single.name, 'سارا');
      await services.close();
      await staff.close();
    });
  });

  group('MorePage hub', () {
    late _MockAuthBloc authBloc;

    setUp(() {
      authBloc = _MockAuthBloc();
      whenListen(
        authBloc,
        const Stream<AuthState>.empty(),
        initialState: Authenticated(_session),
      );
    });

    Future<void> pump(WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<AuthBloc>.value(
            value: authBloc,
            child: const MorePage(),
          ),
        ),
      );
      await tester.pump();
    }

    testWidgets('renders sections, identity, live rows and disabled rows',
        (tester) async {
      await pump(tester);

      expect(find.text(AppStrings.moreBusinessSection), findsOneWidget);
      for (final k in [
        'more-services',
        'more-staff',
        'more-insights',
        'more-share',
      ]) {
        expect(find.byKey(Key(k)), findsOneWidget);
      }
      // No coming-soon rows remain — every hub destination is live.
      expect(find.text(AppStrings.comingSoon), findsNothing);
      for (final k in [
        'more-profile',
        'more-hours',
        'more-holidays',
        'more-gallery',
      ]) {
        expect(tester.widget<InkWell>(find.byKey(Key(k))).onTap, isNotNull);
      }

      // The account section sits below the fold in the test viewport.
      await tester.scrollUntilVisible(
          find.byKey(const Key('more-logout')), 200);
      expect(find.text('سالن رُز'), findsOneWidget);
      expect(find.byKey(const Key('more-logout')), findsOneWidget);
    });

    testWidgets('renders the profile chrome: blue header with the identity',
        (tester) async {
      await pump(tester);

      // Blue chrome carrying the identity (DESIGN_LANGUAGE.md §5.12).
      expect(find.byType(ProfileHeader), findsOneWidget);
      final header = tester.widget<ProfileHeader>(find.byType(ProfileHeader));
      expect(header.name, 'سالن رُز');
      expect(header.subtitle, contains('09121234567'));
      // Logout lives once, as a labelled row — not duplicated as a disc.
      expect(header.action, isNull);
    });

    testWidgets('groups rows into cards with hairline dividers', (tester) async {
      await pump(tester);

      // Business card: 8 rows ⇒ 7 internal dividers (§4.2 one card, many rows).
      final dividers = find.descendant(
        of: find.byType(ListView),
        matching: find.byType(Divider),
      );
      expect(tester.widgetList<Divider>(dividers).length, greaterThanOrEqualTo(7));
      for (final d in tester.widgetList<Divider>(dividers)) {
        expect(d.color, AppColors.menuBorder);
      }
    });

    testWidgets('logout dispatches LogoutRequested', (tester) async {
      await pump(tester);

      // scrollUntilVisible stops as soon as the row attaches, which can leave
      // it under the floating nav pill; ensureVisible brings it fully into
      // the viewport so the tap lands on the row, not the nav.
      await tester.scrollUntilVisible(
          find.byKey(const Key('more-logout')), 200);
      await tester.ensureVisible(find.byKey(const Key('more-logout')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('more-logout')));

      verify(() => authBloc.add(const LogoutRequested())).called(1);
    });
  });

  group('Business profile editing (spec: provider-business-profile-editing)',
      () {
    const profile =
        BusinessProfile(businessName: 'سالن رُز', description: 'توضیح');

    setUp(() {
      when(() => repository.fetchBusinessProfile())
          .thenAnswer((_) async => const Right(profile));
      when(() => repository.updateBusinessProfile(
            businessName: any(named: 'businessName'),
            description: any(named: 'description'),
          )).thenAnswer((_) async => const Right(null));
    });

    test('cubit loads the profile and save maps failures', () async {
      final cubit = BusinessProfileCubit(repository);
      await cubit.load();
      expect(cubit.state.data, profile);

      expect(await cubit.save(businessName: 'سالن نو'), isNull);

      when(() => repository.updateBusinessProfile(
            businessName: any(named: 'businessName'),
            description: any(named: 'description'),
          )).thenAnswer((_) async => const Left(ServerFailure('خطا')));
      final failure = await cubit.save(businessName: 'سالن نو');
      expect(failure!.message, 'خطا');
      await cubit.close();
    });

    Future<void> pumpForm(WidgetTester tester) async {
      final cubit = BusinessProfileCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<BusinessProfileCubit>.value(
            value: cubit..load(),
            child: const BusinessProfileView(),
          ),
        ),
      );
      await tester.pumpAndSettle();
    }

    testWidgets('form is pre-filled and gated on the business name',
        (tester) async {
      await pumpForm(tester);

      expect(find.text('سالن رُز'), findsOneWidget);
      expect(find.text('توضیح'), findsOneWidget);

      await tester.enterText(find.byKey(const Key('business-name')), '');
      await tester.pumpAndSettle();
      final saveButton = tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('business-save')),
        matching: find.byType(FilledButton),
      ));
      expect(saveButton.onPressed, isNull);
    });

    testWidgets('save persists the edited values', (tester) async {
      await pumpForm(tester);

      await tester.enterText(
          find.byKey(const Key('business-description')), 'توضیح تازه');
      await tester.tap(find.byKey(const Key('business-save')));
      await tester.pumpAndSettle();

      verify(() => repository.updateBusinessProfile(
            businessName: 'سالن رُز',
            description: 'توضیح تازه',
          )).called(1);
    });

    testWidgets('failure keeps the edits in the form', (tester) async {
      when(() => repository.updateBusinessProfile(
            businessName: any(named: 'businessName'),
            description: any(named: 'description'),
          )).thenAnswer((_) async => const Left(ServerFailure('خطا')));
      await pumpForm(tester);

      await tester.enterText(
          find.byKey(const Key('business-name')), 'سالن نو');
      await tester.tap(find.byKey(const Key('business-save')));
      await tester.pumpAndSettle();

      expect(find.text('خطا'), findsOneWidget); // snackbar
      expect(find.text('سالن نو'), findsOneWidget); // edit preserved
    });
  });

  group('Service CRUD (spec: provider-service-crud)', () {
    const haircut = ComposerService(
      id: 's1',
      name: 'اصلاح مو',
      durationMinutes: 45,
      price: 250000,
      description: 'با شستشو',
    );

    setUp(() {
      when(() => repository.fetchServices())
          .thenAnswer((_) async => const Right([haircut]));
      when(() => repository.addService(
            name: any(named: 'name'),
            durationMinutes: any(named: 'durationMinutes'),
            price: any(named: 'price'),
            description: any(named: 'description'),
          )).thenAnswer((_) async => const Right(null));
      when(() => repository.updateService(
            any(),
            name: any(named: 'name'),
            durationMinutes: any(named: 'durationMinutes'),
            price: any(named: 'price'),
            description: any(named: 'description'),
          )).thenAnswer((_) async => const Right(null));
      when(() => repository.removeService(any()))
          .thenAnswer((_) async => const Right(null));
    });

    Future<ServicesCubit> pumpServices(WidgetTester tester) async {
      final cubit = ServicesCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<ServicesCubit>.value(
            value: cubit..load(),
            child: const ServicesView(),
          ),
        ),
      );
      await tester.pumpAndSettle();
      return cubit;
    }

    testWidgets('list leads with the green add row; it opens the form sheet',
        (tester) async {
      await pumpServices(tester);

      final row = find.byKey(const Key('service-add-row'));
      expect(row, findsOneWidget);
      expect(
        tester.getTopLeft(row).dy,
        lessThan(
            tester.getTopLeft(find.byKey(const Key('service-row-s1'))).dy),
      );

      await tester.tap(
        find.descendant(of: row, matching: find.byType(TextButton)),
      );
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('service-save')), findsOneWidget);
    });

    testWidgets('add flow: gated until valid, then submits the payload',
        (tester) async {
      await pumpServices(tester);

      await tester.tap(find.byKey(const Key('service-add')));
      await tester.pumpAndSettle();

      var save = tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('service-save')),
        matching: find.byType(FilledButton),
      ));
      expect(save.onPressed, isNull); // empty form gated

      await tester.enterText(
          find.byKey(const Key('service-name')), 'رنگ مو');
      await tester.enterText(find.byKey(const Key('service-duration')), '90');
      await tester.enterText(
          find.byKey(const Key('service-price')), '500000');
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('service-save')));
      await tester.pumpAndSettle();

      verify(() => repository.addService(
            name: 'رنگ مو',
            durationMinutes: 90,
            price: 500000,
            description: '',
          )).called(1);
      expect(find.text(AppStrings.serviceAdded), findsOneWidget);
    });

    testWidgets('edit flow: pre-filled, round-trips the description',
        (tester) async {
      await pumpServices(tester);

      await tester.tap(find.byKey(const Key('service-row-s1')));
      await tester.pumpAndSettle();

      expect(find.text('اصلاح مو'), findsWidgets); // prefilled
      expect(find.text('با شستشو'), findsOneWidget);

      await tester.enterText(
          find.byKey(const Key('service-price')), '300000');
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('service-save')));
      await tester.pumpAndSettle();

      verify(() => repository.updateService(
            's1',
            name: 'اصلاح مو',
            durationMinutes: 45,
            price: 300000,
            description: 'با شستشو', // untouched fields round-trip
          )).called(1);
    });

    testWidgets('delete requires confirmation', (tester) async {
      await pumpServices(tester);

      await tester.tap(find.byKey(const Key('service-remove-s1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('service-remove-cancel')));
      await tester.pumpAndSettle();
      verifyNever(() => repository.removeService(any()));

      await tester.tap(find.byKey(const Key('service-remove-s1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('service-remove-confirm')));
      await tester.pumpAndSettle();
      verify(() => repository.removeService('s1')).called(1);
    });
  });

  group('Block time (spec: provider-block-time)', () {
    testWidgets('sheet gates on reason/times and submits the payload',
        (tester) async {
      DateTime? sentDate;
      String? sentOpen;
      String? sentReason;
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: Scaffold(
            body: BlockTimeSheet(
              initialDate: DateTime(2026, 7, 20),
              onSubmit: ({
                required DateTime date,
                String? openTime,
                String? closeTime,
                required String reason,
              }) async {
                sentDate = date;
                sentOpen = openTime;
                sentReason = reason;
                return null;
              },
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Gated: no reason yet.
      var submit = tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('block-submit')),
        matching: find.byType(FilledButton),
      ));
      expect(submit.onPressed, isNull);

      // Modified-hours mode without times stays gated even with a reason.
      await tester.enterText(
          find.byKey(const Key('block-reason')), 'تعمیرات');
      await tester.tap(find.byKey(const Key('block-all-day')));
      await tester.pumpAndSettle();
      submit = tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('block-submit')),
        matching: find.byType(FilledButton),
      ));
      expect(submit.onPressed, isNull);

      // Back to all-day → submittable; payload carries null times.
      await tester.tap(find.byKey(const Key('block-all-day')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('block-submit')));
      await tester.pumpAndSettle();

      // Payload proves the submit ran (the success snackbar is asserted via
      // the host pages' flows; a directly-pumped sheet has no route to pop).
      expect(sentDate, DateTime(2026, 7, 20));
      expect(sentOpen, isNull);
      expect(sentReason, 'تعمیرات');
    });

    testWidgets('exceptions section lists and removes behind confirmation',
        (tester) async {
      final exception = AvailabilityException(
        id: 'e1',
        date: DateTime(2026, 8, 1),
        reason: 'تعمیرات',
        isClosed: true,
      );
      when(() => repository.fetchExceptions())
          .thenAnswer((_) async => Right([exception]));
      when(() => repository.removeException(any()))
          .thenAnswer((_) async => const Right(null));
      when(() => repository.fetchHolidays())
          .thenAnswer((_) async => const Right([]));

      final holidays = HolidaysCubit(repository);
      final exceptions = ExceptionsCubit(repository);
      addTearDown(holidays.close);
      addTearDown(exceptions.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: MultiBlocProvider(
            providers: [
              BlocProvider<HolidaysCubit>.value(value: holidays..load()),
              BlocProvider<ExceptionsCubit>.value(
                  value: exceptions..load()),
            ],
            child: const HolidaysView(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.exceptionsSection), findsOneWidget);
      expect(find.byKey(const Key('exception-row-e1')), findsOneWidget);
      expect(find.textContaining(AppStrings.exceptionClosedAllDay),
          findsOneWidget);

      await tester.tap(find.byKey(const Key('exception-remove-e1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('exception-remove-confirm')));
      await tester.pumpAndSettle();
      verify(() => repository.removeException('e1')).called(1);
    });
  });

  group('Gallery management (spec: provider-gallery-management)', () {
    const primary = GalleryImage(
        id: 'g1', thumbnailUrl: 'http://x/1.jpg', isPrimary: true);
    const secondary =
        GalleryImage(id: 'g2', thumbnailUrl: 'http://x/2.jpg');

    setUp(() {
      when(() => repository.fetchGallery())
          .thenAnswer((_) async => const Right([primary, secondary]));
      when(() => repository.uploadGalleryImages(any()))
          .thenAnswer((_) async => const Right(null));
      when(() => repository.setPrimaryGalleryImage(any()))
          .thenAnswer((_) async => const Right(null));
      when(() => repository.removeGalleryImage(any()))
          .thenAnswer((_) async => const Right(null));
    });

    test('cubit mutations reload on success', () async {
      final cubit = GalleryCubit(repository);
      await cubit.load();
      clearInteractions(repository);
      when(() => repository.fetchGallery())
          .thenAnswer((_) async => const Right([primary]));

      expect(await cubit.setPrimary('g2'), isNull);
      await Future<void>.delayed(Duration.zero);
      verify(() => repository.fetchGallery()).called(1);
      await cubit.close();
    });

    Future<GalleryCubit> pumpGallery(
      WidgetTester tester, {
      PickGalleryImages? picker,
    }) async {
      final cubit = GalleryCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<GalleryCubit>.value(
            value: cubit..load(),
            child: GalleryView(
              pickImages: picker ??
                  () async => const [
                        GalleryImageUpload(name: 'a.jpg', bytes: [1, 2]),
                      ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      return cubit;
    }

    testWidgets('grid renders with the primary badge', (tester) async {
      await pumpGallery(tester);

      expect(find.byKey(const Key('gallery-grid')), findsOneWidget);
      expect(find.byKey(const Key('gallery-image-g1')), findsOneWidget);
      expect(find.byKey(const Key('gallery-primary-g1')), findsOneWidget);
      expect(find.byKey(const Key('gallery-primary-g2')), findsNothing);
    });

    testWidgets('image sheet sets primary (only offered on non-primary)',
        (tester) async {
      await pumpGallery(tester);

      await tester.tap(find.byKey(const Key('gallery-image-g2')));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('gallery-set-primary')), findsOneWidget);

      await tester.tap(find.byKey(const Key('gallery-set-primary')));
      await tester.pumpAndSettle();
      verify(() => repository.setPrimaryGalleryImage('g2')).called(1);
    });

    testWidgets('delete requires confirmation', (tester) async {
      await pumpGallery(tester);

      await tester.tap(find.byKey(const Key('gallery-image-g1')));
      await tester.pumpAndSettle();
      // The primary image's sheet offers delete only.
      expect(find.byKey(const Key('gallery-set-primary')), findsNothing);
      await tester.tap(find.byKey(const Key('gallery-delete')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('gallery-remove-cancel')));
      await tester.pumpAndSettle();
      verifyNever(() => repository.removeGalleryImage(any()));

      await tester.tap(find.byKey(const Key('gallery-image-g1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('gallery-delete')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('gallery-remove-confirm')));
      await tester.pumpAndSettle();
      verify(() => repository.removeGalleryImage('g1')).called(1);
    });

    testWidgets('upload flows picked images to the repository',
        (tester) async {
      await pumpGallery(tester);

      await tester.tap(find.byKey(const Key('gallery-upload')));
      await tester.pumpAndSettle();

      final sent =
          verify(() => repository.uploadGalleryImages(captureAny()))
              .captured
              .single as List<GalleryImageUpload>;
      expect(sent.single.name, 'a.jpg');
      expect(find.text(AppStrings.galleryUploaded), findsOneWidget);
    });

    testWidgets('empty gallery invites upload', (tester) async {
      when(() => repository.fetchGallery())
          .thenAnswer((_) async => const Right([]));
      await pumpGallery(tester);

      expect(find.text(AppStrings.galleryEmpty), findsOneWidget);
      expect(find.text('+ ${AppStrings.galleryUpload}'), findsOneWidget);
    });
  });

  group('Holidays management (spec: provider-holidays-management)', () {
    final holiday = ProviderHoliday(
      id: 'h1',
      date: DateTime(2026, 8, 1),
      reason: 'مرخصی تابستانی',
      isRecurring: true,
    );

    setUp(() {
      when(() => repository.fetchHolidays())
          .thenAnswer((_) async => Right([holiday]));
      when(() => repository.addHoliday(
            date: any(named: 'date'),
            reason: any(named: 'reason'),
            isRecurring: any(named: 'isRecurring'),
          )).thenAnswer((_) async => const Right(null));
      when(() => repository.removeHoliday(any()))
          .thenAnswer((_) async => const Right(null));
    });

    test('cubit mutations reload on success', () async {
      final cubit = HolidaysCubit(repository);
      await cubit.load();
      clearInteractions(repository);
      when(() => repository.fetchHolidays())
          .thenAnswer((_) async => Right([holiday]));

      expect(
        await cubit.addHoliday(date: DateTime(2026, 9, 1), reason: 'عید'),
        isNull,
      );
      await Future<void>.delayed(Duration.zero);
      verify(() => repository.fetchHolidays()).called(1);
      await cubit.close();
    });

    Future<HolidaysCubit> pumpHolidays(WidgetTester tester) async {
      when(() => repository.fetchExceptions())
          .thenAnswer((_) async => const Right([]));
      final cubit = HolidaysCubit(repository);
      final exceptionsCubit = ExceptionsCubit(repository);
      addTearDown(cubit.close);
      addTearDown(exceptionsCubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: MultiBlocProvider(
            providers: [
              BlocProvider<HolidaysCubit>.value(value: cubit..load()),
              BlocProvider<ExceptionsCubit>.value(
                  value: exceptionsCubit..load()),
            ],
            child: const HolidaysView(),
          ),
        ),
      );
      await tester.pumpAndSettle();
      return cubit;
    }

    testWidgets('list leads with the green add row', (tester) async {
      await pumpHolidays(tester);

      expect(find.byKey(const Key('holiday-add-row')), findsOneWidget);
    });

    testWidgets('lists holidays with recurring badge; remove is confirmed',
        (tester) async {
      await pumpHolidays(tester);

      expect(find.byKey(const Key('holiday-row-h1')), findsOneWidget);
      expect(find.textContaining(AppStrings.holidayRecurringBadge),
          findsOneWidget);

      await tester.tap(find.byKey(const Key('holiday-remove-h1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('holiday-remove-cancel')));
      await tester.pumpAndSettle();
      verifyNever(() => repository.removeHoliday(any()));

      await tester.tap(find.byKey(const Key('holiday-remove-h1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('holiday-remove-confirm')));
      await tester.pumpAndSettle();
      verify(() => repository.removeHoliday('h1')).called(1);
    });

    testWidgets('add sheet gates on reason and submits', (tester) async {
      await pumpHolidays(tester);

      await tester.tap(find.byKey(const Key('holiday-add')));
      await tester.pumpAndSettle();

      final saveButton = tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('holiday-save')),
        matching: find.byType(FilledButton),
      ));
      expect(saveButton.onPressed, isNull); // gated on empty reason

      await tester.enterText(
          find.byKey(const Key('holiday-reason')), 'مرخصی');
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('holiday-save')));
      await tester.pumpAndSettle();

      verify(() => repository.addHoliday(
            date: any(named: 'date'),
            reason: 'مرخصی',
            isRecurring: false,
          )).called(1);
      expect(find.text(AppStrings.holidayAdded), findsOneWidget);
    });
  });

  group('Working hours editing (spec: provider-working-hours-editing)', () {
    const monday = DayHours(
      dayOfWeek: 1,
      isOpen: true,
      openTime: ClockTime(10, 0),
      closeTime: ClockTime(19, 0),
      breaks: [BreakTime(ClockTime(13, 0), ClockTime(14, 0))],
    );
    const tuesday = DayHours(dayOfWeek: 2, isOpen: false);

    test('GET shape parses and PUT shape serializes (round-trip)', () {
      final parsed = HomeApiService.parseBusinessHours({
        'businessHours': [
          {
            'dayOfWeek': 1,
            'dayName': 'Monday',
            'isOpen': true,
            'openTime': '10:00',
            'closeTime': '19:00',
            'breaks': [
              {'startTime': '13:00', 'endTime': '14:00'},
            ],
          },
          {'dayOfWeek': 2, 'isOpen': false, 'openTime': null},
        ],
      });

      expect(parsed, [monday, tuesday]);

      // Breaks survive serialization untouched (spec: break preservation).
      expect(HomeApiService.dayHoursToJson(monday), {
        'dayOfWeek': 1,
        'isOpen': true,
        'openTime': {'hours': 10, 'minutes': 0},
        'closeTime': {'hours': 19, 'minutes': 0},
        'breaks': [
          {
            'start': {'hours': 13, 'minutes': 0},
            'end': {'hours': 14, 'minutes': 0},
          },
        ],
      });
    });

    test('cubit edits days purely and save sends the whole week', () async {
      when(() => repository.fetchBusinessHours())
          .thenAnswer((_) async => const Right([monday, tuesday]));
      when(() => repository.updateBusinessHours(any()))
          .thenAnswer((_) async => const Right(null));

      final cubit = BusinessHoursCubit(repository);
      await cubit.load();

      cubit.toggleDay(2, true); // opening gets default 9–18
      expect(cubit.state.data![1].isOpen, isTrue);
      expect(cubit.state.data![1].openTime, const ClockTime(9, 0));

      cubit.setOpenTime(1, const ClockTime(11, 30));
      expect(cubit.state.data![0].openTime, const ClockTime(11, 30));
      expect(cubit.state.data![0].breaks, monday.breaks); // preserved

      expect(await cubit.save(), isNull);
      final sent = verify(() => repository.updateBusinessHours(captureAny()))
          .captured
          .single as List<DayHours>;
      expect(sent, hasLength(2));
      expect(sent[0].breaks, monday.breaks);
      await cubit.close();
    });

    test('breaks: add on open days, remove by index, closed-day no-op '
        '(spec: provider-break-editing)', () async {
      when(() => repository.fetchBusinessHours())
          .thenAnswer((_) async => const Right([monday, tuesday]));
      when(() => repository.updateBusinessHours(any()))
          .thenAnswer((_) async => const Right(null));
      final cubit = BusinessHoursCubit(repository);
      await cubit.load();

      const evening = BreakTime(ClockTime(16, 0), ClockTime(16, 30));
      cubit.addBreak(1, evening);
      expect(cubit.state.data![0].breaks, [...monday.breaks, evening]);

      cubit.addBreak(2, evening); // Tuesday is closed → no-op
      expect(cubit.state.data![1].breaks, isEmpty);

      cubit.removeBreak(1, 0); // drop the original lunch break
      expect(cubit.state.data![0].breaks, [evening]);

      await cubit.save();
      final sent = verify(() => repository.updateBusinessHours(captureAny()))
          .captured
          .single as List<DayHours>;
      expect(sent[0].breaks, [evening]); // save carries the edits exactly
      await cubit.close();
    });

    testWidgets('break chips are deletable and open days offer add-break',
        (tester) async {
      when(() => repository.fetchBusinessHours())
          .thenAnswer((_) async => const Right([monday, tuesday]));
      when(() => repository.updateBusinessHours(any()))
          .thenAnswer((_) async => const Right(null));

      final cubit = BusinessHoursCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<BusinessHoursCubit>.value(
            value: cubit..load(),
            child: const BusinessHoursView(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Open Monday: break chip + add affordance; closed Tuesday: neither.
      expect(find.byKey(const Key('hours-break-1-13:00')), findsOneWidget);
      expect(find.byKey(const Key('hours-add-break-1')), findsOneWidget);
      expect(find.byKey(const Key('hours-add-break-2')), findsNothing);

      // Chip delete removes the break from state (invoke the wired handler —
      // the delete glyph isn't hit-testable as a plain Icon descendant).
      final chip =
          tester.widget<Chip>(find.byKey(const Key('hours-break-1-13:00')));
      expect(chip.onDeleted, isNotNull);
      chip.onDeleted!();
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('hours-break-1-13:00')), findsNothing);
      expect(cubit.state.data![0].breaks, isEmpty);
    });

    testWidgets('editor renders Saturday-first, toggles, and saves',
        (tester) async {
      when(() => repository.fetchBusinessHours())
          .thenAnswer((_) async => const Right([monday, tuesday]));
      when(() => repository.updateBusinessHours(any()))
          .thenAnswer((_) async => const Right(null));

      final cubit = BusinessHoursCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<BusinessHoursCubit>.value(
            value: cubit..load(),
            child: const BusinessHoursView(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Monday shows its times + preserved break chip; Tuesday reads closed.
      expect(find.text('10:00'), findsOneWidget);
      expect(
          find.byKey(const Key('hours-break-1-13:00')), findsOneWidget);
      expect(find.text(AppStrings.hoursClosedDay), findsOneWidget);

      await tester.tap(find.byKey(const Key('hours-switch-2')));
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.hoursClosedDay), findsNothing);

      await tester.ensureVisible(find.byKey(const Key('hours-save')));
      await tester.tap(find.byKey(const Key('hours-save')));
      await tester.pumpAndSettle();

      final sent = verify(() => repository.updateBusinessHours(captureAny()))
          .captured
          .single as List<DayHours>;
      expect(sent.firstWhere((d) => d.dayOfWeek == 2).isOpen, isTrue);
    });

    testWidgets('save failure keeps the edited week on screen',
        (tester) async {
      when(() => repository.fetchBusinessHours())
          .thenAnswer((_) async => const Right([monday, tuesday]));
      when(() => repository.updateBusinessHours(any()))
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));

      final cubit = BusinessHoursCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<BusinessHoursCubit>.value(
            value: cubit..load(),
            child: const BusinessHoursView(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('hours-switch-2')));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('hours-save')));
      await tester.tap(find.byKey(const Key('hours-save')));
      await tester.pumpAndSettle();

      expect(find.text('خطا'), findsOneWidget); // snackbar
      // Edited week still on screen (Tuesday remains open).
      expect(find.text(AppStrings.hoursClosedDay), findsNothing);
    });
  });

  group('Staff management (spec: organization-membership)', () {
    const owner = OrgMember(
      membershipId: 'mem-owner',
      personId: 'p-owner',
      name: 'مالک سالن',
      roles: ['Owner', 'StaffProvider'],
      status: 'Active',
      isOwner: true,
      providesServices: true,
    );
    const staff = OrgMember(
      membershipId: 'mem-1',
      personId: 'p-1',
      name: 'سارا احمدی',
      phone: '09121112233',
      roles: ['StaffProvider'],
      status: 'Active',
      providesServices: true,
    );

    setUp(() {
      when(() => repository.fetchOrgMembers())
          .thenAnswer((_) async => const Right([owner, staff]));
      when(() => repository.inviteStaff(
            phoneNumber: any(named: 'phoneNumber'),
            inviteeName: any(named: 'inviteeName'),
          )).thenAnswer((_) async => const Right(null));
      when(() => repository.terminateMember(any()))
          .thenAnswer((_) async => const Right(null));
    });

    test('mutations reload on success and surface failures', () async {
      final cubit = StaffCubit(repository);
      await cubit.load();
      clearInteractions(repository);
      when(() => repository.fetchOrgMembers())
          .thenAnswer((_) async => const Right([owner, staff]));

      expect(await cubit.inviteStaff(phoneNumber: '09121110022'), isNull);
      await Future<void>.delayed(Duration.zero);
      verify(() => repository.fetchOrgMembers()).called(1);

      when(() => repository.terminateMember(any()))
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      final failure = await cubit.removeMember('mem-1');
      expect(failure!.message, 'خطا');
      await cubit.close();
    });

    Future<StaffCubit> pumpStaff(WidgetTester tester) async {
      final cubit = StaffCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<StaffCubit>.value(
            value: cubit..load(),
            child: const StaffView(),
          ),
        ),
      );
      await tester.pumpAndSettle();
      return cubit;
    }

    testWidgets('lists members; owner has a badge and no remove, staff can be removed',
        (tester) async {
      await pumpStaff(tester);

      expect(find.byKey(const Key('member-row-mem-owner')), findsOneWidget);
      expect(find.byKey(const Key('member-row-mem-1')), findsOneWidget);
      // Owner cannot be removed from here; a regular member can.
      expect(find.byKey(const Key('member-remove-mem-owner')), findsNothing);
      expect(find.byKey(const Key('member-remove-mem-1')), findsOneWidget);
    });

    testWidgets('the invite action is visible against the blue header',
        (tester) async {
      await pumpStaff(tester);

      final icon = tester.widget<Icon>(
        find.descendant(
          of: find.byKey(const Key('staff-invite')),
          matching: find.byType(Icon),
        ),
      );

      // Regression: this shipped as AppColors.primary (0xFF3777BF) on the
      // 0xFF3777C0 app bar — one step apart in the blue channel, so the only
      // route to inviting a team member was invisible and the feature looked
      // unbuilt. Add affordances on the chrome use the green accent, the rule
      // AppPageScaffold.actions documents and service-add/holiday-add follow.
      expect(icon.color, AppColors.success);
      expect(
        icon.color,
        isNot(AppColors.primary),
        reason: 'brand blue disappears against the blue header',
      );
    });

    testWidgets('a populated member list still offers the discoverable add row',
        (tester) async {
      await pumpStaff(tester);

      // Services and Holidays pin a green "+ add" row above a populated list
      // because the header icon alone proved easy to miss; Staff was the one
      // list that never got it, leaving invite reachable only from the
      // (then invisible) chrome icon.
      expect(find.byKey(const Key('staff-invite-row')), findsOneWidget);
      expect(find.byKey(const Key('member-row-mem-owner')), findsOneWidget,
          reason: 'the add row must not displace the members');
    });

    testWidgets('the add row opens the invite sheet', (tester) async {
      await pumpStaff(tester);

      // _AddLinkRow is Align-wrapped, so the row's box is wider than its
      // button and its centre misses the tap target — press the button itself.
      await tester.tap(find.descendant(
        of: find.byKey(const Key('staff-invite-row')),
        matching: find.byType(TextButton),
      ));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('invite-phone')), findsOneWidget);
    });

    testWidgets('invite flow: gated on a valid phone, submits and confirms',
        (tester) async {
      await pumpStaff(tester);

      await tester.tap(find.byKey(const Key('staff-invite')));
      await tester.pumpAndSettle();

      FilledButton sendButton() => tester.widget<FilledButton>(
            find.descendant(
              of: find.byKey(const Key('invite-send')),
              matching: find.byType(FilledButton),
            ),
          );

      // Gated until a valid Iranian mobile is entered.
      expect(sendButton().onPressed, isNull);

      await tester.enterText(
          find.byKey(const Key('invite-phone')), '09121110022');
      await tester.pumpAndSettle();
      expect(sendButton().onPressed, isNotNull);

      await tester.tap(find.byKey(const Key('invite-send')));
      await tester.pumpAndSettle();

      verify(() => repository.inviteStaff(
            phoneNumber: '09121110022',
            inviteeName: any(named: 'inviteeName'),
          )).called(1);
      expect(find.text(AppStrings.staffInviteSent), findsOneWidget);
    });

    testWidgets('remove requires confirmation; cancel terminates nothing',
        (tester) async {
      await pumpStaff(tester);

      await tester.tap(find.byKey(const Key('member-remove-mem-1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('member-remove-cancel')));
      await tester.pumpAndSettle();
      verifyNever(() => repository.terminateMember(any()));

      await tester.tap(find.byKey(const Key('member-remove-mem-1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('member-remove-confirm')));
      await tester.pumpAndSettle();

      verify(() => repository.terminateMember('mem-1')).called(1);
      expect(find.text(AppStrings.staffRemoved), findsOneWidget);
    });
  });

  group('My salons (memberships — S6)', () {
    const membership = ProviderMembership(
      membershipId: 'm-1',
      organizationId: 'org-1',
      organizationName: 'سالن رُز',
      roles: ['Owner', 'StaffProvider'],
      status: 'Active',
      providesServices: true,
    );

    test('MembershipsCubit: ready on success, failed with message on error',
        () async {
      when(() => repository.fetchMyMemberships())
          .thenAnswer((_) async => const Right([membership]));
      final cubit = MembershipsCubit(repository, _authRepo());
      await cubit.load();
      expect(cubit.state.status, MoreStatus.ready);
      expect(cubit.state.data!.single.organizationName, 'سالن رُز');
      expect(cubit.state.data!.single.isOwner, isTrue);

      when(() => repository.fetchMyMemberships())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      await cubit.load();
      expect(cubit.state.status, MoreStatus.failed);
      expect(cubit.state.error, 'خطا');
      await cubit.close();
    });

    testWidgets('lists memberships with roles and the owner/provider badges',
        (tester) async {
      when(() => repository.fetchMyMemberships())
          .thenAnswer((_) async => const Right([membership]));
      final cubit = MembershipsCubit(repository, _authRepo());
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<MembershipsCubit>.value(
            value: cubit..load(),
            child: const MyMembershipsView(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('membership-row-m-1')), findsOneWidget);
      expect(find.text('سالن رُز'), findsOneWidget);
      expect(
        find.text(
            '${AppStrings.membershipOwner} · ${AppStrings.membershipProvidesServices}'),
        findsOneWidget,
      );
    });

    testWidgets('empty state when the person has no memberships',
        (tester) async {
      when(() => repository.fetchMyMemberships())
          .thenAnswer((_) async => const Right(<ProviderMembership>[]));
      final cubit = MembershipsCubit(repository, _authRepo());
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<MembershipsCubit>.value(
            value: cubit..load(),
            child: const MyMembershipsView(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.membershipsEmpty), findsOneWidget);
    });
  });

  group('More sub-pages', () {
    Future<void> pumpView(WidgetTester tester, Widget view) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: view,
        ),
      );
      await tester.pumpAndSettle();
    }

    testWidgets('Insights shows the stat tiles', (tester) async {
      when(() => repository.fetchInsights()).thenAnswer(
        (_) async => const Right(InsightsSummary(
          totalBookings: 12,
          completedBookings: 7,
          cancelledBookings: 2,
          noShowBookings: 1,
          totalRevenue: 500000,
          completedRevenue: 250000,
          currency: 'IRR',
          bookingsTrailing30d: 5,
        )),
      );
      final cubit = InsightsCubit(repository);
      addTearDown(cubit.close);
      await pumpView(
        tester,
        BlocProvider<InsightsCubit>.value(
          value: cubit..load(),
          child: const InsightsView(),
        ),
      );

      expect(find.text('12'), findsOneWidget);
      expect(find.text('7'), findsOneWidget);
      expect(find.text('500000 IRR'), findsOneWidget);
      expect(find.text('5'), findsOneWidget);
    });

    testWidgets(
        'sub-pages wear the blue chrome with a white title '
        '(DESIGN_LANGUAGE.md §1) — not a white app bar', (tester) async {
      when(() => repository.fetchServices())
          .thenAnswer((_) async => const Right([]));
      final cubit = ServicesCubit(repository);
      addTearDown(cubit.close);
      await pumpView(
        tester,
        BlocProvider<ServicesCubit>.value(
          value: cubit..load(),
          child: const ServicesView(),
        ),
      );

      final scaffold = tester.widget<Scaffold>(find.byType(Scaffold).last);
      expect(scaffold.backgroundColor, AppColors.appBar,
          reason: 'the chrome behind the sheet must be brand blue');

      final title = tester.widget<Text>(find.text(AppStrings.moreServices));
      expect(title.style?.color, Colors.white,
          reason: 'title sits on blue, so it must be white');

      // Add actions use the green accent: brand blue would vanish on blue.
      final addIcon = tester.widget<Icon>(find.descendant(
        of: find.byKey(const Key('service-add')),
        matching: find.byType(Icon),
      ));
      expect(addIcon.color, AppColors.success);
    });

    testWidgets('Staff list marks pending (invited) members', (tester) async {
      when(() => repository.fetchOrgMembers()).thenAnswer(
        (_) async => const Right([
          OrgMember(
              membershipId: 'mem-1',
              name: 'سارا',
              roles: ['StaffProvider'],
              status: 'Active'),
          OrgMember(
              membershipId: 'mem-2',
              name: 'رضا',
              roles: ['StaffProvider'],
              status: 'Invited'),
        ]),
      );
      final cubit = StaffCubit(repository);
      addTearDown(cubit.close);
      await pumpView(
        tester,
        BlocProvider<StaffCubit>.value(
          value: cubit..load(),
          child: const StaffView(),
        ),
      );

      expect(find.byKey(const Key('member-row-mem-1')), findsOneWidget);
      expect(find.textContaining(AppStrings.staffInvitePending), findsOneWidget);
    });

    testWidgets('Services failure shows retry that reloads', (tester) async {
      when(() => repository.fetchServices())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      final cubit = ServicesCubit(repository);
      addTearDown(cubit.close);
      await pumpView(
        tester,
        BlocProvider<ServicesCubit>.value(
          value: cubit..load(),
          child: const ServicesView(),
        ),
      );

      expect(find.byKey(const Key('app-error-retry')), findsOneWidget);

      when(() => repository.fetchServices()).thenAnswer(
        (_) async => const Right([
          ComposerService(id: 's1', name: 'اصلاح', durationMinutes: 45),
        ]),
      );
      await tester.tap(find.byKey(const Key('app-error-retry')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('service-row-s1')), findsOneWidget);
    });
  });
}
