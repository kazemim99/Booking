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
  final ComposerService? service;
  final ComposerStaff? staff;

  /// Date-only (local) day being composed.
  final DateTime date;
  final SlotsStatus slotsStatus;
  final List<DateTime> slots;
  final DateTime? slot;
  final bool submitting;
  final bool submitted;
  final String? error;

  const ComposerState({
    this.status = ComposerStatus.loading,
    this.catalog = const ComposerCatalog(services: [], staff: []),
    this.service,
    this.staff,
    required this.date,
    this.slotsStatus = SlotsStatus.idle,
    this.slots = const [],
    this.slot,
    this.submitting = false,
    this.submitted = false,
    this.error,
  });

  bool get canSubmit =>
      service != null && staff != null && slot != null && !submitting;

  ComposerState copyWith({
    ComposerStatus? status,
    ComposerCatalog? catalog,
    ComposerService? service,
    ComposerStaff? staff,
    DateTime? date,
    SlotsStatus? slotsStatus,
    List<DateTime>? slots,
    DateTime? Function()? slot,
    bool? submitting,
    bool? submitted,
    String? Function()? error,
  }) {
    return ComposerState(
      status: status ?? this.status,
      catalog: catalog ?? this.catalog,
      service: service ?? this.service,
      staff: staff ?? this.staff,
      date: date ?? this.date,
      slotsStatus: slotsStatus ?? this.slotsStatus,
      slots: slots ?? this.slots,
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
        service,
        staff,
        date,
        slotsStatus,
        slots,
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
          service:
              catalog.services.length == 1 ? catalog.services.single : null,
          staff: catalog.staff.length == 1 ? catalog.staff.single : null,
          error: () => null,
        ));
        if (state.service != null) _refreshSlots();
      },
    );
  }

  void selectService(ComposerService service) {
    emit(state.copyWith(service: service, slot: () => null));
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
    final service = state.service;
    if (service == null) {
      emit(state.copyWith(slotsStatus: SlotsStatus.idle, slots: const []));
      return;
    }
    final seq = ++_slotsSeq;
    emit(state.copyWith(slotsStatus: SlotsStatus.loading));
    final result = await _repository.fetchAvailableSlots(
      serviceId: service.id,
      date: state.date,
      staffId: state.staff?.id,
    );
    if (isClosed || seq != _slotsSeq) return; // superseded — discard
    result.fold(
      (f) => emit(state.copyWith(
        slotsStatus: SlotsStatus.failed,
        slots: const [],
        slot: () => null,
      )),
      (slots) => emit(state.copyWith(
        slotsStatus: SlotsStatus.ready,
        slots: slots,
        // Keep the selection only if it still exists.
        slot: () => slots.contains(state.slot) ? state.slot : null,
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
      serviceId: state.service!.id,
      staffId: state.staff!.id,
      startTime: state.slot!,
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
