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

const _staffA = StaffMember(id: 'st1', name: 'مریم احمدی', isActive: true);
const _staffB = StaffMember(id: 'st2', name: 'سارا رضایی', isActive: true);

TimeSlot _slot(int hour, {String? staffId = 'st1'}) => TimeSlot(
      startTime: DateTime(2026, 8, 1, hour),
      endTime: DateTime(2026, 8, 1, hour, 45),
      durationMinutes: 45,
      isAvailable: true,
      staffId: staffId,
    );

ProviderDetail _provider({List<StaffMember> staff = const [_staffA]}) =>
    ProviderDetail(
      id: 'p1',
      businessName: 'سالن نمونه',
      averageRating: 4.8,
      totalReviews: 12,
      businessHours: const [],
      services: const [_service],
      staff: staff,
    );

class FakeBookingRepository implements BookingRepository {
  ProviderDetail provider;
  Either<Failure, List<TimeSlot>> slotsResult;
  List<Either<Failure, String>> createResults;
  int createCalls = 0;
  int slotsCalls = 0;
  String? lastStaffProviderId;

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
  }) async {
    slotsCalls++;
    return slotsResult;
  }

  @override
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
  }) async {
    lastStaffProviderId = staffProviderId;
    return createResults[createCalls++];
  }
}

Future<void> _pump() => Future<void>.delayed(const Duration(milliseconds: 20));

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
      bloc.add(const BookingServiceSelected(_service));
      await _pump();
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
      bloc.add(const BookingServiceSelected(_service));
      await _pump();

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
      bloc.add(const BookingServiceSelected(_service));
      await _pump();

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
      bloc.add(const BookingServiceSelected(_service));
      await _pump();
      bloc.add(const BookingStaffSelected(_staffB));
      await _pump();

      bloc.add(const BookingStepBack());
      await _pump();
      expect(bloc.state.step, BookingStep.staff);
      bloc.add(const BookingStepBack());
      await _pump();
      expect(bloc.state.step, BookingStep.service);

      // Selections made earlier are still there.
      expect(bloc.state.service, _service);
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
      bloc.add(const BookingServiceSelected(_service));
      await _pump();
      final slotsCallsBefore = repo.slotsCalls;

      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();
      bloc.add(const BookingSubmitted());
      await _pump();

      expect(bloc.state.step, BookingStep.time);
      expect(bloc.state.slot, isNull);
      expect(bloc.state.service, _service);
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
      bloc.add(const BookingServiceSelected(_service));
      await _pump();
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
      bloc.add(const BookingServiceSelected(_service));
      await _pump();
      bloc.add(BookingSlotSelected(bloc.state.slots.first));
      await _pump();

      // Login round-trip re-enters the flow route.
      bloc.add(const BookingStarted('p1'));
      await _pump();

      expect(bloc.state.step, BookingStep.confirm);
      expect(bloc.state.service, _service);
      expect(bloc.state.slot, isNotNull);
      await bloc.close();
    });
  });
}
