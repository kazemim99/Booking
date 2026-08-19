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

/// Adds the service to the visit, or removes it when already selected.
///
/// A visit may bundle several services (cut + colour), so this only mutates the
/// selection — advancing to the next step is [BookingServicesConfirmed]. Any
/// change invalidates already-loaded slots, because the slot length is the sum
/// of the selected services' durations.
class BookingServiceToggled extends BookingEvent {
  final ServiceItem service;

  const BookingServiceToggled(this.service);

  @override
  List<Object?> get props => [service];
}

/// Leaves the service step. Ignored while nothing is selected — a visit needs
/// at least one service.
class BookingServicesConfirmed extends BookingEvent {
  const BookingServicesConfirmed();
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

  /// Every service bundled into this visit, in the order the customer picked
  /// them. Empty until the first selection; never null.
  final List<ServiceItem> services;
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
    this.services = const [],
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

  /// Whether the visit has at least one service — the gate on leaving the
  /// service step and on submitting.
  bool get hasServices => services.isNotEmpty;

  /// Ids of every selected service, in selection order. The first one is also
  /// sent as the request's required single `serviceId`.
  List<String> get selectedServiceIds =>
      services.map((s) => s.id).toList(growable: false);

  /// How long the appointment runs: services in one visit are performed
  /// back-to-back, so the slot must span their combined duration.
  int get totalDurationMinutes =>
      services.fold(0, (sum, s) => sum + s.durationMinutes);

  /// What the visit costs — the sum over the selected services.
  double get totalPrice => services.fold(0, (sum, s) => sum + s.price);

  /// Currency for [totalPrice]. The backend rejects a visit mixing currencies,
  /// so the first service's currency describes the whole set.
  String get currency => services.isEmpty ? '' : services.first.currency;

  bool isServiceSelected(ServiceItem service) =>
      services.any((s) => s.id == service.id);

  BookingState copyWith({
    String? providerId,
    BookingProviderStatus? providerStatus,
    ProviderDetail? provider,
    String? providerError,
    BookingStep? step,
    List<ServiceItem>? services,
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
      services: services ?? this.services,
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
        services,
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

/// Stepped booking flow: services → staff (auto-skipped for single-staff
/// providers) → Jalali date/slot → confirm. Selections survive back/forward
/// navigation; a slot-taken failure returns to the slot step with refreshed
/// availability while keeping every other selection.
///
/// A visit may bundle several services. Their durations sum into the slot
/// length requested from the backend, and their prices sum into the total the
/// customer is shown, so the slot offered always fits the whole visit.
class BookingBloc extends Bloc<BookingEvent, BookingState> {
  final BookingRepository repository;
  int _slotsRequestId = 0;

  BookingBloc(this.repository) : super(const BookingState()) {
    on<BookingStarted>(_onStarted);
    on<BookingServiceToggled>(_onServiceToggled);
    on<BookingServicesConfirmed>(_onServicesConfirmed);
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

  void _onServiceToggled(
    BookingServiceToggled event,
    Emitter<BookingState> emit,
  ) {
    final selected = state.services.toList();
    final index = selected.indexWhere((s) => s.id == event.service.id);
    if (index >= 0) {
      selected.removeAt(index);
    } else {
      selected.add(event.service);
    }

    // Changing the set changes the visit's length, so any slots already
    // fetched are for the wrong duration. Drop them rather than re-fetch on
    // every tap — the fetch happens once the selection is confirmed.
    emit(state.copyWith(
      services: selected,
      slots: const [],
      slotsStatus: SlotsStatus.initial,
      slot: () => null,
    ));
  }

  void _onServicesConfirmed(
    BookingServicesConfirmed event,
    Emitter<BookingState> emit,
  ) {
    // A visit needs at least one service. The UI disables its continue button,
    // and this guard keeps the rule true regardless of the caller.
    if (!state.hasServices) return;

    final singleStaff = (state.provider?.activeStaff.length ?? 0) <= 1;
    emit(state.copyWith(
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
    final serviceIds = state.selectedServiceIds;
    final providerId = state.providerId;
    if (serviceIds.isEmpty || providerId == null) return;

    final requestId = ++_slotsRequestId;
    emit(state.copyWith(
      date: event.date,
      slots: const [],
      slotsStatus: SlotsStatus.loading,
      slot: () => null,
    ));

    // The whole set goes to the backend so the returned slots are long enough
    // for the combined duration — a two-service visit must not be offered a
    // slot sized for one.
    final result = await repository.getAvailableSlots(
      providerId: providerId,
      serviceId: serviceIds.first,
      date: event.date,
      staffId: state.staff?.id,
      serviceIds: serviceIds,
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
    final serviceIds = state.selectedServiceIds;
    final slot = state.slot;
    final staffId = state.effectiveStaffId;
    if (providerId == null || serviceIds.isEmpty || slot == null) return;
    if (staffId == null) {
      emit(state.copyWith(submitStatus: SubmitStatus.error));
      return;
    }

    emit(state.copyWith(submitStatus: SubmitStatus.submitting));
    final result = await repository.createBooking(
      providerId: providerId,
      serviceId: serviceIds.first,
      staffProviderId: staffId,
      startTime: slot.startTime,
      serviceIds: serviceIds,
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
