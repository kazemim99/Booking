import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../booking/domain/entities/booking_entities.dart';
import '../../../booking/domain/repositories/booking_repository.dart';
import '../../domain/entities/booking_summary.dart';
import '../../domain/repositories/bookings_repository.dart';

enum RescheduleStatus { pickingSlots, loadingSlots, slotsError, submitting, success, failure }

class RescheduleState extends Equatable {
  final RescheduleStatus status;
  final DateTime selectedDate;
  final List<TimeSlot> slots;
  final TimeSlot? selectedSlot;
  final String? errorMessage;

  const RescheduleState({
    required this.status,
    required this.selectedDate,
    this.slots = const [],
    this.selectedSlot,
    this.errorMessage,
  });

  RescheduleState copyWith({
    RescheduleStatus? status,
    DateTime? selectedDate,
    List<TimeSlot>? slots,
    TimeSlot? Function()? selectedSlot,
    String? errorMessage,
  }) {
    return RescheduleState(
      status: status ?? this.status,
      selectedDate: selectedDate ?? this.selectedDate,
      slots: slots ?? this.slots,
      selectedSlot:
          selectedSlot != null ? selectedSlot() : this.selectedSlot,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object?> get props =>
      [status, selectedDate, slots, selectedSlot, errorMessage];
}

/// Reschedule an existing booking by reusing the slot picker, scoped to
/// the booking's provider/service/staff — same endpoint semantics as the
/// web RescheduleBookingModal.
class RescheduleCubit extends Cubit<RescheduleState> {
  final BookingRepository bookingRepository;
  final BookingsRepository bookingsRepository;
  final BookingSummary booking;
  int _requestId = 0;

  RescheduleCubit({
    required this.bookingRepository,
    required this.bookingsRepository,
    required this.booking,
  }) : super(RescheduleState(
          status: RescheduleStatus.loadingSlots,
          selectedDate: _today(),
        )) {
    loadSlots(_today());
  }

  static DateTime _today() {
    final now = DateTime.now();
    return DateTime(now.year, now.month, now.day);
  }

  Future<void> loadSlots(DateTime date) async {
    final id = ++_requestId;
    emit(state.copyWith(
      status: RescheduleStatus.loadingSlots,
      selectedDate: date,
      slots: const [],
      selectedSlot: () => null,
    ));

    final result = await bookingRepository.getAvailableSlots(
      providerId: booking.providerId,
      serviceId: booking.serviceId,
      date: date,
      staffId: booking.staffId,
    );

    if (id != _requestId) return; // stale day superseded

    result.fold(
      (failure) => emit(state.copyWith(
        status: RescheduleStatus.slotsError,
        errorMessage: failure.message,
      )),
      (slots) => emit(state.copyWith(
        status: RescheduleStatus.pickingSlots,
        slots: slots,
      )),
    );
  }

  void selectSlot(TimeSlot slot) {
    emit(state.copyWith(
      status: RescheduleStatus.pickingSlots,
      selectedSlot: () => slot,
    ));
  }

  Future<void> submit() async {
    final slot = state.selectedSlot;
    if (slot == null) return;

    emit(state.copyWith(
      status: RescheduleStatus.submitting,
      selectedSlot: () => slot,
    ));

    final result = await bookingsRepository.rescheduleBooking(
      bookingId: booking.id,
      newStartTime: slot.startTime,
      newStaffId: booking.staffId == null ? slot.staffId : null,
    );

    result.fold(
      (failure) => emit(state.copyWith(
        status: RescheduleStatus.failure,
        selectedSlot: () => slot,
        errorMessage: failure.message,
      )),
      (_) => emit(state.copyWith(
        status: RescheduleStatus.success,
        selectedSlot: () => slot,
      )),
    );
  }
}
