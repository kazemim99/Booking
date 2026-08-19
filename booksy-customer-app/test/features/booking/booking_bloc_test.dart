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
}) =>
    ProviderDetail(
      id: 'p1',
      businessName: 'سالن نمونه',
      averageRating: 4.8,
      totalReviews: 12,
      businessHours: const [],
      services: services,
      staff: staff,
    );

class FakeBookingRepository implements BookingRepository {
  ProviderDetail provider;
  Either<Failure, List<TimeSlot>> slotsResult;
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

  FakeBookingRepository({
    required this.provider,
    required this.slotsResult,
    required this.createResults,
  });

  @override
  Future<Either<Failure, ProviderDetail>> getProviderDetail(String id) async =>
      Right(provider);

  @override
  Future<Either<Failure, List<TimeSlot>>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
    List<String>? serviceIds,
  }) async {
    slotsCalls++;
    lastSlotsServiceId = serviceId;
    lastSlotsServiceIds = serviceIds;
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
        slotsResult: Right([_slot(10), _slot(11)]),
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
        slotsResult: Right([_slot(10)]),
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
        slotsResult: Right([_slot(10)]),
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
        slotsResult: Right([_slot(10)]),
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
        slotsResult: Right([_slot(10), _slot(11)]),
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
        slotsResult: Right([_slot(10, staffId: 'st2')]),
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
        slotsResult: Right([_slot(10)]),
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
        slotsResult: Right([_slot(10), _slot(11)]),
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
}
