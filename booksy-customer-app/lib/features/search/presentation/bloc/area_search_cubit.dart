import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/location/geocoding_service.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../../domain/repositories/search_repository.dart';

enum AreaSearchStatus { initial, loading, loaded, empty, areaNotFound, error }

class AreaSearchState extends Equatable {
  final AreaSearchStatus status;
  final String areaName;
  final List<ProviderSummary> providers;
  final String? errorMessage;

  const AreaSearchState({
    this.status = AreaSearchStatus.initial,
    this.areaName = '',
    this.providers = const [],
    this.errorMessage,
  });

  AreaSearchState copyWith({
    AreaSearchStatus? status,
    String? areaName,
    List<ProviderSummary>? providers,
    String? errorMessage,
  }) =>
      AreaSearchState(
        status: status ?? this.status,
        areaName: areaName ?? this.areaName,
        providers: providers ?? this.providers,
        errorMessage: errorMessage,
      );

  @override
  List<Object?> get props => [status, areaName, providers, errorMessage];
}

/// Discovery within a selected area/district. The area **name** is geocoded to
/// coordinates via [GeocodingService] (keyless Nominatim), then providers are
/// searched around that point via the confirmed `/Providers/search` distance
/// path — deliberately avoiding the unverified `/Locations/search` endpoint.
class AreaSearchCubit extends Cubit<AreaSearchState> {
  final GeocodingService geocodingService;
  final SearchRepository repository;
  final double radiusKm;

  AreaSearchCubit({
    required this.geocodingService,
    required this.repository,
    this.radiusKm = 10,
  }) : super(const AreaSearchState());

  Future<void> searchArea(String areaName) async {
    final trimmed = areaName.trim();
    if (trimmed.isEmpty) return;

    emit(state.copyWith(status: AreaSearchStatus.loading, areaName: trimmed));

    final coords = await geocodingService.geocode(trimmed);
    if (coords == null) {
      emit(state.copyWith(status: AreaSearchStatus.areaNotFound));
      return;
    }

    final result = await repository.searchProviders(
      latitude: coords.latitude,
      longitude: coords.longitude,
      radiusKm: radiusKm,
      sortBy: 'distance',
    );
    result.fold(
      (failure) => emit(state.copyWith(
        status: AreaSearchStatus.error,
        errorMessage: failure.message,
      )),
      (providers) => emit(state.copyWith(
        status: providers.isEmpty
            ? AreaSearchStatus.empty
            : AreaSearchStatus.loaded,
        providers: providers,
      )),
    );
  }
}
