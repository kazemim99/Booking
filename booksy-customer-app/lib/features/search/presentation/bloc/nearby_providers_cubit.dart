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

/// Drives nearby-me discovery: resolve device location, then search providers
/// sorted by distance via the existing `/Providers/search` contract. When
/// location permission is denied or the service is off, it emits a fallback
/// status so the UI can offer manual area/district search instead of blocking.
class NearbyProvidersCubit extends Cubit<NearbyState> {
  final LocationService locationService;
  final SearchRepository repository;
  final double radiusKm;

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
      case LocationSuccess(:final latitude, :final longitude):
        final result = await repository.searchProviders(
          latitude: latitude,
          longitude: longitude,
          radiusKm: radiusKm,
          sortBy: 'distance',
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
