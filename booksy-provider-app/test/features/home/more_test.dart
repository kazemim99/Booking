import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:booksy_provider_app/features/home/domain/entities/composer_models.dart';
import 'package:booksy_provider_app/features/home/domain/entities/more_models.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/more_cubits.dart';
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
      // Remaining coming-soon rows (hours, gallery) are visible but disabled.
      expect(find.text(AppStrings.comingSoon), findsNWidgets(2));
      final hoursRow =
          tester.widget<InkWell>(find.byKey(const Key('more-hours')));
      expect(hoursRow.onTap, isNull);
      // The business-profile row is now live.
      final profileRow =
          tester.widget<InkWell>(find.byKey(const Key('more-profile')));
      expect(profileRow.onTap, isNotNull);

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
