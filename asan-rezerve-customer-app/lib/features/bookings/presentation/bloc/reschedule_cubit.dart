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

  /// The server's reason for an empty day (closed that weekday, too short
  /// for the visit, ...); null when the day has times or gave none.
  final String? slotsReason;

  /// How far ahead the salon takes bookings — the day strip offers today
  /// plus these, like the booking flow. Seven until the salon's profile
  /// arrives (the customer booking window).
  final int maxAdvanceBookingDays;

  /// The salon's weekly hours, from its profile — the day strip disables the weekdays it is closed, as the booking
  /// flow's does. Empty until the profile arrives (no day is treated as closed).
  final List<BusinessHour> businessHours;

  const RescheduleState({
    required this.status,
    required this.selectedDate,
    this.slots = const [],
    this.selectedSlot,
    this.errorMessage,
    this.slotsReason,
    this.maxAdvanceBookingDays = RescheduleCubit.defaultBookingWindowDays,
    this.businessHours = const [],
  });

  RescheduleState copyWith({
    RescheduleStatus? status,
    DateTime? selectedDate,
    List<TimeSlot>? slots,
    TimeSlot? Function()? selectedSlot,
    String? errorMessage,
    String? Function()? slotsReason,
    int? maxAdvanceBookingDays,
    List<BusinessHour>? businessHours,
  }) {
    return RescheduleState(
      status: status ?? this.status,
      selectedDate: selectedDate ?? this.selectedDate,
      slots: slots ?? this.slots,
      selectedSlot:
          selectedSlot != null ? selectedSlot() : this.selectedSlot,
      errorMessage: errorMessage,
      slotsReason: slotsReason != null ? slotsReason() : this.slotsReason,
      maxAdvanceBookingDays:
          maxAdvanceBookingDays ?? this.maxAdvanceBookingDays,
      businessHours: businessHours ?? this.businessHours,
    );
  }

  @override
  List<Object?> get props => [
        status,
        selectedDate,
        slots,
        selectedSlot,
        errorMessage,
        slotsReason,
        maxAdvanceBookingDays,
        businessHours,
      ];
}

/// Reschedule an existing booking by reusing the slot picker, scoped to
/// the booking's provider/service/staff — same endpoint semantics as the
/// web RescheduleBookingModal.
class RescheduleCubit extends Cubit<RescheduleState> {
  final BookingRepository bookingRepository;
  final BookingsRepository bookingsRepository;
  final BookingSummary booking;
  int _requestId = 0;

  /// The first day of the strip (the clock's day when the screen opened).
  late final DateTime _today;

  /// The first day of the strip: the injected clock's day when the screen opened.
  DateTime get today => _today;

  /// The customer booking window, used until the salon's own arrives.
  static const int defaultBookingWindowDays = 7;

  RescheduleCubit({
    required this.bookingRepository,
    required this.bookingsRepository,
    required this.booking,
    DateTime Function()? now,
  }) : super(RescheduleState(
          status: RescheduleStatus.loadingSlots,
          selectedDate: _dayOf((now ?? DateTime.now)()),
        )) {
    _today = state.selectedDate;
    loadSlots(_today);
    _loadBookingWindow();
  }

  static DateTime _dayOf(DateTime t) => DateTime(t.year, t.month, t.day);

  /// The salon's booking window, from the same profile the booking flow
  /// reads. A failure keeps the default: the server still enforces the
  /// real window on submit.
  ///
  /// A day the customer picked from the default strip before the window
  /// arrived may lie past it; the strip no longer shows that day, so the
  /// selection goes back to today and today's times are loaded.
  Future<void> _loadBookingWindow() async {
    final result = await bookingRepository.getProviderDetail(booking.providerId);
    if (isClosed) return;
    final provider = result.fold((_) => null, (p) => p);
    if (provider == null) return;
    final days = provider.maxAdvanceBookingDays;

    emit(state.copyWith(
      maxAdvanceBookingDays: days,
      businessHours: provider.businessHours,
      errorMessage: state.errorMessage,
    ));
    final lastDay = DateTime(_today.year, _today.month, _today.day + days);
    if (state.selectedDate.isAfter(lastDay)) await loadSlots(_today);
  }

  Future<void> loadSlots(DateTime date) async {
    final id = ++_requestId;
    emit(state.copyWith(
      status: RescheduleStatus.loadingSlots,
      selectedDate: date,
      slots: const [],
      selectedSlot: () => null,
      slotsReason: () => null,
    ));

    final result = await bookingRepository.getAvailableSlots(
      providerId: booking.providerId,
      serviceId: booking.serviceId,
      date: date,
      staffId: booking.staffId,
    );

    if (id != _requestId || isClosed) return; // stale day superseded

    result.fold(
      (failure) => emit(state.copyWith(
        status: RescheduleStatus.slotsError,
        errorMessage: failure.message,
      )),
      (day) => emit(state.copyWith(
        status: RescheduleStatus.pickingSlots,
        slots: day.slots,
        slotsReason: () => day.slots.isEmpty ? day.reason : null,
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
