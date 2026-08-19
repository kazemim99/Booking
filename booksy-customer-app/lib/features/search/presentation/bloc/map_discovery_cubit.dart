import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/location/geocoding_service.dart';
import '../../../../core/location/location_service.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../../domain/repositories/search_repository.dart';

enum MapDiscoveryStatus { initial, loading, loaded, empty, error }

/// Why the map is not showing what the customer asked for. Purely advisory:
/// the map always has a centre and always tries to show providers, so a notice
/// never replaces the content — it is surfaced alongside it.
enum MapNotice {
  none,

  /// Location permission refused; the map fell back to the launch city.
  permissionDenied,

  /// The OS location service is off; the map fell back to the launch city.
  serviceDisabled,

  /// The device could not produce a position at all.
  locationUnavailable,

  /// The typed city/area could not be geocoded; the map did not move.
  areaNotFound,
}

class MapDiscoveryState extends Equatable {
  final MapDiscoveryStatus status;

  /// Providers to draw. Only ever those that carry coordinates — a provider
  /// with no position cannot be a pin, and silently dropping it from the
  /// carousel keeps pins and cards in one-to-one correspondence.
  final List<ProviderSummary> providers;

  /// The centre the last query was run around.
  final double latitude;
  final double longitude;

  /// Radius the last query used, in kilometres.
  final double radiusKm;

  /// What the search field shows — a city/area name, not coordinates.
  final String areaLabel;

  /// Active category filter as the API's `ServiceCategory` enum name.
  final String? category;

  final String? selectedProviderId;
  final String? errorMessage;
  final MapNotice notice;

  /// Bumped whenever the **cubit** moved the centre (start, city search, my
  /// location). The page drives the map camera off this, so a user pan — which
  /// leaves it unchanged — never fights the gesture it came from.
  final int cameraRevision;

  const MapDiscoveryState({
    this.status = MapDiscoveryStatus.initial,
    this.providers = const [],
    this.latitude = MapDiscoveryCubit.fallbackLatitude,
    this.longitude = MapDiscoveryCubit.fallbackLongitude,
    this.radiusKm = MapDiscoveryCubit.defaultRadiusKm,
    this.areaLabel = '',
    this.category,
    this.selectedProviderId,
    this.errorMessage,
    this.notice = MapNotice.none,
    this.cameraRevision = 0,
  });

  /// Index of the selected provider in [providers], or 0 when nothing matches.
  int get selectedIndex {
    final id = selectedProviderId;
    if (id == null) return 0;
    final index = providers.indexWhere((p) => p.id == id);
    return index < 0 ? 0 : index;
  }

  MapDiscoveryState copyWith({
    MapDiscoveryStatus? status,
    List<ProviderSummary>? providers,
    double? latitude,
    double? longitude,
    double? radiusKm,
    String? areaLabel,
    String? Function()? category,
    String? Function()? selectedProviderId,
    String? Function()? errorMessage,
    MapNotice? notice,
    int? cameraRevision,
  }) =>
      MapDiscoveryState(
        status: status ?? this.status,
        providers: providers ?? this.providers,
        latitude: latitude ?? this.latitude,
        longitude: longitude ?? this.longitude,
        radiusKm: radiusKm ?? this.radiusKm,
        areaLabel: areaLabel ?? this.areaLabel,
        category: category != null ? category() : this.category,
        selectedProviderId: selectedProviderId != null
            ? selectedProviderId()
            : this.selectedProviderId,
        errorMessage:
            errorMessage != null ? errorMessage() : this.errorMessage,
        notice: notice ?? this.notice,
        cameraRevision: cameraRevision ?? this.cameraRevision,
      );

  @override
  List<Object?> get props => [
        status,
        providers,
        latitude,
        longitude,
        radiusKm,
        areaLabel,
        category,
        selectedProviderId,
        errorMessage,
        notice,
        cameraRevision,
      ];
}

/// Drives the map + carousel discovery surface.
///
/// Every query goes through `/Providers/by-location` (via
/// [SearchRepository.providersByLocation]) because it is the only endpoint that
/// returns coordinates and a distance — `/Providers/search` returns neither and
/// therefore cannot feed a map.
///
/// Location is never a precondition: when it is refused, off, or unavailable
/// the map centres on the launch city and still loads providers, with a Persian
/// notice explaining why. A blank screen is never an acceptable outcome.
class MapDiscoveryCubit extends Cubit<MapDiscoveryState> {
  final SearchRepository repository;
  final LocationService locationService;
  final GeocodingService geocodingService;

  /// Parsabad, Ardabil — the launch city, and the fallback centre whenever the
  /// device position is unavailable.
  static const double fallbackLatitude = 39.6482;
  static const double fallbackLongitude = 47.9174;

  static const double defaultRadiusKm = 10;

  /// Guards the stale-result race: an older in-flight response must never
  /// overwrite a newer one's providers.
  int _requestId = 0;

  MapDiscoveryCubit({
    required this.repository,
    required this.locationService,
    required this.geocodingService,
    String fallbackAreaLabel = '',
  }) : super(MapDiscoveryState(areaLabel: fallbackAreaLabel));

  /// First load: try the device position, fall back to the launch city, then
  /// query around whichever centre was resolved.
  Future<void> start() async {
    final location = await locationService.currentPosition();
    switch (location) {
      case LocationSuccess(:final latitude, :final longitude):
        await _load(
          latitude: latitude,
          longitude: longitude,
          notice: MapNotice.none,
          moveCamera: true,
        );
      case LocationPermissionDenied():
        await _loadFallback(MapNotice.permissionDenied);
      case LocationServiceDisabled():
        await _loadFallback(MapNotice.serviceDisabled);
      case LocationError():
        await _loadFallback(MapNotice.locationUnavailable);
    }
  }

  /// Re-run the last query unchanged (error retry / pull-to-refresh).
  Future<void> retry() => _load(
        latitude: state.latitude,
        longitude: state.longitude,
        radiusKm: state.radiusKm,
        notice: state.notice,
        moveCamera: false,
      );

  /// Category chip tapped. `null` clears the filter.
  Future<void> selectCategory(String? category) => _load(
        latitude: state.latitude,
        longitude: state.longitude,
        radiusKm: state.radiusKm,
        category: () => category,
        notice: state.notice,
        moveCamera: false,
      );

  /// "جستجو در این محدوده" — the camera is already where the customer put it,
  /// so the centre must not be pushed back onto the map.
  Future<void> searchVisibleArea({
    required double latitude,
    required double longitude,
    required double radiusKm,
  }) =>
      _load(
        latitude: latitude,
        longitude: longitude,
        radiusKm: radiusKm,
        notice: MapNotice.none,
        moveCamera: false,
      );

  /// City / area name submitted in the search field. Geocoded through the
  /// shared keyless Nominatim service — this screen does not geocode itself.
  Future<void> searchArea(String term) async {
    final trimmed = term.trim();
    if (trimmed.isEmpty) return;

    emit(state.copyWith(
      status: MapDiscoveryStatus.loading,
      notice: MapNotice.none,
    ));

    final coordinates = await geocodingService.geocode(trimmed);
    if (coordinates == null) {
      // The map stays where it is: throwing the customer somewhere arbitrary
      // because a name did not resolve is worse than not moving.
      emit(state.copyWith(
        status: state.providers.isEmpty
            ? MapDiscoveryStatus.empty
            : MapDiscoveryStatus.loaded,
        notice: MapNotice.areaNotFound,
      ));
      return;
    }

    await _load(
      latitude: coordinates.latitude,
      longitude: coordinates.longitude,
      radiusKm: state.radiusKm,
      areaLabel: trimmed,
      notice: MapNotice.none,
      moveCamera: true,
    );
  }

  /// Re-centre on the device position (the map's floating action button).
  Future<void> useMyLocation() async {
    final location = await locationService.currentPosition();
    switch (location) {
      case LocationSuccess(:final latitude, :final longitude):
        await _load(
          latitude: latitude,
          longitude: longitude,
          radiusKm: state.radiusKm,
          notice: MapNotice.none,
          moveCamera: true,
        );
      case LocationPermissionDenied():
        emit(state.copyWith(notice: MapNotice.permissionDenied));
      case LocationServiceDisabled():
        emit(state.copyWith(notice: MapNotice.serviceDisabled));
      case LocationError():
        emit(state.copyWith(notice: MapNotice.locationUnavailable));
    }
  }

  /// Selection is shared by the pins and the carousel — one id, both surfaces.
  void selectProvider(String? providerId) {
    if (providerId == state.selectedProviderId) return;
    emit(state.copyWith(selectedProviderId: () => providerId));
  }

  /// Dismisses the advisory notice without touching the results.
  void dismissNotice() {
    if (state.notice == MapNotice.none) return;
    emit(state.copyWith(notice: MapNotice.none));
  }

  Future<void> _loadFallback(MapNotice notice) => _load(
        latitude: fallbackLatitude,
        longitude: fallbackLongitude,
        notice: notice,
        moveCamera: true,
      );

  Future<void> _load({
    required double latitude,
    required double longitude,
    required MapNotice notice,
    required bool moveCamera,
    double? radiusKm,
    String? areaLabel,

    /// Nullable setter: omitted keeps the current filter, `() => null` clears
    /// it. A plain `String?` could not tell those two apart.
    String? Function()? category,
  }) async {
    final id = ++_requestId;
    final effectiveRadius = radiusKm ?? state.radiusKm;
    final effectiveCategory =
        category != null ? category() : state.category;

    emit(state.copyWith(
      status: MapDiscoveryStatus.loading,
      latitude: latitude,
      longitude: longitude,
      radiusKm: effectiveRadius,
      areaLabel: areaLabel,
      category: category,
      errorMessage: () => null,
      notice: notice,
      cameraRevision:
          moveCamera ? state.cameraRevision + 1 : state.cameraRevision,
    ));

    final result = await repository.providersByLocation(
      latitude: latitude,
      longitude: longitude,
      radiusKm: effectiveRadius,
      serviceCategory: effectiveCategory,
    );

    // Superseded by a newer request — drop this response entirely.
    if (id != _requestId || isClosed) return;

    result.fold(
      (failure) => emit(state.copyWith(
        status: MapDiscoveryStatus.error,
        errorMessage: () => failure.message,
      )),
      (providers) {
        // A pin needs a position; anything without one is not map content.
        final mappable =
            providers.where((provider) => provider.hasCoordinates).toList();
        emit(state.copyWith(
          status: mappable.isEmpty
              ? MapDiscoveryStatus.empty
              : MapDiscoveryStatus.loaded,
          providers: mappable,
          // Keep the previous selection when it survived the refresh, so a
          // category change does not silently jump the carousel elsewhere.
          selectedProviderId: () {
            final previous = state.selectedProviderId;
            if (previous != null && mappable.any((p) => p.id == previous)) {
              return previous;
            }
            return mappable.isEmpty ? null : mappable.first.id;
          },
        ));
      },
    );
  }
}
