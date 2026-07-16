import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/home/domain/entities/composer_models.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/composer_cubit.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/booking_composer_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockHomeRepository extends Mock implements HomeRepository {}

void main() {
  late MockHomeRepository repository;

  const service = ComposerService(id: 's1', name: 'اصلاح مو', durationMinutes: 45);
  const staff = ComposerStaff(id: 'st1', name: 'سارا');
  final day = DateTime(2026, 7, 15);
  final slot = DateTime(2026, 7, 15, 10);

  setUp(() {
    repository = MockHomeRepository();
    when(() => repository.fetchComposerCatalog()).thenAnswer(
      (_) async => const Right(
          ComposerCatalog(services: [service], staff: [staff])),
    );
    when(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
        )).thenAnswer((_) async => Right([slot]));
    when(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
        )).thenAnswer((_) async => const Right(null));
  });

  /// Pumps a stub home that pushes the composer as a real route so the
  /// pop-result (`true` on creation) is observable. Real theme (button
  /// footgun) + RTL.
  Future<bool?Function()> pumpComposer(WidgetTester tester) async {
    bool? result;
    final cubit = ComposerCubit(repository, now: () => day);
    addTearDown(cubit.close);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                key: const Key('open-composer'),
                onPressed: () async {
                  result = await Navigator.of(context).push<bool>(
                    MaterialPageRoute(
                      builder: (_) => BlocProvider.value(
                        value: cubit..load(),
                        child: const ComposerView(),
                      ),
                    ),
                  );
                },
                child: const Text('open'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.byKey(const Key('open-composer')));
    await tester.pumpAndSettle();
    return () => result;
  }

  testWidgets('single service+staff pre-select and slots render (RTL, real theme)',
      (tester) async {
    await pumpComposer(tester);

    // Pre-selected single options show their names.
    expect(find.text('اصلاح مو'), findsOneWidget);
    expect(find.text('سارا'), findsOneWidget);
    // Slot chip rendered from the live fetch.
    expect(find.byKey(const Key('slot-1000')), findsOneWidget);
  });

  testWidgets('submit is gated until a slot is chosen, then creates and pops true',
      (tester) async {
    final result = await pumpComposer(tester);

    // Gated: no slot selected yet.
    FilledButton button() => tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('composer-submit')),
        matching: find.byType(FilledButton)));
    expect(button().onPressed, isNull);

    // Choose the slot → submit enables.
    await tester.tap(find.byKey(const Key('slot-1000')));
    await tester.pumpAndSettle();
    expect(button().onPressed, isNotNull);

    // Fill the walk-in client and submit.
    await tester.enterText(
        find.byKey(const Key('composer-client-name')), 'رضا کریمی');
    await tester.tap(find.byKey(const Key('composer-submit')));
    await tester.pumpAndSettle();

    verify(() => repository.createBooking(
          serviceId: 's1',
          staffId: 'st1',
          startTime: slot,
          clientName: 'رضا کریمی',
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
        )).called(1);
    expect(result(), isTrue); // popped with true → Home refreshes
  });

  testWidgets('book-again prefill seeds the walk-in fields', (tester) async {
    final cubit = ComposerCubit(repository, now: () => day);
    addTearDown(cubit.close);
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: BlocProvider.value(
            value: cubit..load(),
            child: const ComposerView(
              initialClientName: 'مینا رستمی',
              initialClientPhone: '+989157330950',
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('مینا رستمی'), findsOneWidget);
    expect(find.text('+989157330950'), findsOneWidget);
  });

  testWidgets('no slots for the day shows the plain empty message',
      (tester) async {
    when(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
        )).thenAnswer((_) async => const Right([]));

    await pumpComposer(tester);

    expect(find.byKey(const Key('composer-no-slots')), findsOneWidget);
    expect(find.text(AppStrings.composerNoSlots), findsOneWidget);
  });

  testWidgets('creation failure keeps the composer open with selections',
      (tester) async {
    when(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
        )).thenAnswer(
            (_) async => const Left(ServerFailure('ثبت نوبت ناموفق بود')));

    final result = await pumpComposer(tester);
    await tester.tap(find.byKey(const Key('slot-1000')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('composer-submit')));
    await tester.pumpAndSettle();

    // Still on the composer, selection intact, error surfaced.
    expect(result(), isNull);
    expect(find.byKey(const Key('composer-submit')), findsOneWidget);
    expect(find.text('ثبت نوبت ناموفق بود'), findsOneWidget);
  });
}
