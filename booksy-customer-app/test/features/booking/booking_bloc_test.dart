import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/booking/presentation/bloc/booking_bloc.dart';

const _service = ServiceItem(
  id: 's1',
  name: 'کوتاهی مو',
  price: 250000,
  currency: 'تومان',
  durationMinutes: 45,
);

/// A second service so a visit can bundle two (cut + colour): the totals and
/// the slot length must both grow.
const _service2 = ServiceItem(
  id: 's2',
  name: 'رنگ مو',
  price: 500000,
  currency: 'تومان',
  durationMinutes: 90,
);

const _staffA = StaffMember(id: 'st1', name: 'مریم احمدی', isActive: true);
const _staffB = StaffMember(id: 'st2', name: 'سارا رضایی', isActive: true);

TimeSlot _slot(int hour, {String? staffId = 'st1'}) => TimeSlot(
      startTime: DateTime(2026, 8, 1, hour),
      endTime: DateTime(2026, 8, 1, hour, 45),
      durationMinutes: 45,
      isAvailable: true,
      staffId: staffId,
    );

ProviderDetail _provider({
  List<StaffMember> staff = const [_staffA],
  List<ServiceItem> services = const [_service, _service2],
  List<BusinessHour> businessHours = const [],
  int maxAdvanceBookingDays = 7,
}) =>
    ProviderDetail(
      id: 'p1',
      businessName: 'سالن نمونه',
      averageRating: 4.8,
      totalReviews: 12,
      maxAdvanceBookingDays: maxAdvanceBookingDays,
      businessHours: businessHours,
      services: services,
      staff: staff,
    );

class FakeBookingRepository implements BookingRepository {
  ProviderDetail provider;
  Either<Failure, DaySlots> slotsResult;
  List<Either<Failure, String>> createResults;
  int createCalls = 0;
  int slotsCalls = 0;
  String? lastStaffProviderId;

  /// What the last create/slots call put on the wire, so the tests can assert
  /// that every selected service reached the backend.
  String? lastCreateServiceId;
  List<String>? lastCreateServiceIds;
  String? lastSlotsServiceId;
  List<String>? lastSlotsServiceIds;

  /// When set, each day answers from here by its day of the month, and a day
  /// missing from the map is empty — how the first-free-day search is driven.
  Map<int, DaySlots>? slotsByDay;

  /// When set, a staff member's days answer from here (by staff id, then day of the month) ahead of
  /// [slotsByDay]; a day missing from that staff member's map is empty.
  Map<String, Map<int, DaySlots>>? slotsByStaff;

  /// Every day asked for, in order.
  final List<DateTime> slotsDates = [];

  /// The staff id sent with each slots request, in order.
  final List<String?> slotsStaffIds = [];

  /// Holds a day's answer back until the test releases it (stale-result cover).
  Map<int, Completer<void>> gates = {};

  FakeBookingRepository({
    required this.provider,
    required this.slotsResult,
    required this.createResults,
    this.slotsByDay,
  });

  @override
  Future<Either<Failure, ProviderDetail>> getProviderDetail(String id) async =>
      Right(provider);

  @override
  Future<Either<Failure, DaySlots>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
    List<String>? serviceIds,
  }) async {
    slotsCalls++;
    slotsDates.add(date);
    slotsStaffIds.add(staffId);
    lastSlotsServiceId = serviceId;
    lastSlotsServiceIds = serviceIds;
    final gate = gates[date.day];
    if (gate != null) await gate.future;
    final byStaff = slotsByStaff?[staffId];
    if (byStaff != null) return Right(byStaff[date.day] ?? const DaySlots());
    final byDay = slotsByDay;
    if (byDay != null) return Right(byDay[date.day] ?? const DaySlots());
    return slotsResult;
  }

  @override
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
    List<String>? serviceIds,
  }) async {
    lastStaffProviderId = staffProviderId;
    lastCreateServiceId = serviceId;
    lastCreateServiceIds = serviceIds;
    return createResults[createCalls++];
  }
}

Future<void> _pump() => Future<void>.delayed(const Duration(milliseconds: 20));

/// Picks a single service and leaves the step — the shape of the old
/// one-service-per-booking flow, now expressed as toggle + confirm.
Future<void> _pickServices(
  BookingBloc bloc,
  List<ServiceItem> services,
) async {
  for (final service in services) {
    bloc.add(BookingServiceToggled(service));
  }
  await _pump();
  bloc.add(const BookingServicesConfirmed());
  await _pump();
}

void main() {
  group('BookingBloc', () {
    test('complete flow: service → slot → confirm creates the booking',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10), _slot(11)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);

      bloc.add(const BookingStarted('p1'));
      await _pump();
      await _pickServices(bloc, [_service]);
      expect(bloc.state.slotsStatus, SlotsStatus.loaded);

      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();
      expect(bloc.state.step, BookingStep.confirm);

      bloc.add(const BookingSubmitted());
      await _pump();
      expect(bloc.state.submitStatus, SubmitStatus.success);
      expect(bloc.state.bookingId, 'b1');
      await bloc.close();
    });

    test('single-staff provider skips the staff step and assigns that staff',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(staff: const [_staffA]),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);

      bloc.add(const BookingStarted('p1'));
      await _pump();
      await _pickServices(bloc, [_service]);

      expect(bloc.state.step, BookingStep.time);
      expect(bloc.state.visibleSteps, isNot(contains(BookingStep.staff)));
      expect(bloc.state.staff, _staffA);
      await bloc.close();
    });

    test('multi-staff provider shows the staff step', () async {
      final repo = FakeBookingRepository(
        provider: _provider(staff: const [_staffA, _staffB]),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);

      bloc.add(const BookingStarted('p1'));
      await _pump();
      await _pickServices(bloc, [_service]);

      expect(bloc.state.step, BookingStep.staff);
      expect(bloc.state.visibleSteps, contains(BookingStep.staff));
      await bloc.close();
    });

    test('back navigation preserves earlier selections', () async {
      final repo = FakeBookingRepository(
        provider: _provider(staff: const [_staffA, _staffB]),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);

      bloc.add(const BookingStarted('p1'));
      await _pump();
      await _pickServices(bloc, [_service]);
      bloc.add(const BookingStaffSelected(_staffB));
      await _pump();

      bloc.add(const BookingStepBack());
      await _pump();
      expect(bloc.state.step, BookingStep.staff);
      bloc.add(const BookingStepBack());
      await _pump();
      expect(bloc.state.step, BookingStep.service);

      // Selections made earlier are still there.
      expect(bloc.state.services, [_service]);
      expect(bloc.state.staff, _staffB);
      await bloc.close();
    });

    test(
        'slot-taken failure returns to the time step with refreshed slots '
        'and keeps selections', () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10), _slot(11)])),
        createResults: [
          const Left(SlotTakenFailure('گرفته شد')),
          const Right('b2'),
        ],
      );
      final bloc = BookingBloc(repo);

      bloc.add(const BookingStarted('p1'));
      await _pump();
      await _pickServices(bloc, [_service]);
      final slotsCallsBefore = repo.slotsCalls;

      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();
      bloc.add(const BookingSubmitted());
      await _pump();

      expect(bloc.state.step, BookingStep.time);
      expect(bloc.state.slot, isNull);
      expect(bloc.state.services, [_service]);
      expect(repo.slotsCalls, greaterThan(slotsCallsBefore));

      // Recovery: pick another slot and succeed.
      bloc.add(BookingSlotSelected(bloc.state.slots.last));
      await _pump();
      bloc.add(const BookingSubmitted());
      await _pump();
      expect(bloc.state.submitStatus, SubmitStatus.success);
      await bloc.close();
    });

    test('"any staff" uses the slot\'s assigned staff id on create', () async {
      final repo = FakeBookingRepository(
        provider: _provider(staff: const [_staffA, _staffB]),
        slotsResult: Right(DaySlots(slots: [_slot(10, staffId: 'st2')])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);

      bloc.add(const BookingStarted('p1'));
      await _pump();
      await _pickServices(bloc, [_service]);
      bloc.add(const BookingStaffSelected(null)); // فرقی نمی‌کند
      await _pump();
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();
      bloc.add(const BookingSubmitted());
      await _pump();

      expect(repo.lastStaffProviderId, 'st2');
      await bloc.close();
    });

    test('re-entering the flow for the same provider keeps selections',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);

      bloc.add(const BookingStarted('p1'));
      await _pump();
      await _pickServices(bloc, [_service]);
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();

      // Login round-trip re-enters the flow route.
      bloc.add(const BookingStarted('p1'));
      await _pump();

      expect(bloc.state.step, BookingStep.confirm);
      expect(bloc.state.services, [_service]);
      expect(bloc.state.slot, isNotNull);
      await bloc.close();
    });
  });

  // A visit may bundle several services (cut + colour). The totals the customer
  // sees and the slot length asked of the backend must both follow the set.
  group('BookingBloc multi-service selection', () {
    late FakeBookingRepository repo;
    late BookingBloc bloc;

    Future<BookingBloc> started() async {
      repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10), _slot(11)])),
        createResults: [const Right('b1')],
      );
      bloc = BookingBloc(repo);
      bloc.add(const BookingStarted('p1'));
      await _pump();
      return bloc;
    }

    tearDown(() => bloc.close());

    test('toggling two services keeps both, in selection order', () async {
      final bloc = await started();

      bloc.add(const BookingServiceToggled(_service));
      bloc.add(const BookingServiceToggled(_service2));
      await _pump();

      expect(bloc.state.services, [_service, _service2]);
      expect(bloc.state.selectedServiceIds, ['s1', 's2']);
      expect(bloc.state.hasServices, isTrue);
      expect(bloc.state.isServiceSelected(_service), isTrue);
      expect(bloc.state.isServiceSelected(_service2), isTrue);
    });

    test('totals are the sums over the selected services', () async {
      final bloc = await started();

      bloc.add(const BookingServiceToggled(_service));
      await _pump();
      expect(bloc.state.totalDurationMinutes, 45);
      expect(bloc.state.totalPrice, 250000);

      bloc.add(const BookingServiceToggled(_service2));
      await _pump();
      expect(bloc.state.totalDurationMinutes, 135, reason: '45 + 90');
      expect(bloc.state.totalPrice, 750000, reason: '250000 + 500000');
      expect(bloc.state.currency, 'تومان');
    });

    test('toggling a selected service again deselects it and drops its totals',
        () async {
      final bloc = await started();

      bloc.add(const BookingServiceToggled(_service));
      bloc.add(const BookingServiceToggled(_service2));
      await _pump();

      bloc.add(const BookingServiceToggled(_service));
      await _pump();

      expect(bloc.state.services, [_service2]);
      expect(bloc.state.isServiceSelected(_service), isFalse);
      expect(bloc.state.totalDurationMinutes, 90);
      expect(bloc.state.totalPrice, 500000);
    });

    test('deselecting the last service empties the visit and zeroes the totals',
        () async {
      final bloc = await started();

      bloc.add(const BookingServiceToggled(_service));
      await _pump();
      bloc.add(const BookingServiceToggled(_service));
      await _pump();

      expect(bloc.state.services, isEmpty);
      expect(bloc.state.hasServices, isFalse);
      expect(bloc.state.totalDurationMinutes, 0);
      expect(bloc.state.totalPrice, 0);
      expect(bloc.state.currency, '');
    });

    test('confirming with zero services does not leave the service step',
        () async {
      final bloc = await started();
      final slotsCallsBefore = repo.slotsCalls;

      bloc.add(const BookingServicesConfirmed());
      await _pump();

      expect(bloc.state.step, BookingStep.service);
      expect(repo.slotsCalls, slotsCallsBefore,
          reason: 'no availability query without a service');
    });

    test('confirming with a selection leaves the service step', () async {
      final bloc = await started();

      bloc.add(const BookingServiceToggled(_service));
      await _pump();
      bloc.add(const BookingServicesConfirmed());
      await _pump();

      expect(bloc.state.step, BookingStep.time);
    });

    test('toggling does not fetch slots — only confirming does', () async {
      final bloc = await started();

      bloc.add(const BookingServiceToggled(_service));
      bloc.add(const BookingServiceToggled(_service2));
      await _pump();
      expect(repo.slotsCalls, 0);
      expect(bloc.state.slotsStatus, SlotsStatus.initial);

      bloc.add(const BookingServicesConfirmed());
      await _pump();
      expect(repo.slotsCalls, 1);
    });

    test('changing the selection invalidates already-loaded slots', () async {
      final bloc = await started();

      await _pickServices(bloc, [_service]);
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();
      expect(bloc.state.slots, isNotEmpty);

      // Back to the services step and add a second service: the loaded slots
      // are sized for 45 minutes and would no longer fit the visit.
      bloc.add(const BookingStepBack());
      await _pump();
      bloc.add(const BookingServiceToggled(_service2));
      await _pump();

      expect(bloc.state.slots, isEmpty);
      expect(bloc.state.slot, isNull);
      expect(bloc.state.slotsStatus, SlotsStatus.initial);
    });

    test('the availability query carries every selected service', () async {
      final bloc = await started();

      await _pickServices(bloc, [_service, _service2]);

      expect(bloc.state.slotsStatus, SlotsStatus.loaded);
      expect(repo.lastSlotsServiceIds, ['s1', 's2'],
          reason: 'slot length must cover the combined duration');
      expect(repo.lastSlotsServiceId, 's1',
          reason: 'the required single id is the first selected service');
    });

    test('the create request carries every selected service', () async {
      final bloc = await started();

      await _pickServices(bloc, [_service, _service2]);
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();
      bloc.add(const BookingSubmitted());
      await _pump();

      expect(bloc.state.submitStatus, SubmitStatus.success);
      expect(repo.lastCreateServiceIds, ['s1', 's2']);
      expect(repo.lastCreateServiceId, 's1');
    });

    test('emptying the selection blocks submission', () async {
      final bloc = await started();

      // Reach the confirm step with a slot, then empty the selection. Nothing
      // must reach the backend: a booking always needs a service.
      await _pickServices(bloc, [_service]);
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();
      bloc.add(const BookingServiceToggled(_service));
      await _pump();

      bloc.add(const BookingSubmitted());
      await _pump();

      expect(repo.createCalls, 0);
      expect(bloc.state.submitStatus, isNot(SubmitStatus.success));
    });
  });

  // UX review 2026-09-23, #4: a service tapped on the salon's profile opens the
  // flow with that service chosen, and the customer lands past the service step.
  group('BookingStarted with a service', () {
    test('selects it and goes straight to the time step (single staff)',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);
      addTearDown(bloc.close);

      bloc.add(const BookingStarted('p1', serviceId: 's2'));
      await _flush();

      expect(bloc.state.services, [_service2]);
      expect(bloc.state.step, BookingStep.time);
      expect(bloc.state.staff, _staffA);
      expect(bloc.state.slotsStatus, SlotsStatus.loaded);
      expect(repo.lastSlotsServiceIds, ['s2']);
    });

    test('goes to the staff step when the salon has a choice of staff',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(staff: const [_staffA, _staffB]),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);
      addTearDown(bloc.close);

      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _flush();

      expect(bloc.state.services, [_service]);
      expect(bloc.state.step, BookingStep.staff);
    });

    test('back from the time step shows the service step with it selected',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);
      addTearDown(bloc.close);

      bloc.add(const BookingStarted('p1', serviceId: 's2'));
      await _flush();
      bloc.add(const BookingStepBack());
      await _flush();

      expect(bloc.state.step, BookingStep.service);
      expect(bloc.state.isServiceSelected(_service2), isTrue);
    });

    test('an unknown service starts the flow as usual', () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);
      addTearDown(bloc.close);

      bloc.add(const BookingStarted('p1', serviceId: 'gone'));
      await _flush();

      expect(bloc.state.providerStatus, BookingProviderStatus.loaded);
      expect(bloc.state.step, BookingStep.service);
      expect(bloc.state.services, isEmpty);
      expect(repo.slotsCalls, 0);
    });

    test('coming back with the same service keeps the selections made since',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);
      addTearDown(bloc.close);

      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _flush();
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _flush();

      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _flush();

      expect(bloc.state.step, BookingStep.confirm);
      expect(bloc.state.slot, isNotNull);
    });

    test(
        'tapping the service again after backing out to the salon moves past '
        'the service step once more', () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);
      addTearDown(bloc.close);

      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _flush();
      // Back to the service step, then out of the flow to the salon's profile.
      bloc.add(const BookingStepBack());
      await _flush();
      expect(bloc.state.step, BookingStep.service);

      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _flush();

      expect(bloc.state.services, [_service]);
      expect(bloc.state.step, BookingStep.time);
    });

    test('another service of the same salon starts over with that service',
        () async {
      final repo = FakeBookingRepository(
        provider: _provider(),
        slotsResult: Right(DaySlots(slots: [_slot(10)])),
        createResults: [const Right('b1')],
      );
      final bloc = BookingBloc(repo);
      addTearDown(bloc.close);

      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _flush();
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _flush();

      bloc.add(const BookingStarted('p1', serviceId: 's2'));
      await _flush();

      expect(bloc.state.services, [_service2]);
      expect(bloc.state.step, BookingStep.time);
      expect(bloc.state.slot, isNull);
    });
  });

  // UX review 2026-09-23, #4 (observed live): the salon had today registered as
  // a holiday and the customer landed on an empty day with no way forward, while
  // the home card already said "tomorrow, 18 free times".
  group('BookingBloc first day with free times', () {
    // A Wednesday. Thursday the 24th, Friday the 25th, and so on.
    final now = DateTime(2026, 9, 23, 10, 30);
    DateTime day(int d) => DateTime(2026, 9, d);
    TimeSlot at(int d, int hour) => TimeSlot(
          startTime: DateTime(2026, 9, d, hour),
          endTime: DateTime(2026, 9, d, hour, 45),
          durationMinutes: 45,
          isAvailable: true,
          staffId: 'st1',
        );

    Future<(BookingBloc, FakeBookingRepository)> atTimeStep(
      Map<int, DaySlots> days, {
      List<BusinessHour> hours = const [],
      int window = 7,
      Map<int, Completer<void>> gates = const {},
      List<StaffMember> staff = const [_staffA],
    }) async {
      final repo = FakeBookingRepository(
        provider: _provider(
          businessHours: hours,
          maxAdvanceBookingDays: window,
          staff: staff,
        ),
        slotsResult: const Right(DaySlots()),
        createResults: [const Right('b1')],
        slotsByDay: days,
      )..gates = Map.of(gates);
      final bloc = BookingBloc(repo, now: () => now);
      addTearDown(bloc.close);
      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _flush();
      return (bloc, repo);
    }

    test('today with free times stays on today and says nothing', () async {
      final (bloc, repo) = await atTimeStep({
        23: DaySlots(slots: [at(23, 16)]),
      });

      expect(bloc.state.date, day(23));
      expect(bloc.state.slots, [at(23, 16)]);
      expect(bloc.state.freeDayNotice, isNull);
      expect(repo.slotsDates, [day(23)]);
    });

    test('an empty today moves to the first day with free times and says why',
        () async {
      final (bloc, repo) = await atTimeStep({
        23: const DaySlots(reason: 'مجموعه در این روز تعطیل است.'),
        24: const DaySlots(),
        25: DaySlots(slots: [at(25, 9), at(25, 10)]),
        26: DaySlots(slots: [at(26, 9)]),
      });

      expect(bloc.state.date, day(25));
      expect(bloc.state.slots, [at(25, 9), at(25, 10)]);
      expect(bloc.state.slotsStatus, SlotsStatus.loaded);
      expect(bloc.state.slotsReason, isNull);
      expect(bloc.state.freeDayNotice, FreeDayNotice.movedFromToday);
      expect(repo.slotsDates, [day(23), day(24), day(25)],
          reason: 'one day at a time, stopping at the first hit');
    });

    test("no free day in the window stays on today with the salon's reason",
        () async {
      final (bloc, repo) = await atTimeStep({
        23: const DaySlots(reason: 'مجموعه در این روز تعطیل است.'),
      });

      expect(bloc.state.date, day(23));
      expect(bloc.state.slots, isEmpty);
      expect(bloc.state.slotsStatus, SlotsStatus.loaded);
      expect(bloc.state.slotsReason, 'مجموعه در این روز تعطیل است.');
      expect(bloc.state.freeDayNotice, FreeDayNotice.noneInWindow);
      expect(repo.slotsDates, hasLength(8), reason: 'today plus a week');
    });

    test("the search never goes past the salon's booking window", () async {
      final (bloc, repo) = await atTimeStep({
        27: DaySlots(slots: [at(27, 9)]),
      }, window: 2);

      expect(repo.slotsDates, [day(23), day(24), day(25)]);
      expect(bloc.state.date, day(23));
      expect(bloc.state.freeDayNotice, FreeDayNotice.noneInWindow);
    });

    test('weekdays the salon is closed are not asked about', () async {
      final (bloc, repo) = await atTimeStep({
        26: DaySlots(slots: [at(26, 9)]),
      }, hours: const [
        BusinessHour(dayOfWeek: 'پنج‌شنبه', isClosed: true),
        BusinessHour(dayOfWeek: 'جمعه', isClosed: true),
      ]);

      expect(repo.slotsDates, [day(23), day(26)]);
      expect(bloc.state.date, day(26));
    });

    test('a day the customer picks meanwhile wins over the search', () async {
      final gate = Completer<void>();
      final (bloc, repo) = await atTimeStep({
        24: DaySlots(slots: [at(24, 9)]),
        27: DaySlots(slots: [at(27, 11)]),
      }, gates: {24: gate});

      // The search is waiting on the 24th; the customer taps the 27th.
      bloc.add(BookingDateSelected(day(27)));
      await _flush();
      gate.complete();
      await _flush();

      expect(bloc.state.date, day(27));
      expect(bloc.state.slots, [at(27, 11)]);
      expect(bloc.state.freeDayNotice, isNull);
      expect(repo.slotsDates, [day(23), day(24), day(27)],
          reason: 'the search stopped at the stale answer for the 24th');
    });

    test('an empty day the customer picked offers the next day with free times',
        () async {
      final (bloc, repo) = await atTimeStep({
        23: DaySlots(slots: [at(23, 16)]),
        28: DaySlots(slots: [at(28, 12)]),
      });

      bloc.add(BookingDateSelected(day(26)));
      await _flush();
      expect(bloc.state.slots, isEmpty);

      repo.slotsDates.clear();
      bloc.add(const BookingNextFreeDayRequested());
      await _flush();

      expect(bloc.state.date, day(28));
      expect(bloc.state.slots, [at(28, 12)]);
      expect(bloc.state.freeDayNotice, FreeDayNotice.movedFromPickedDay);
      expect(repo.slotsDates, [day(26), day(27), day(28)]);
    });

    test('picking a day clears the notice', () async {
      final (bloc, _) = await atTimeStep({
        24: DaySlots(slots: [at(24, 9)]),
        25: DaySlots(slots: [at(25, 9)]),
      });
      expect(bloc.state.freeDayNotice, FreeDayNotice.movedFromToday);

      bloc.add(BookingDateSelected(day(25)));
      await _flush();

      expect(bloc.state.freeDayNotice, isNull);
    });

    // Review follow-up: the search used to start from whatever day was showing, even one an earlier search had
    // chosen, so a staff member free today was never looked at today.
    test(
        'a day the app chose for one staff member is not where the search '
        'starts for another', () async {
      final (bloc, repo) = await atTimeStep(
        const {},
        staff: const [_staffA, _staffB],
      );
      repo.slotsByStaff = {
        // Staff A has nothing today; tomorrow is the first day with times.
        'st1': {24: DaySlots(slots: [at(24, 9)])},
        // Staff B is free today.
        'st2': {23: DaySlots(slots: [at(23, 16)])},
      };

      bloc.add(const BookingStaffSelected(_staffA));
      await _flush();
      expect(bloc.state.date, day(24));
      expect(bloc.state.freeDayNotice, FreeDayNotice.movedFromToday);

      bloc.add(const BookingStepBack());
      await _flush();
      repo.slotsDates.clear();
      bloc.add(const BookingStaffSelected(_staffB));
      await _flush();

      expect(bloc.state.date, day(23));
      expect(bloc.state.slots, [at(23, 16)]);
      expect(bloc.state.freeDayNotice, isNull);
      expect(repo.slotsDates, [day(23)]);
    });

    test('a day the customer picked is where the search starts for another '
        'staff member', () async {
      final (bloc, repo) = await atTimeStep(
        const {},
        staff: const [_staffA, _staffB],
      );
      repo.slotsByStaff = {
        'st1': {
          23: DaySlots(slots: [at(23, 9)]),
          26: DaySlots(slots: [at(26, 9)]),
        },
        'st2': {
          23: DaySlots(slots: [at(23, 16)]),
          26: DaySlots(slots: [at(26, 11)]),
        },
      };

      bloc.add(const BookingStaffSelected(_staffA));
      await _flush();
      bloc.add(BookingDateSelected(day(26)));
      await _flush();

      bloc.add(const BookingStepBack());
      await _flush();
      repo.slotsDates.clear();
      bloc.add(const BookingStaffSelected(_staffB));
      await _flush();

      expect(bloc.state.date, day(26));
      expect(bloc.state.slots, [at(26, 11)]);
      expect(repo.slotsDates, [day(26)]);
    });

    test('a customer-picked day the search moved on from is no longer theirs',
        () async {
      final (bloc, repo) = await atTimeStep(
        const {},
        staff: const [_staffA, _staffB],
      );
      repo.slotsByStaff = {
        'st1': {
          23: DaySlots(slots: [at(23, 9)]),
          27: DaySlots(slots: [at(27, 9)]),
        },
        'st2': {23: DaySlots(slots: [at(23, 16)])},
      };

      bloc.add(const BookingStaffSelected(_staffA));
      await _flush();
      // The customer picks an empty day and asks for the next free one: the app chose the 27th.
      bloc.add(BookingDateSelected(day(25)));
      await _flush();
      bloc.add(const BookingNextFreeDayRequested());
      await _flush();
      expect(bloc.state.date, day(27));

      bloc.add(const BookingStepBack());
      await _flush();
      bloc.add(const BookingStaffSelected(_staffB));
      await _flush();

      expect(bloc.state.date, day(23));
      expect(bloc.state.slots, [at(23, 16)]);
    });

    test('the staff step asks for no times until a staff member is chosen',
        () async {
      final (bloc, repo) = await atTimeStep(
        {23: DaySlots(slots: [at(23, 16)])},
        staff: const [_staffA, _staffB],
      );

      expect(bloc.state.step, BookingStep.staff);
      expect(repo.slotsDates, isEmpty,
          reason: 'the times depend on the staff member not yet chosen');

      bloc.add(const BookingStaffSelected(_staffB));
      await _flush();
      expect(repo.slotsStaffIds, ['st2']);
    });

    test('stepping back out of the time step stops the search', () async {
      final gate = Completer<void>();
      final (bloc, repo) = await atTimeStep(
        const {},
        staff: const [_staffA, _staffB],
      );
      repo
        ..slotsByStaff = {
          'st1': {25: DaySlots(slots: [at(25, 9)])},
        }
        ..gates = {23: gate};

      bloc.add(const BookingStaffSelected(_staffA));
      await _flush();
      // The search is waiting on today; the customer goes back to the staff step.
      bloc.add(const BookingStepBack());
      await _flush();
      gate.complete();
      await _flush();

      expect(bloc.state.step, BookingStep.staff);
      expect(repo.slotsDates, [day(23)],
          reason: 'no later day is asked for a step the customer left');
      expect(bloc.state.slots, isEmpty);
    });

    test('slot-taken recovery reloads the chosen day, not the first free one',
        () async {
      final (bloc, repo) = await atTimeStep({
        23: DaySlots(slots: [at(23, 16)]),
        26: DaySlots(slots: [at(26, 9), at(26, 10)]),
      });
      repo.createResults = [const Left(SlotTakenFailure('گرفته شد'))];

      bloc.add(BookingDateSelected(day(26)));
      await _flush();
      bloc.add(BookingSlotSelected(at(26, 9)));
      await _flush();
      bloc.add(const BookingSubmitted());
      await _flush();

      expect(bloc.state.step, BookingStep.time);
      expect(bloc.state.date, day(26));
      expect(bloc.state.slots, [at(26, 9), at(26, 10)]);
    });
  });
}

/// Lets queued events and the fake repository's answers run to completion
/// without waiting on the clock.
Future<void> _flush() async {
  for (var i = 0; i < 30; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}
