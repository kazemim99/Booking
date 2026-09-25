import 'package:asan_rezerve_provider_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/steps/working_hours_step.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

void main() {
  late _MockRepo repo;

  setUp(() {
    repo = _MockRepo();
    when(() => repo.getDraft()).thenAnswer((_) async => const Right(null));
  });

  Future<OnboardingCubit> pumpStep(WidgetTester tester) async {
    // Use the real theme at a phone width: the app's OutlinedButton theme uses
    // an infinite-width min size, which crashed inline chips in a Row (blank
    // page). Pumping with AppTheme guards that regression.
    tester.view.physicalSize = const Size(400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final cubit = OnboardingCubit(repo);
    // Seeds 7 open days at 09:00–18:00 with no breaks.
    await cubit.init(phoneNumber: '09120000000');

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: BlocProvider.value(
            value: cubit,
            child: const Scaffold(body: WorkingHoursStep()),
          ),
        ),
      ),
    );
    await tester.pump();
    expect(tester.takeException(), isNull);
    return cubit;
  }

  testWidgets('open day shows no break by default', (tester) async {
    final cubit = await pumpStep(tester);

    expect(cubit.state.data.businessHours.first.breaks, isEmpty);
    expect(find.byKey(const Key('break-0-0')), findsNothing);
    // The add-break affordance is present for an open day.
    expect(find.byKey(const Key('add-break-0')), findsOneWidget);
  });

  testWidgets('adding a break appends one and rendering a row', (tester) async {
    final cubit = await pumpStep(tester);

    await tester.tap(find.byKey(const Key('add-break-0')));
    await tester.pump();

    expect(cubit.state.data.businessHours.first.breaks, hasLength(1));
    expect(find.byKey(const Key('break-0-0')), findsOneWidget);
  });

  testWidgets('removing a break drops it again', (tester) async {
    final cubit = await pumpStep(tester);

    await tester.tap(find.byKey(const Key('add-break-0')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('remove-break-0-0')));
    await tester.pump();

    expect(cubit.state.data.businessHours.first.breaks, isEmpty);
    expect(find.byKey(const Key('break-0-0')), findsNothing);
  });

  testWidgets('closing a day hides its time + breaks controls', (tester) async {
    final cubit = await pumpStep(tester);

    await tester.tap(find.byKey(const Key('day-toggle-0')));
    await tester.pump();

    expect(cubit.state.data.businessHours.first.isOpen, isFalse);
    expect(find.byKey(const Key('add-break-0')), findsNothing);
    expect(find.byKey(const Key('open-0')), findsNothing);
  });

  testWidgets('copy-to-all-days propagates the day schedule', (tester) async {
    final cubit = await pumpStep(tester);

    // Give day 0 a break, then copy everything to the other days.
    await tester.tap(find.byKey(const Key('add-break-0')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('copy-0')));
    await tester.pump();

    final hours = cubit.state.data.businessHours;
    expect(hours.every((d) => d.breaks.length == 1), isTrue);
    expect(hours.every((d) => d.isOpen), isTrue);
  });

  group('a range that ends before it starts is flagged where it is made', () {
    // The picker let a day close before it opened and only complained when the
    // user pressed Next, by which point the offending day was scrolled away
    // (user report, 2026-09-19). The API refuses such a schedule too.
    testWidgets('the day says so under its own row', (tester) async {
      final cubit = await pumpStep(tester);
      // init() seeds 7 days asynchronously; let that finish or it overwrites this.
      await tester.pumpAndSettle();
      cubit.setBusinessHours([
        for (final day in cubit.state.data.businessHours)
          if (day.dayOfWeek != 1)
            day
          else
            day.copyWith(
              isOpen: true,
              openTime: const ClockTime(18, 0),
              closeTime: const ClockTime(9, 0),
              breaks: const [],
            ),
      ]);

      // Two frames: the cubit's emit reaches the builder on the next one.
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('day-error-1')), findsOneWidget);
      expect(
        find.text(AppStrings.closeAfterOpenError(AppStrings.weekDays[1])),
        findsOneWidget,
      );
    });

    testWidgets('a break that ends before it starts says so too', (
      tester,
    ) async {
      final cubit = await pumpStep(tester);
      // init() seeds 7 days asynchronously; let that finish or it overwrites this.
      await tester.pumpAndSettle();
      cubit.setBusinessHours([
        for (final day in cubit.state.data.businessHours)
          if (day.dayOfWeek != 1)
            day
          else
            day.copyWith(
              isOpen: true,
              openTime: const ClockTime(9, 0),
              closeTime: const ClockTime(18, 0),
              breaks: const [BreakTime(ClockTime(13, 0), ClockTime(12, 0))],
            ),
      ]);

      // Two frames: the cubit's emit reaches the builder on the next one.
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('day-error-1')), findsOneWidget);
    });

    testWidgets('a sound day shows no error', (tester) async {
      final cubit = await pumpStep(tester);
      // init() seeds 7 days asynchronously; let that finish or it overwrites this.
      await tester.pumpAndSettle();
      cubit.setBusinessHours([
        for (final day in cubit.state.data.businessHours)
          if (day.dayOfWeek != 1)
            day
          else
            day.copyWith(
              isOpen: true,
              openTime: const ClockTime(9, 0),
              closeTime: const ClockTime(18, 0),
              breaks: const [BreakTime(ClockTime(12, 0), ClockTime(13, 0))],
            ),
      ]);

      // Two frames: the cubit's emit reaches the builder on the next one.
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('day-error-1')), findsNothing);
    });
  });
}
