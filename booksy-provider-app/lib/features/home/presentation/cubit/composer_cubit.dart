import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/composer_models.dart';
import '../../domain/repositories/home_repository.dart';

/// Catalog load lifecycle.
enum ComposerStatus { loading, ready, failed }

/// Slot fetch lifecycle for the current selection.
enum SlotsStatus { idle, loading, ready, failed }

class ComposerState extends Equatable {
  final ComposerStatus status;
  final ComposerCatalog catalog;

  /// One or more services performed in a single visit (cut + color).
  /// Order = selection order; the first is the primary.
  final List<ComposerService> services;
  final ComposerStaff? staff;

  /// Date-only (local) day being composed.
  final DateTime date;
  final SlotsStatus slotsStatus;
  final List<DateTime> slots;

  /// Server's explanation for an empty [slots] (no staff, closed day, …).
  /// Null when slots exist or the server offered no reason.
  final String? slotsUnavailableReason;
  final DateTime? slot;
  final bool submitting;
  final bool submitted;
  final String? error;

  const ComposerState({
    this.status = ComposerStatus.loading,
    this.catalog = const ComposerCatalog(services: [], staff: []),
    this.services = const [],
    this.staff,
    required this.date,
    this.slotsStatus = SlotsStatus.idle,
    this.slots = const [],
    this.slotsUnavailableReason,
    this.slot,
    this.submitting = false,
    this.submitted = false,
    this.error,
  });

  /// Primary (first) service, or null when nothing is selected.
  ComposerService? get service => services.isEmpty ? null : services.first;

  /// Combined length of all selected services (the slot's duration).
  int get totalDurationMinutes =>
      services.fold(0, (sum, s) => sum + s.durationMinutes);

  /// Combined price of all selected services.
  double get totalPrice => services.fold(0.0, (sum, s) => sum + s.price);

  bool get canSubmit =>
      services.isNotEmpty && staff != null && slot != null && !submitting;

  ComposerState copyWith({
    ComposerStatus? status,
    ComposerCatalog? catalog,
    List<ComposerService>? services,
    ComposerStaff? staff,
    DateTime? date,
    SlotsStatus? slotsStatus,
    List<DateTime>? slots,
    String? Function()? slotsUnavailableReason,
    DateTime? Function()? slot,
    bool? submitting,
    bool? submitted,
    String? Function()? error,
  }) {
    return ComposerState(
      status: status ?? this.status,
      catalog: catalog ?? this.catalog,
      services: services ?? this.services,
      staff: staff ?? this.staff,
      date: date ?? this.date,
      slotsStatus: slotsStatus ?? this.slotsStatus,
      slots: slots ?? this.slots,
      slotsUnavailableReason: slotsUnavailableReason != null
          ? slotsUnavailableReason()
          : this.slotsUnavailableReason,
      slot: slot != null ? slot() : this.slot,
      submitting: submitting ?? this.submitting,
      submitted: submitted ?? this.submitted,
      error: error != null ? error() : this.error,
    );
  }

  @override
  List<Object?> get props => [
        status,
        catalog,
        services,
        staff,
        date,
        slotsStatus,
        slots,
        slotsUnavailableReason,
        slot,
        submitting,
        submitted,
        error,
      ];
}

/// State for the one-screen booking composer (spec:
/// provider-booking-composer). Slots re-fetch on any selection change with a
/// monotonic sequence guard so a stale response never overwrites a newer one.
class ComposerCubit extends Cubit<ComposerState> {
  final HomeRepository _repository;
  int _slotsSeq = 0;

  /// [initialDate] pre-sets the composed day (calendar-initiated creation);
  /// defaults to today.
  ComposerCubit(this._repository, {DateTime Function()? now, DateTime? initialDate})
      : super(ComposerState(
            date: _dateOnly(initialDate ?? (now ?? DateTime.now)())));

  static DateTime _dateOnly(DateTime d) => DateTime(d.year, d.month, d.day);

  Future<void> load() async {
    emit(state.copyWith(status: ComposerStatus.loading));
    final result = await _repository.fetchComposerCatalog();
    if (isClosed) return;
    result.fold(
      (f) => emit(state.copyWith(
        status: ComposerStatus.failed,
        error: () => f.message,
      )),
      (catalog) {
        // Single options pre-select for speed (the common solo case).
        emit(state.copyWith(
          status: ComposerStatus.ready,
          catalog: catalog,
          services: catalog.services.length == 1
              ? [catalog.services.single]
              : const [],
          staff: catalog.staff.length == 1 ? catalog.staff.single : null,
          error: () => null,
        ));
        if (state.services.isNotEmpty) _refreshSlots();
      },
    );
  }

  /// Adds the service to the visit, or removes it if already selected.
  /// Slots re-fetch against the new combined duration.
  void toggleService(ComposerService service) {
    final next = List<ComposerService>.from(state.services);
    final at = next.indexWhere((s) => s.id == service.id);
    if (at >= 0) {
      next.removeAt(at);
    } else {
      next.add(service);
    }
    emit(state.copyWith(services: next, slot: () => null));
    _refreshSlots();
  }

  void selectStaff(ComposerStaff staff) {
    emit(state.copyWith(staff: staff, slot: () => null));
    _refreshSlots();
  }

  void selectDate(DateTime date) {
    emit(state.copyWith(date: _dateOnly(date), slot: () => null));
    _refreshSlots();
  }

  void selectSlot(DateTime slot) => emit(state.copyWith(slot: () => slot));

  Future<void> _refreshSlots() async {
    if (state.services.isEmpty) {
      emit(state.copyWith(slotsStatus: SlotsStatus.idle, slots: const []));
      return;
    }
    final seq = ++_slotsSeq;
    emit(state.copyWith(slotsStatus: SlotsStatus.loading));
    final result = await _repository.fetchAvailableSlots(
      serviceId: state.services.first.id,
      date: state.date,
      staffId: state.staff?.id,
      serviceIds: state.services.map((s) => s.id).toList(),
    );
    if (isClosed || seq != _slotsSeq) return; // superseded — discard
    result.fold(
      (f) => emit(state.copyWith(
        slotsStatus: SlotsStatus.failed,
        slots: const [],
        slotsUnavailableReason: () => null,
        slot: () => null,
      )),
      (availability) => emit(state.copyWith(
        slotsStatus: SlotsStatus.ready,
        slots: availability.slots,
        // Why the day is empty (no staff, closed, …) — shown in place of the
        // generic "no times" so a misconfiguration is not silently hidden.
        slotsUnavailableReason: () => availability.unavailableReason,
        // Keep the selection only if it still exists.
        slot: () =>
            availability.slots.contains(state.slot) ? state.slot : null,
      )),
    );
  }

  Future<void> retrySlots() => _refreshSlots();

  Future<void> submit({
    String? clientName,
    String? clientPhone,
    String? notes,
  }) async {
    if (!state.canSubmit) return;
    emit(state.copyWith(submitting: true, error: () => null));
    final result = await _repository.createBooking(
      serviceId: state.services.first.id,
      staffId: state.staff!.id,
      startTime: state.slot!,
      serviceIds: state.services.map((s) => s.id).toList(),
      clientName: clientName,
      clientPhone: clientPhone,
      notes: notes,
    );
    if (isClosed) return;
    result.fold(
      // Failure preserves every selection for retry (spec).
      (f) => emit(state.copyWith(
        submitting: false,
        error: () => f.message,
      )),
      (_) => emit(state.copyWith(submitting: false, submitted: true)),
    );
  }
}
