import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/network/connectivity_service.dart';
import '../../../auth/domain/entities/provider_status.dart';
import '../../domain/entities/home_context.dart';
import '../../domain/entities/home_enums.dart';
import '../../domain/entities/home_inputs.dart';
import '../../domain/entities/home_snapshot.dart';
import '../../domain/repositories/home_repository.dart';
import '../../domain/services/home_context_resolver.dart';

/// Emits the [HomeContext] the Home orchestrator renders from.
///
/// Owns the system layer (connectivity / load status / cache) and delegates
/// all composition decisions to the pure [HomeContextResolver]:
/// - `load()` — initial fetch (skeleton first when no cache).
/// - `refresh()` — pull-to-refresh / poll tick; keeps cached content on screen
///   while in flight and falls back to it (stale) on failure.
/// - Connectivity stream — offline flips the context to stale-cached mode;
///   reconnect re-resolves and triggers a refresh.
/// - [pollInterval] — MVP polling (resolved decision #2). Push-ready: a live
///   source later calls [refresh] (or feeds the repository) with polling off,
///   with no changes here beyond disabling the timer.
class HomeCubit extends Cubit<HomeContext> {
  final HomeRepository _repository;
  final ConnectivityService _connectivity;
  final HomeContextResolver resolver;
  final Duration? pollInterval;

  HomeSnapshot? _cached;
  ConnectivityStatus _conn = ConnectivityStatus.online;
  DataLoadStatus _loadStatus = DataLoadStatus.loading;
  StreamSubscription<bool>? _connSub;
  Timer? _pollTimer;
  int _requestSeq = 0; // stale-result guard

  HomeCubit(
    this._repository,
    this._connectivity, {
    this.resolver = const HomeContextResolver(),
    this.pollInterval,
  }) : super(_initial);

  /// The pre-load context: system LOADING → the orchestrator shows the
  /// composition skeleton (spec §7 row 16).
  static final HomeContext _initial = const HomeContextResolver().resolve(
    const HomeInputs(
      connectivity: ConnectivityStatus.online,
      loadStatus: DataLoadStatus.loading,
      hasCachedData: false,
      providerStatus: ProviderStatus.active, // irrelevant while LOADING
      bookingMode: HomeBookingMode.request,
      availability: HomeAvailability.open,
      signals: MaturitySignals(
        profileComplete: false,
        totalBookingsAllTime: 0,
        bookingsTrailing30d: 0,
      ),
    ),
  );

  /// Initial load: resolves connectivity, subscribes to changes, starts the
  /// poll timer (when configured), and fetches the first snapshot.
  Future<void> load() async {
    _conn = await _connectivity.isOnline
        ? ConnectivityStatus.online
        : ConnectivityStatus.offline;
    _connSub ??= _connectivity.onStatusChange.listen(_onConnectivity);
    if (pollInterval != null) {
      _pollTimer ??= Timer.periodic(pollInterval!, (_) => refresh());
    }
    await refresh();
  }

  /// Fetches a fresh snapshot and re-resolves. Cached content stays on screen
  /// while in flight; on failure the cache is surfaced as stale, or the error
  /// context when there is nothing to show.
  Future<void> refresh() async {
    final seq = ++_requestSeq;
    _loadStatus = DataLoadStatus.loading;
    _emitResolved();

    final result = await _repository.fetchSnapshot();
    if (isClosed || seq != _requestSeq) return; // superseded by a newer call

    result.fold(
      (_) => _loadStatus = DataLoadStatus.failed,
      (snapshot) {
        _cached = snapshot;
        _loadStatus = DataLoadStatus.loaded;
      },
    );
    _emitResolved();
  }

  // ---- Booking quick actions (T0/T1). Return null on success, or the
  // Failure for the page to surface. Success triggers a refresh so the
  // composition reflects the change. Offline mutations are refused with a
  // reason for now (queued offline mutations are a later increment).

  Future<Failure?> confirmBooking(String id) =>
      _mutate(() => _repository.confirmBooking(id));

  Future<Failure?> declineBooking(String id, {required String reason}) =>
      _mutate(() => _repository.declineBooking(id, reason: reason));

  Future<Failure?> completeBooking(String id) =>
      _mutate(() => _repository.completeBooking(id));

  Future<Failure?> markNoShow(String id) =>
      _mutate(() => _repository.markNoShow(id));

  Future<Failure?> _mutate(
    Future<Either<Failure, void>> Function() call,
  ) async {
    if (_conn == ConnectivityStatus.offline) {
      return const NetworkFailure(AppStrings.offline);
    }
    final result = await call();
    return result.fold((f) => f, (_) {
      refresh();
      return null;
    });
  }

  void _onConnectivity(bool online) {
    final next =
        online ? ConnectivityStatus.online : ConnectivityStatus.offline;
    if (next == _conn) return;
    final wasOffline = _conn == ConnectivityStatus.offline;
    _conn = next;
    _emitResolved();
    if (wasOffline && online) {
      refresh(); // back online → sync
    }
  }

  void _emitResolved() {
    final snapshot = _cached;
    final inputs = snapshot != null
        ? snapshot.toInputs(
            connectivity: _conn,
            loadStatus: _loadStatus,
            hasCachedData: true,
          )
        : HomeInputs(
            connectivity: _conn,
            loadStatus: _loadStatus,
            hasCachedData: false,
            providerStatus: ProviderStatus.active, // masked by system state
            bookingMode: HomeBookingMode.request,
            availability: HomeAvailability.open,
            signals: const MaturitySignals(
              profileComplete: false,
              totalBookingsAllTime: 0,
              bookingsTrailing30d: 0,
            ),
          );
    emit(resolver.resolve(inputs));
  }

  @override
  Future<void> close() {
    _connSub?.cancel();
    _pollTimer?.cancel();
    return super.close();
  }
}
