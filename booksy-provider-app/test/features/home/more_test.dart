import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
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
      when(() => repository.fetchStaff()).thenAnswer(
        (_) async => const Right([
          ProviderStaffMember(id: 'm1', name: 'سارا', role: 'Stylist'),
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

    testWidgets('logout dispatches LogoutRequested', (tester) async {
      await pump(tester);

      await tester.scrollUntilVisible(
          find.byKey(const Key('more-logout')), 200);
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

  group('Staff management (spec: provider-staff-management)', () {
    const member = ProviderStaffMember(
      id: 'm1',
      name: 'سارا احمدی',
      firstName: 'سارا',
      lastName: 'احمدی',
      phone: '0912',
      role: 'Stylist',
    );

    setUp(() {
      when(() => repository.fetchStaff())
          .thenAnswer((_) async => const Right([member]));
      when(() => repository.addStaff(
            firstName: any(named: 'firstName'),
            lastName: any(named: 'lastName'),
            phoneNumber: any(named: 'phoneNumber'),
            role: any(named: 'role'),
          )).thenAnswer((_) async => const Right(null));
      when(() => repository.updateStaff(
            any(),
            firstName: any(named: 'firstName'),
            lastName: any(named: 'lastName'),
            phoneNumber: any(named: 'phoneNumber'),
            role: any(named: 'role'),
          )).thenAnswer((_) async => const Right(null));
      when(() => repository.removeStaff(any()))
          .thenAnswer((_) async => const Right(null));
    });

    test('mutations reload on success and surface failures', () async {
      final cubit = StaffCubit(repository);
      await cubit.load();
      clearInteractions(repository);
      when(() => repository.fetchStaff())
          .thenAnswer((_) async => const Right([member]));

      expect(await cubit.addStaff(firstName: 'رضا'), isNull);
      await Future<void>.delayed(Duration.zero);
      verify(() => repository.fetchStaff()).called(1);

      when(() => repository.removeStaff(any()))
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      final failure = await cubit.removeStaff('m1');
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

    testWidgets('add flow: gated on first name, submits and refreshes',
        (tester) async {
      await pumpStaff(tester);

      await tester.tap(find.byKey(const Key('staff-add')));
      await tester.pumpAndSettle();

      // Gated while the required name is empty.
      final saveButton =
          tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('staff-save')),
        matching: find.byType(FilledButton),
      ));
      expect(saveButton.onPressed, isNull);

      await tester.enterText(
          find.byKey(const Key('staff-first-name')), 'رضا');
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('staff-save')));
      await tester.pumpAndSettle();

      verify(() => repository.addStaff(
            firstName: 'رضا',
            lastName: any(named: 'lastName'),
            phoneNumber: any(named: 'phoneNumber'),
            role: any(named: 'role'),
          )).called(1);
      expect(find.text(AppStrings.staffAdded), findsOneWidget);
    });

    testWidgets('edit flow: pre-filled form updates the member',
        (tester) async {
      await pumpStaff(tester);

      await tester.tap(find.byKey(const Key('staff-row-m1')));
      await tester.pumpAndSettle();

      expect(find.text('سارا'), findsOneWidget); // pre-filled
      await tester.enterText(find.byKey(const Key('staff-role')), 'Barber');
      await tester.tap(find.byKey(const Key('staff-save')));
      await tester.pumpAndSettle();

      verify(() => repository.updateStaff(
            'm1',
            firstName: 'سارا',
            lastName: any(named: 'lastName'),
            phoneNumber: any(named: 'phoneNumber'),
            role: 'Barber',
          )).called(1);
    });

    testWidgets('remove requires confirmation; cancel deletes nothing',
        (tester) async {
      await pumpStaff(tester);

      await tester.tap(find.byKey(const Key('staff-row-m1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('staff-remove')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('staff-remove-cancel')));
      await tester.pumpAndSettle();
      verifyNever(() => repository.removeStaff(any()));

      await tester.tap(find.byKey(const Key('staff-remove')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('staff-remove-confirm')));
      await tester.pumpAndSettle();

      verify(() => repository.removeStaff('m1')).called(1);
      expect(find.text(AppStrings.staffRemoved), findsOneWidget);
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

    testWidgets('Staff list marks inactive members', (tester) async {
      when(() => repository.fetchStaff()).thenAnswer(
        (_) async => const Right([
          ProviderStaffMember(id: 'm1', name: 'سارا', role: 'Stylist'),
          ProviderStaffMember(
              id: 'm2', name: 'رضا', role: 'Barber', isActive: false),
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

      expect(find.byKey(const Key('staff-row-m1')), findsOneWidget);
      expect(find.textContaining(AppStrings.staffInactive), findsOneWidget);
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
