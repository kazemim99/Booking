import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/location/location_service.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../../domain/repositories/search_repository.dart';

enum NearbyStatus {
  initial,
  loading,
  loaded,
  empty,
  permissionDenied,
  serviceDisabled,

  /// The fix was a network guess (tens of kilometres), not a street: "nearest" from it is another city.
  imprecise,
  error,
}

class NearbyState extends Equatable {
  final NearbyStatus status;
  final List<ProviderSummary> providers;
  final String? errorMessage;

  const NearbyState({
    this.status = NearbyStatus.initial,
    this.providers = const [],
    this.errorMessage,
  });

  NearbyState copyWith({
    NearbyStatus? status,
    List<ProviderSummary>? providers,
    String? errorMessage,
  }) =>
      NearbyState(
        status: status ?? this.status,
        providers: providers ?? this.providers,
        errorMessage: errorMessage,
      );

  @override
  List<Object?> get props => [status, providers, errorMessage];
}

/// Drives nearby-me discovery: resolve the device location, then ask `/Providers/by-location`, which applies the
/// radius server-side and returns each salon's distance.
///
/// It used to call `/Providers/search`, which has no radius parameter at all — the 10 km was dropped and every
/// active salon came back sorted by distance, so "nearest" showed a salon in Tehran to someone in پارس‌آباد, with
/// no distance on the card (QA walkthrough 2026-09-22).
///
/// When permission is denied, the service is off, or the fix is too coarse to be a street, it emits a fallback
/// status so the UI can offer manual area search instead of blocking.
class NearbyProvidersCubit extends Cubit<NearbyState> {
  final LocationService locationService;
  final SearchRepository repository;
  final double radiusKm;

  /// Beyond this a fix is a network guess rather than a place someone is standing (the map cubit's own limit).
  static const double maxTrustedAccuracyMeters = 20000;

  NearbyProvidersCubit({
    required this.locationService,
    required this.repository,
    this.radiusKm = 10,
  }) : super(const NearbyState());

  Future<void> load() async {
    emit(state.copyWith(status: NearbyStatus.loading));

    final location = await locationService.currentPosition();
    switch (location) {
      case LocationPermissionDenied():
        emit(state.copyWith(status: NearbyStatus.permissionDenied));
      case LocationServiceDisabled():
        emit(state.copyWith(status: NearbyStatus.serviceDisabled));
      case LocationError(:final message):
        emit(state.copyWith(status: NearbyStatus.error, errorMessage: message));
      case LocationSuccess(:final latitude, :final longitude, :final accuracyMeters):
        // A fix this coarse is the network's guess (an IP behind a VPN reports tens of kilometres), so "near me"
        // from it is meaningless. The map cubit already refuses these.
        if (accuracyMeters != null && accuracyMeters > maxTrustedAccuracyMeters) {
          emit(state.copyWith(status: NearbyStatus.imprecise));
          return;
        }
        final result = await repository.providersByLocation(
          latitude: latitude,
          longitude: longitude,
          radiusKm: radiusKm,
        );
        result.fold(
          (failure) => emit(state.copyWith(
            status: NearbyStatus.error,
            errorMessage: failure.message,
          )),
          (providers) => emit(state.copyWith(
            status: providers.isEmpty
                ? NearbyStatus.empty
                : NearbyStatus.loaded,
            providers: providers,
          )),
        );
    }
  }
}
