import 'dart:async';

import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/home/domain/entities/composer_models.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/composer_cubit.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockHomeRepository extends Mock implements HomeRepository {}

void main() {
  late MockHomeRepository repository;

  const service1 = ComposerService(id: 's1', name: 'اصلاح مو', durationMinutes: 45);
  const service2 = ComposerService(id: 's2', name: 'رنگ مو', durationMinutes: 90);
  const staff1 = ComposerStaff(id: 'st1', name: 'سارا');
  final day = DateTime(2026, 7, 15);
  final slotA = DateTime(2026, 7, 15, 10);
  final slotB = DateTime(2026, 7, 15, 11);

  setUp(() {
    repository = MockHomeRepository();
    when(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
        )).thenAnswer((_) async => Right([slotA, slotB]));
  });

  ComposerCubit build({List<ComposerService>? services}) {
    when(() => repository.fetchComposerCatalog()).thenAnswer(
      (_) async => Right(ComposerCatalog(
        services: services ?? const [service1, service2],
        staff: const [staff1],
      )),
    );
    return ComposerCubit(repository, now: () => day);
  }

  test('initialDate seeds the composed day (calendar-initiated)', () async {
    when(() => repository.fetchComposerCatalog()).thenAnswer(
      (_) async => const Right(
          ComposerCatalog(services: [service1], staff: [staff1])),
    );
    final cubit = ComposerCubit(repository,
        now: () => day, initialDate: DateTime(2026, 7, 20, 15));
    await cubit.load();
    await Future<void>.delayed(Duration.zero);

    expect(cubit.state.date, DateTime(2026, 7, 20)); // date-only
    verify(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: DateTime(2026, 7, 20),
          staffId: any(named: 'staffId'),
        )).called(1);
    await cubit.close();
  });

  test('load: single options pre-select and slots fetch for them', () async {
    final cubit = build(services: const [service1]);
    await cubit.load();
    await Future<void>.delayed(Duration.zero);

    expect(cubit.state.status, ComposerStatus.ready);
    expect(cubit.state.service, service1);
    expect(cubit.state.staff, staff1); // single staff pre-selected
    expect(cubit.state.slotsStatus, SlotsStatus.ready);
    expect(cubit.state.slots, [slotA, slotB]);
    await cubit.close();
  });

  test('load: multiple services stay unselected (no slots fetch)', () async {
    final cubit = build();
    await cubit.load();

    expect(cubit.state.service, isNull);
    expect(cubit.state.slotsStatus, SlotsStatus.idle);
    verifyNever(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
        ));
    await cubit.close();
  });

  blocTest<ComposerCubit, ComposerState>(
    'load failure → failed with message',
    build: () {
      when(() => repository.fetchComposerCatalog()).thenAnswer(
          (_) async => const Left(ServerFailure('خطا')));
      return ComposerCubit(repository, now: () => day);
    },
    act: (c) => c.load(),
    verify: (c) {
      expect(c.state.status, ComposerStatus.failed);
      expect(c.state.error, 'خطا');
    },
  );

  test('changing selection clears a slot that no longer exists', () async {
    final cubit = build();
    await cubit.load();
    cubit.selectService(service1);
    await Future<void>.delayed(Duration.zero);
    cubit.selectSlot(slotA);
    expect(cubit.state.slot, slotA);

    // New selection returns only slotB → slotA selection is cleared.
    when(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
        )).thenAnswer((_) async => Right([slotB]));
    cubit.selectDate(DateTime(2026, 7, 16));
    await Future<void>.delayed(Duration.zero);

    expect(cubit.state.slots, [slotB]);
    expect(cubit.state.slot, isNull);
    await cubit.close();
  });

  test('stale slots response is discarded (spec: race guard)', () async {
    final cubit = build();
    await cubit.load();

    final slow = Completer<Either<Failure, List<DateTime>>>();
    when(() => repository.fetchAvailableSlots(
          serviceId: 's1',
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
        )).thenAnswer((_) => slow.future);
    when(() => repository.fetchAvailableSlots(
          serviceId: 's2',
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
        )).thenAnswer((_) async => Right([slotB]));

    cubit.selectService(service1); // slow request in flight
    cubit.selectService(service2); // fast request wins
    await Future<void>.delayed(Duration.zero);
    expect(cubit.state.slots, [slotB]);

    slow.complete(Right([slotA])); // stale — must be discarded
    await Future<void>.delayed(Duration.zero);
    expect(cubit.state.slots, [slotB]);
    expect(cubit.state.slotsStatus, SlotsStatus.ready);
    await cubit.close();
  });

  test('submit is gated until service+staff+slot selected', () async {
    final cubit = build();
    await cubit.load();

    await cubit.submit();
    verifyNever(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
        ));
    await cubit.close();
  });

  test('submit success → submitted; failure preserves selections', () async {
    final cubit = build();
    await cubit.load();
    cubit.selectService(service1);
    cubit.selectStaff(staff1);
    await Future<void>.delayed(Duration.zero);
    cubit.selectSlot(slotA);

    when(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
        )).thenAnswer((_) async => const Left(ServerFailure('ثبت ناموفق')));
    await cubit.submit(clientName: 'رضا');

    expect(cubit.state.submitted, isFalse);
    expect(cubit.state.error, 'ثبت ناموفق');
    // Selections preserved for retry (spec).
    expect(cubit.state.service, service1);
    expect(cubit.state.slot, slotA);

    when(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
        )).thenAnswer((_) async => const Right(null));
    await cubit.submit(clientName: 'رضا', clientPhone: '0912');

    expect(cubit.state.submitted, isTrue);
    verify(() => repository.createBooking(
          serviceId: 's1',
          staffId: 'st1',
          startTime: slotA,
          clientName: 'رضا',
          clientPhone: '0912',
          notes: any(named: 'notes'),
        )).called(1);
    await cubit.close();
  });
}
