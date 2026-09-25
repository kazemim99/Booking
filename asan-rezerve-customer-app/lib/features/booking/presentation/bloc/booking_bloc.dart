import 'package:dartz/dartz.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/errors/failures.dart';
import '../../domain/business_days.dart';
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
///
/// [serviceId] is the service the customer tapped on the salon's profile (or a
/// past visit booked again). Once the salon has loaded and the id is one of its
/// services, the flow starts with it selected and moves on exactly as
/// [BookingServicesConfirmed] would. An id the salon does not offer is ignored.
/// Re-entering the same salon with no service (the login round-trip) keeps what
/// the customer has chosen, as does the same service while they are past the
/// service step; otherwise a service of the salon starts over with it.
class BookingStarted extends BookingEvent {
  final String providerId;
  final String? serviceId;

  const BookingStarted(this.providerId, {this.serviceId});

  @override
  List<Object?> get props => [providerId, serviceId];
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

/// The customer is on a day with no free time and asks for the nearest day that
/// has some: searched from the shown day to the end of the salon's window.
class BookingNextFreeDayRequested extends BookingEvent {
  const BookingNextFreeDayRequested();
}

/// The time step has just been entered: show the day the customer picked
/// (today when they picked none), or the first day after it with free times
/// when it has none.
class _BookingTimeStepEntered extends BookingEvent {
  const _BookingTimeStepEntered();
}

class BookingStepBack extends BookingEvent {
  const BookingStepBack();
}

class BookingSubmitted extends BookingEvent {
  /// A discount code the quote accepted (add-discounts-and-campaigns); the server prices the visit again.
  final String? promotionCode;

  const BookingSubmitted({this.promotionCode});

  @override
  List<Object?> get props => [promotionCode];
}

class BookingReset extends BookingEvent {
  const BookingReset();
}

// ---------- State ----------

enum BookingStep { service, staff, time, confirm }

enum BookingProviderStatus { loading, loaded, error }

enum SlotsStatus { initial, loading, loaded, error }

/// Why the time step shows the day it shows, when the app chose it rather than
/// the customer.
enum FreeDayNotice {
  /// Today had no free time; the first day that has some was selected.
  movedFromToday,

  /// The day the customer was on had no free time; the next one was selected.
  movedFromPickedDay,

  /// No day in the salon's booking window has free time.
  noneInWindow,
}

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

  /// Why the chosen day has no times, as the salon's own answer (null while there are times).
  final String? slotsReason;
  final TimeSlot? slot;

  final SubmitStatus submitStatus;
  final String? submitError;
  final String? bookingId;

  /// Set when the app moved the customer to another day; null otherwise.
  final FreeDayNotice? freeDayNotice;

  /// Whether [date] is a day the customer picked, rather than one the app chose
  /// (today, or a day a free-day search moved to). Only a picked day is kept
  /// when the time step is entered again — after another staff member, say —
  /// because a day chosen for the old choice says nothing about the new one.
  final bool dateChosenByCustomer;

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
    this.slotsReason,
    this.slot,
    this.submitStatus = SubmitStatus.idle,
    this.submitError,
    this.bookingId,
    this.freeDayNotice,
    this.dateChosenByCustomer = false,
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
    String? Function()? slotsReason,
    TimeSlot? Function()? slot,
    SubmitStatus? submitStatus,
    String? submitError,
    String? bookingId,
    FreeDayNotice? Function()? freeDayNotice,
    bool? dateChosenByCustomer,
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
      slotsReason: slotsReason != null ? slotsReason() : this.slotsReason,
      slot: slot != null ? slot() : this.slot,
      submitStatus: submitStatus ?? this.submitStatus,
      submitError: submitError,
      bookingId: bookingId ?? this.bookingId,
      freeDayNotice:
          freeDayNotice != null ? freeDayNotice() : this.freeDayNotice,
      dateChosenByCustomer: dateChosenByCustomer ?? this.dateChosenByCustomer,
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
        slotsReason,
        slot,
        submitStatus,
        submitError,
        bookingId,
        freeDayNotice,
        dateChosenByCustomer,
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
///
/// Entering the time step shows the day the customer picked (today when they
/// picked none) and, when it has no free time, the first later day in the
/// salon's window that does (UX review 2026-09-23, #4). A day an earlier search
/// chose is not a starting point: it was chosen for the staff member and
/// services of that search. The search asks the slot endpoint one day at a time rather
/// than using the providers' availability summary, because only the slot query
/// knows this visit: the summed duration of the chosen services and the chosen
/// staff member. The summary is service-agnostic, so it can promise a day on
/// which a two-service visit does not fit. A week's window is at most eight
/// small requests, stopped at the first day with times, and the salon's closed
/// weekdays are skipped without asking.
class BookingBloc extends Bloc<BookingEvent, BookingState> {
  final BookingRepository repository;
  final DateTime Function() _now;

  /// Bumped by every request for slots and by anything that invalidates one in
  /// flight, so a late answer never lands on a newer choice.
  int _slotsRequestId = 0;

  /// Bumped by every start that loads a salon and by a reset: the bloc is app-scoped, so a customer can back out of
  /// a salon still loading and open another, and the first salon's late answer must not land under the second.
  int _startId = 0;

  BookingBloc(this.repository, {DateTime Function()? now})
      : _now = now ?? DateTime.now,
        super(const BookingState()) {
    on<BookingStarted>(_onStarted);
    on<BookingServiceToggled>(_onServiceToggled);
    on<BookingServicesConfirmed>(_onServicesConfirmed);
    on<BookingStaffSelected>(_onStaffSelected);
    on<BookingDateSelected>(_onDateSelected);
    on<_BookingTimeStepEntered>(_onTimeStepEntered);
    on<BookingNextFreeDayRequested>(_onNextFreeDayRequested);
    on<BookingSlotSelected>(_onSlotSelected);
    on<BookingStepBack>(_onStepBack);
    on<BookingSubmitted>(_onSubmitted);
    on<BookingReset>(_onReset);
  }

  /// Today's date, midnight, by the injected clock.
  DateTime get today {
    final now = _now();
    return DateTime(now.year, now.month, now.day);
  }

  /// The last day this salon takes bookings for: today plus its window.
  DateTime get _lastBookableDay {
    final first = today;
    return DateTime(
      first.year,
      first.month,
      first.day + (state.provider?.maxAdvanceBookingDays ?? 7),
    );
  }

  Future<void> _onStarted(
    BookingStarted event,
    Emitter<BookingState> emit,
  ) async {
    final wanted = event.serviceId;

    // Same provider and already loaded → keep selections (login round-trip).
    if (state.providerId == event.providerId &&
        state.providerStatus == BookingProviderStatus.loaded &&
        state.submitStatus != SubmitStatus.success) {
      if (wanted == null) return;
      // Past the service step with it chosen: the customer is mid-flow.
      if (state.step != BookingStep.service &&
          state.services.any((s) => s.id == wanted)) {
        return;
      }
      final service = _serviceOf(state.provider, wanted);
      if (service == null) return;
      // Another of this salon's services was tapped, or this one again after
      // backing out to the profile: start over with it.
      _slotsRequestId++;
      emit(BookingState(
        providerId: event.providerId,
        providerStatus: BookingProviderStatus.loaded,
        provider: state.provider,
      ));
      _startWith(service, emit);
      return;
    }

    _slotsRequestId++;
    final start = ++_startId;
    emit(BookingState(providerId: event.providerId));
    final result = await repository.getProviderDetail(event.providerId);
    if (start != _startId) return;
    result.fold(
      (failure) => emit(state.copyWith(
        providerStatus: BookingProviderStatus.error,
        providerError: failure.message,
      )),
      (provider) {
        emit(state.copyWith(
          providerStatus: BookingProviderStatus.loaded,
          provider: provider,
        ));
        final service = wanted == null ? null : _serviceOf(provider, wanted);
        if (service != null) _startWith(service, emit);
      },
    );
  }

  static ServiceItem? _serviceOf(ProviderDetail? provider, String id) =>
      provider?.services.where((s) => s.id == id).firstOrNull;

  /// Selects [service] and leaves the service step as confirming it would.
  void _startWith(ServiceItem service, Emitter<BookingState> emit) {
    emit(state.copyWith(services: [service]));
    _leaveServiceStep(emit);
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
    // fetched — or on their way — are for the wrong duration. Drop them rather
    // than re-fetch on every tap; the fetch happens once the selection is
    // confirmed.
    _slotsRequestId++;
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
    _leaveServiceStep(emit);
  }

  void _leaveServiceStep(Emitter<BookingState> emit) {
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
    // With a choice of staff the times wait until a staff member is chosen.
    if (singleStaff) add(const _BookingTimeStepEntered());
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
    add(const _BookingTimeStepEntered());
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
      slotsReason: () => null,
      slot: () => null,
      // The customer chose this day; nothing to explain any more.
      freeDayNotice: () => null,
      dateChosenByCustomer: true,
    ));

    final result = await _slotsFor(event.date);

    // A newer day selection superseded this request — never show stale
    // slots for the wrong day.
    if (requestId != _slotsRequestId) return;

    result.fold(
      (failure) => emit(state.copyWith(slotsStatus: SlotsStatus.error)),
      (day) => emit(state.copyWith(
        slots: day.slots,
        slotsStatus: SlotsStatus.loaded,
        slotsReason: () => day.reason,
      )),
    );
  }

  Future<void> _onTimeStepEntered(
    _BookingTimeStepEntered event,
    Emitter<BookingState> emit,
  ) =>
      _showFirstFreeDayFrom(
        state.dateChosenByCustomer ? state.date ?? today : today,
        emit,
      );

  Future<void> _onNextFreeDayRequested(
    BookingNextFreeDayRequested event,
    Emitter<BookingState> emit,
  ) =>
      _showFirstFreeDayFrom(state.date ?? today, emit);

  /// Shows [from] when it has free times, else the first later day in the
  /// salon's window that has some, asking one day at a time.
  ///
  /// When no day has any, [from] stays selected with the salon's reason for it.
  /// Another day chosen while this runs wins: every answer is checked against
  /// the latest request before it is shown. A day the search moves to is the
  /// app's choice, not the customer's.
  Future<void> _showFirstFreeDayFrom(
    DateTime from,
    Emitter<BookingState> emit,
  ) async {
    if (state.selectedServiceIds.isEmpty || state.providerId == null) return;

    final first = today;
    final last = _lastBookableDay;
    var anchor = DateTime(from.year, from.month, from.day);
    // A day kept from an earlier visit may have slipped out of the window.
    if (anchor.isBefore(first) || anchor.isAfter(last)) anchor = first;
    // The customer's pick survives only while the search stays on it.
    final picked = state.date;
    final anchorPicked = state.dateChosenByCustomer &&
        picked != null &&
        DateTime(picked.year, picked.month, picked.day) == anchor;
    final moved = anchor == first
        ? FreeDayNotice.movedFromToday
        : FreeDayNotice.movedFromPickedDay;
    final closed =
        BusinessDays.closedWeekdays(state.provider?.businessHours ?? const []);

    final requestId = ++_slotsRequestId;
    emit(state.copyWith(
      date: anchor,
      slots: const [],
      slotsStatus: SlotsStatus.loading,
      slotsReason: () => null,
      slot: () => null,
      freeDayNotice: () => null,
      dateChosenByCustomer: anchorPicked,
    ));

    String? anchorReason;
    for (var day = anchor;
        !day.isAfter(last);
        day = DateTime(day.year, day.month, day.day + 1)) {
      // The shown day is always asked, so its empty state carries the salon's
      // own reason; later closed weekdays cannot have times.
      if (day != anchor && closed.contains(day.weekday)) continue;

      final result = await _slotsFor(day);
      if (requestId != _slotsRequestId) return;

      final found = result.getOrElse(() => const DaySlots());
      if (result.isLeft()) {
        // The shown day failing is an error to retry; a later day failing just
        // ends the search on the shown day's own answer.
        emit(day == anchor
            ? state.copyWith(slotsStatus: SlotsStatus.error)
            : state.copyWith(
                slotsStatus: SlotsStatus.loaded,
                slotsReason: () => anchorReason,
              ));
        return;
      }
      if (found.slots.isNotEmpty) {
        emit(state.copyWith(
          date: day,
          slots: found.slots,
          slotsStatus: SlotsStatus.loaded,
          slotsReason: () => null,
          freeDayNotice: () => day == anchor ? null : moved,
          dateChosenByCustomer: day == anchor && anchorPicked,
        ));
        return;
      }
      if (day == anchor) anchorReason = found.reason;
    }

    emit(state.copyWith(
      date: anchor,
      slots: const [],
      slotsStatus: SlotsStatus.loaded,
      slotsReason: () => anchorReason,
      freeDayNotice: () => FreeDayNotice.noneInWindow,
    ));
  }

  /// One day's slots for the visit as chosen so far. The whole set of services
  /// goes to the backend so the returned slots are long enough for the combined
  /// duration — a two-service visit must not be offered a slot sized for one.
  Future<Either<Failure, DaySlots>> _slotsFor(DateTime day) {
    final serviceIds = state.selectedServiceIds;
    return repository.getAvailableSlots(
      providerId: state.providerId!,
      serviceId: serviceIds.first,
      date: day,
      staffId: state.staff?.id,
      serviceIds: serviceIds,
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
      // Leaving the time step drops a search still running there: its answers
      // are for a step the customer left, and entering it again searches anew.
      if (state.step == BookingStep.time) _slotsRequestId++;
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
      promotionCode: event.promotionCode,
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
          add(BookingDateSelected(state.date ?? today));
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
    _slotsRequestId++;
    _startId++;
    emit(const BookingState());
  }
}
