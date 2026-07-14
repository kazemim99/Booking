import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/booking_entities.dart';
import '../../domain/repositories/booking_repository.dart';

// ---------- Events ----------

abstract class BookingEvent extends Equatable {
  const BookingEvent();

  @override
  List<Object?> get props => [];
}

/// Enter the flow for a provider. Keeps existing state when re-entering for
/// the same provider (e.g. returning from a login round-trip) so selections
/// survive; resets for a different provider.
class BookingStarted extends BookingEvent {
  final String providerId;

  const BookingStarted(this.providerId);

  @override
  List<Object?> get props => [providerId];
}

class BookingServiceSelected extends BookingEvent {
  final ServiceItem service;

  const BookingServiceSelected(this.service);

  @override
  List<Object?> get props => [service];
}

/// null staff = «فرقی نمی‌کند» (any staff; backend picks per slot).
class BookingStaffSelected extends BookingEvent {
  final StaffMember? staff;

  const BookingStaffSelected(this.staff);

  @override
  List<Object?> get props => [staff];
}

class BookingDateSelected extends BookingEvent {
  final DateTime date;

  const BookingDateSelected(this.date);

  @override
  List<Object?> get props => [date];
}

class BookingSlotSelected extends BookingEvent {
  final TimeSlot slot;

  const BookingSlotSelected(this.slot);

  @override
  List<Object?> get props => [slot];
}

class BookingStepBack extends BookingEvent {
  const BookingStepBack();
}

class BookingSubmitted extends BookingEvent {
  const BookingSubmitted();
}

class BookingReset extends BookingEvent {
  const BookingReset();
}

// ---------- State ----------

enum BookingStep { service, staff, time, confirm }

enum BookingProviderStatus { loading, loaded, error }

enum SlotsStatus { initial, loading, loaded, error }

enum SubmitStatus { idle, submitting, success, slotTaken, error }

class BookingState extends Equatable {
  final String? providerId;
  final BookingProviderStatus providerStatus;
  final ProviderDetail? provider;
  final String? providerError;

  final BookingStep step;
  final ServiceItem? service;
  final StaffMember? staff;
  final bool anyStaff;
  final DateTime? date;
  final List<TimeSlot> slots;
  final SlotsStatus slotsStatus;
  final TimeSlot? slot;

  final SubmitStatus submitStatus;
  final String? submitError;
  final String? bookingId;

  const BookingState({
    this.providerId,
    this.providerStatus = BookingProviderStatus.loading,
    this.provider,
    this.providerError,
    this.step = BookingStep.service,
    this.service,
    this.staff,
    this.anyStaff = false,
    this.date,
    this.slots = const [],
    this.slotsStatus = SlotsStatus.initial,
    this.slot,
    this.submitStatus = SubmitStatus.idle,
    this.submitError,
    this.bookingId,
  });

  /// Steps actually shown for this provider (staff step auto-skipped when
  /// at most one staff member exists).
  List<BookingStep> get visibleSteps => [
        BookingStep.service,
        if ((provider?.activeStaff.length ?? 0) > 1) BookingStep.staff,
        BookingStep.time,
        BookingStep.confirm,
      ];

  /// The staff id sent to the backend: explicit choice, or the slot's
  /// assigned staff when «any» was selected.
  String? get effectiveStaffId => staff?.id ?? slot?.staffId;

  BookingState copyWith({
    String? providerId,
    BookingProviderStatus? providerStatus,
    ProviderDetail? provider,
    String? providerError,
    BookingStep? step,
    ServiceItem? service,
    StaffMember? Function()? staff,
    bool? anyStaff,
    DateTime? date,
    List<TimeSlot>? slots,
    SlotsStatus? slotsStatus,
    TimeSlot? Function()? slot,
    SubmitStatus? submitStatus,
    String? submitError,
    String? bookingId,
  }) {
    return BookingState(
      providerId: providerId ?? this.providerId,
      providerStatus: providerStatus ?? this.providerStatus,
      provider: provider ?? this.provider,
      providerError: providerError,
      step: step ?? this.step,
      service: service ?? this.service,
      staff: staff != null ? staff() : this.staff,
      anyStaff: anyStaff ?? this.anyStaff,
      date: date ?? this.date,
      slots: slots ?? this.slots,
      slotsStatus: slotsStatus ?? this.slotsStatus,
      slot: slot != null ? slot() : this.slot,
      submitStatus: submitStatus ?? this.submitStatus,
      submitError: submitError,
      bookingId: bookingId ?? this.bookingId,
    );
  }

  @override
  List<Object?> get props => [
        providerId,
        providerStatus,
        provider,
        providerError,
        step,
        service,
        staff,
        anyStaff,
        date,
        slots,
        slotsStatus,
        slot,
        submitStatus,
        submitError,
        bookingId,
      ];
}

// ---------- Bloc ----------

/// Stepped booking flow: service → staff (auto-skipped for single-staff
/// providers) → Jalali date/slot → confirm. Selections survive back/forward
/// navigation; a slot-taken failure returns to the slot step with refreshed
/// availability while keeping every other selection.
class BookingBloc extends Bloc<BookingEvent, BookingState> {
  final BookingRepository repository;
  int _slotsRequestId = 0;

  BookingBloc(this.repository) : super(const BookingState()) {
    on<BookingStarted>(_onStarted);
    on<BookingServiceSelected>(_onServiceSelected);
    on<BookingStaffSelected>(_onStaffSelected);
    on<BookingDateSelected>(_onDateSelected);
    on<BookingSlotSelected>(_onSlotSelected);
    on<BookingStepBack>(_onStepBack);
    on<BookingSubmitted>(_onSubmitted);
    on<BookingReset>(_onReset);
  }

  Future<void> _onStarted(
    BookingStarted event,
    Emitter<BookingState> emit,
  ) async {
    // Same provider and already loaded → keep selections (login round-trip).
    if (state.providerId == event.providerId &&
        state.providerStatus == BookingProviderStatus.loaded &&
        state.submitStatus != SubmitStatus.success) {
      return;
    }

    emit(BookingState(providerId: event.providerId));
    final result = await repository.getProviderDetail(event.providerId);
    result.fold(
      (failure) => emit(state.copyWith(
        providerStatus: BookingProviderStatus.error,
        providerError: failure.message,
      )),
      (provider) => emit(state.copyWith(
        providerStatus: BookingProviderStatus.loaded,
        provider: provider,
      )),
    );
  }

  void _onServiceSelected(
    BookingServiceSelected event,
    Emitter<BookingState> emit,
  ) {
    final singleStaff = (state.provider?.activeStaff.length ?? 0) <= 1;
    emit(state.copyWith(
      service: event.service,
      // Auto-skip the staff step when there is no real choice.
      step: singleStaff ? BookingStep.time : BookingStep.staff,
      staff: singleStaff
          ? () => state.provider?.activeStaff.firstOrNull
          : () => state.staff,
      anyStaff: singleStaff ? false : state.anyStaff,
      slots: const [],
      slotsStatus: SlotsStatus.initial,
      slot: () => null,
    ));
    _loadSlotsForDate(state.date ?? DateTime.now());
  }

  void _onStaffSelected(
    BookingStaffSelected event,
    Emitter<BookingState> emit,
  ) {
    emit(state.copyWith(
      staff: () => event.staff,
      anyStaff: event.staff == null,
      step: BookingStep.time,
      slots: const [],
      slotsStatus: SlotsStatus.initial,
      slot: () => null,
    ));
    _loadSlotsForDate(state.date ?? DateTime.now());
  }

  Future<void> _onDateSelected(
    BookingDateSelected event,
    Emitter<BookingState> emit,
  ) async {
    final service = state.service;
    final providerId = state.providerId;
    if (service == null || providerId == null) return;

    final requestId = ++_slotsRequestId;
    emit(state.copyWith(
      date: event.date,
      slots: const [],
      slotsStatus: SlotsStatus.loading,
      slot: () => null,
    ));

    final result = await repository.getAvailableSlots(
      providerId: providerId,
      serviceId: service.id,
      date: event.date,
      staffId: state.staff?.id,
    );

    // A newer day selection superseded this request — never show stale
    // slots for the wrong day.
    if (requestId != _slotsRequestId) return;

    result.fold(
      (failure) => emit(state.copyWith(slotsStatus: SlotsStatus.error)),
      (slots) => emit(state.copyWith(
        slots: slots,
        slotsStatus: SlotsStatus.loaded,
      )),
    );
  }

  void _onSlotSelected(BookingSlotSelected event, Emitter<BookingState> emit) {
    emit(state.copyWith(
      slot: () => event.slot,
      step: BookingStep.confirm,
      submitStatus: SubmitStatus.idle,
    ));
  }

  void _onStepBack(BookingStepBack event, Emitter<BookingState> emit) {
    final steps = state.visibleSteps;
    final index = steps.indexOf(state.step);
    if (index > 0) {
      emit(state.copyWith(
        step: steps[index - 1],
        submitStatus: SubmitStatus.idle,
      ));
    }
  }

  Future<void> _onSubmitted(
    BookingSubmitted event,
    Emitter<BookingState> emit,
  ) async {
    final providerId = state.providerId;
    final service = state.service;
    final slot = state.slot;
    final staffId = state.effectiveStaffId;
    if (providerId == null || service == null || slot == null) return;
    if (staffId == null) {
      emit(state.copyWith(submitStatus: SubmitStatus.error));
      return;
    }

    emit(state.copyWith(submitStatus: SubmitStatus.submitting));
    final result = await repository.createBooking(
      providerId: providerId,
      serviceId: service.id,
      staffProviderId: staffId,
      startTime: slot.startTime,
    );

    await result.fold(
      (failure) async {
        if (failure is SlotTakenFailure) {
          // Return to the slot step with refreshed availability; all other
          // selections stay intact.
          emit(state.copyWith(
            submitStatus: SubmitStatus.slotTaken,
            submitError: failure.message,
            step: BookingStep.time,
            slot: () => null,
          ));
          add(BookingDateSelected(state.date ?? DateTime.now()));
        } else {
          emit(state.copyWith(
            submitStatus: SubmitStatus.error,
            submitError: failure.message,
          ));
        }
      },
      (bookingId) async {
        emit(state.copyWith(
          submitStatus: SubmitStatus.success,
          bookingId: bookingId,
        ));
      },
    );
  }

  void _onReset(BookingReset event, Emitter<BookingState> emit) {
    emit(const BookingState());
  }

  void _loadSlotsForDate(DateTime date) {
    add(BookingDateSelected(DateTime(date.year, date.month, date.day)));
  }
}
