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
      // Coming-soon rows are visible but disabled.
      expect(find.text(AppStrings.comingSoon), findsNWidgets(3));
      final profileRow =
          tester.widget<InkWell>(find.byKey(const Key('more-profile')));
      expect(profileRow.onTap, isNull);

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
