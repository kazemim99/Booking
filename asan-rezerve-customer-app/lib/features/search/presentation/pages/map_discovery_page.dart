import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../bloc/map_discovery_cubit.dart';
import '../widgets/category_filter_row.dart';
import '../widgets/map_clustering.dart';
import '../widgets/map_pin.dart';
import '../widgets/map_provider_card.dart';
import '../../../../core/location/geocoding_service.dart';

/// Map + list discovery.
///
/// Replaces the two former list-only destinations ("اطراف من" and
/// "جستجو در محله"): the map's own search field geocodes a city or area, and
/// the floating action button re-centres on the device — so both jobs are done
/// here, on one screen, next to the results they affect.
///
/// Tiles come from OpenStreetMap, whose usage policy requires visible
/// attribution and an identifying user agent; both are set below.
class MapDiscoveryPage extends StatefulWidget {
  /// Injected in tests; resolved from DI in the app.
  final MapDiscoveryCubit? cubit;

  /// Injected in tests so the widget tree never reaches the tile network.
  /// `null` uses flutter_map's default network provider.
  final TileProvider? tileProvider;

  /// Today, for reading the cards' free-slot day as «امروز»/«فردا».
  /// Injected by tests; `null` reads the clock.
  final DateTime? now;

  const MapDiscoveryPage({
    super.key,
    this.cubit,
    this.tileProvider,
    this.now,
  });

  /// OSM raster tiles: keyless, and the only source that works unchanged on
  /// Android, iOS and Flutter web.
  static const String tileUrlTemplate =
      'https://tile.openstreetmap.org/{z}/{x}/{y}.png';

  static const String userAgentPackageName = 'com.asanrezerve.customer';

  static const double initialZoom = 13;
  static const double maxZoom = 18;
  static const double minZoom = 4;

  @override
  State<MapDiscoveryPage> createState() => _MapDiscoveryPageState();
}

class _MapDiscoveryPageState extends State<MapDiscoveryPage> {
  /// How long the camera must be still before the "search this area" button
  /// appears. Long enough that it never flickers mid-drag.
  static const Duration _panSettleDelay = Duration(milliseconds: 500);

  late final MapDiscoveryCubit _cubit;
  late final bool _ownsCubit;

  final MapController _mapController = MapController();
  final PageController _pageController =
      PageController(viewportFraction: 0.86);
  final TextEditingController _searchController = TextEditingController();

  Timer? _panSettleTimer;

  /// Where the camera came to rest after the last gesture, and how wide the
  /// viewport is there. Only set from a gesture — a programmatic move must not
  /// offer to re-search the place it just went to.
  LatLng? _settledCenter;
  double _settledRadiusKm = MapDiscoveryCubit.defaultRadiusKm;
  bool _searchAreaOffered = false;

  /// Place suggestions for what is being typed, and the timer that keeps the
  /// app from asking on every keystroke.
  List<PlaceSuggestion> _suggestions = const [];
  Timer? _suggestTimer;
  static const Duration _suggestDelay = Duration(milliseconds: 350);

  bool _mapReady = false;
  int _appliedCameraRevision = 0;

  @override
  void initState() {
    super.initState();
    _ownsCubit = widget.cubit == null;
    _cubit = widget.cubit ?? getIt<MapDiscoveryCubit>();
    if (_cubit.state.status == MapDiscoveryStatus.initial) {
      _cubit.start();
    }
    _searchController.text = _cubit.state.areaLabel;
  }

  @override
  void dispose() {
    _panSettleTimer?.cancel();
    _suggestTimer?.cancel();
    _searchController.dispose();
    _pageController.dispose();
    _mapController.dispose();
    if (_ownsCubit) _cubit.close();
    super.dispose();
  }

  // ------------------------------------------------------------ camera

  void _onMapReady() {
    _mapReady = true;
    _appliedCameraRevision = _cubit.state.cameraRevision;
    // The first load usually resolves after the map was built, so the centre
    // the cubit settled on has to be pushed in rather than passed as an
    // initial value.
    _moveTo(_cubit.state);
  }

  void _moveTo(MapDiscoveryState state) {
    if (!_mapReady) return;
    _mapController.move(
      LatLng(state.latitude, state.longitude),
      _mapController.camera.zoom,
    );
    _dismissSearchArea();
  }

  void _onPositionChanged(MapCamera camera, bool hasGesture) {
    // Never re-query mid-gesture, and never re-query silently: a pan only ever
    // *offers* the refresh, after the camera has been still for a moment.
    if (!hasGesture) return;

    _panSettleTimer?.cancel();
    _panSettleTimer = Timer(_panSettleDelay, () {
      if (!mounted) return;
      final center = camera.center;
      final radiusKm = _radiusForCamera(camera);
      final state = _cubit.state;
      final moved = const Distance().as(
            LengthUnit.Kilometer,
            center,
            LatLng(state.latitude, state.longitude),
          ) >
          math.max(0.5, state.radiusKm * 0.15);
      // Zooming out far enough to reveal a much larger area is also worth
      // re-querying, even from the same centre.
      final widened = radiusKm > state.radiusKm * 1.3;

      if (!moved && !widened) return;
      setState(() {
        _settledCenter = center;
        _settledRadiusKm = radiusKm;
        _searchAreaOffered = true;
      });
    });
  }

  /// Half the diagonal of the visible viewport, which is the smallest circle
  /// that covers everything the customer can see.
  static double _radiusForCamera(MapCamera camera) {
    final bounds = camera.visibleBounds;
    final radius = const Distance().as(
      LengthUnit.Kilometer,
      bounds.center,
      bounds.northEast,
    );
    return radius.clamp(1.0, 50.0);
  }

  void _dismissSearchArea() {
    _panSettleTimer?.cancel();
    if (!_searchAreaOffered) return;
    setState(() {
      _searchAreaOffered = false;
      _settledCenter = null;
    });
  }

  void _searchVisibleArea() {
    final center = _settledCenter;
    if (center == null) return;
    final radiusKm = _settledRadiusKm;
    _dismissSearchArea();
    _cubit.searchVisibleArea(
      latitude: center.latitude,
      longitude: center.longitude,
      radiusKm: radiusKm,
    );
  }

  // ------------------------------------------------------------ selection

  void _onPinTapped(MapPinGroup pin) {
    if (pin.isCluster) {
      // Zooming in is what breaks a cluster apart — the grid is recomputed
      // against the new camera on the very next frame.
      _mapController.move(
        LatLng(pin.latitude, pin.longitude),
        math.min(_mapController.camera.zoom + 2, MapDiscoveryPage.maxZoom),
      );
      _dismissSearchArea();
      return;
    }
    _cubit.selectProvider(pin.provider.id);
  }

  void _syncCarouselTo(MapDiscoveryState state) {
    if (!_pageController.hasClients || state.providers.isEmpty) return;
    final target = state.selectedIndex;
    final current = _pageController.page?.round() ?? _pageController.initialPage;
    if (current == target) return;
    _pageController.animateToPage(
      target,
      duration: AppMotion.fast,
      curve: AppMotion.standard,
    );
  }

  // ------------------------------------------------------------ build

  /// Suggestions follow the typing, one request per pause rather than per key.
  void _onSearchChanged(String term) {
    _suggestTimer?.cancel();
    if (term.trim().length < 2) {
      if (_suggestions.isNotEmpty) setState(() => _suggestions = const []);
      return;
    }
    _suggestTimer = Timer(_suggestDelay, () async {
      final places = await _cubit.suggestPlaces(term);
      if (!mounted) return;
      setState(() => _suggestions = places);
    });
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider.value(
      value: _cubit,
      child: Scaffold(
        appBar: AppBar(
          centerTitle: true,
          title: const Text(AppStrings.mapTitle),
        ),
        body: MultiBlocListener(
          listeners: [
            BlocListener<MapDiscoveryCubit, MapDiscoveryState>(
              listenWhen: (previous, current) =>
                  previous.cameraRevision != current.cameraRevision,
              listener: (context, state) {
                if (state.cameraRevision == _appliedCameraRevision) return;
                _appliedCameraRevision = state.cameraRevision;
                _moveTo(state);
              },
            ),
            BlocListener<MapDiscoveryCubit, MapDiscoveryState>(
              listenWhen: (previous, current) =>
                  previous.selectedProviderId != current.selectedProviderId ||
                  previous.providers != current.providers,
              listener: (context, state) => _syncCarouselTo(state),
            ),
            BlocListener<MapDiscoveryCubit, MapDiscoveryState>(
              listenWhen: (previous, current) =>
                  previous.areaLabel != current.areaLabel,
              listener: (context, state) {
                if (_searchController.text != state.areaLabel) {
                  _searchController.text = state.areaLabel;
                }
              },
            ),
          ],
          child: SafeArea(
            child: Column(
              children: [
                _AreaSearchField(
                  controller: _searchController,
                  onChanged: _onSearchChanged,
                  onSubmitted: (value) {
                    FocusScope.of(context).unfocus();
                    setState(() => _suggestions = const []);
                    _cubit.searchArea(value);
                  },
                ),
                // Suggestions float over the chips and the map, so opening them
                // never shoves the map down under the customer's finger.
                Expanded(
                  child: Stack(
                    children: [
                      Positioned.fill(
                        child: Column(
                          children: [
                            const SizedBox(height: AppSpacing.xs),
                            BlocBuilder<MapDiscoveryCubit, MapDiscoveryState>(
                              buildWhen: (previous, current) =>
                                  previous.category != current.category,
                              builder: (context, state) => CategoryFilterRow(
                                selected: state.category,
                                onSelected: _cubit.selectCategory,
                              ),
                            ),
                            const SizedBox(height: AppSpacing.xs),
                            Expanded(
                              child: BlocBuilder<MapDiscoveryCubit,
                                  MapDiscoveryState>(
                                builder: (context, state) => _MapSurface(
                                  state: state,
                                  now: widget.now,
                                  mapController: _mapController,
                                  pageController: _pageController,
                                  tileProvider: widget.tileProvider,
                                  onMapReady: _onMapReady,
                                  onPositionChanged: _onPositionChanged,
                                  onPinTapped: _onPinTapped,
                                  onCardSelected: _cubit.selectProvider,
                                  onRetry: _cubit.retry,
                                  onMyLocation: _cubit.useMyLocation,
                                  onDismissNotice: _cubit.dismissNotice,
                                  searchAreaOffered: _searchAreaOffered,
                                  onSearchThisArea: _searchVisibleArea,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      if (_suggestions.isNotEmpty)
                        Positioned.fill(
                          child: Align(
                            alignment: AlignmentDirectional.topCenter,
                            child: _AreaSuggestions(
                              suggestions: _suggestions,
                              onSelected: (place) {
                                FocusScope.of(context).unfocus();
                                setState(() => _suggestions = const []);
                                _searchController.text = place.label;
                                _cubit.goToPlace(place);
                              },
                            ),
                          ),
                        ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Rounded pill search field. The customer types a city, village or province
/// and picks from what the catalogue's place search suggests; the cubit
/// re-centres the map on it.
///
/// The pill draws the only border. The app theme gives every field an outlined
/// enabled and focused border, so switching off `border` alone left a second
/// box inside the pill; every state is switched off here.
class _AreaSearchField extends StatelessWidget {
  final TextEditingController controller;
  final ValueChanged<String> onSubmitted;
  final ValueChanged<String> onChanged;

  const _AreaSearchField({
    required this.controller,
    required this.onSubmitted,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsetsDirectional.fromSTEB(
        AppSpacing.md,
        AppSpacing.sm,
        AppSpacing.md,
        AppSpacing.xxs,
      ),
      child: Material(
        key: const Key('map-area-search-pill'),
        color: theme.colorScheme.surfaceContainerHighest,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.full),
          side: BorderSide(color: theme.dividerColor),
        ),
        clipBehavior: Clip.antiAlias,
        child: ConstrainedBox(
          constraints: const BoxConstraints(minHeight: 48),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
            child: Row(
              children: [
                Icon(
                  Icons.search,
                  size: AppIconSize.action,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: TextField(
                    key: const Key('map-area-search-field'),
                    controller: controller,
                    textInputAction: TextInputAction.search,
                    onSubmitted: onSubmitted,
                    onChanged: onChanged,
                    style: theme.textTheme.bodyMedium,
                    decoration: const InputDecoration(
                      hintText: AppStrings.mapAreaSearchHint,
                      border: InputBorder.none,
                      enabledBorder: InputBorder.none,
                      focusedBorder: InputBorder.none,
                      disabledBorder: InputBorder.none,
                      errorBorder: InputBorder.none,
                      focusedErrorBorder: InputBorder.none,
                      filled: false,
                      isDense: true,
                      contentPadding:
                          EdgeInsets.symmetric(vertical: AppSpacing.sm),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// The places suggested for what is typed, floating under the search field.
/// A labelled container, so a screen reader announces the list before its
/// items; each item is a tappable list tile.
class _AreaSuggestions extends StatelessWidget {
  final List<PlaceSuggestion> suggestions;
  final ValueChanged<PlaceSuggestion> onSelected;

  const _AreaSuggestions({
    required this.suggestions,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: Semantics(
        key: const Key('map-area-suggestions'),
        container: true,
        explicitChildNodes: true,
        label: AppStrings.mapAreaSuggestionsLabel,
        child: Material(
          color: theme.colorScheme.surface,
          elevation: AppElevation.medium,
          borderRadius: BorderRadius.circular(AppRadius.md),
          clipBehavior: Clip.antiAlias,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                for (final place in suggestions)
                  ListTile(
                    key: Key('map-area-suggestion-${place.label}'),
                    dense: true,
                    minTileHeight: 48,
                    leading: Icon(
                      Icons.place_outlined,
                      size: AppIconSize.action,
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                    title: Text(
                      place.label,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.bodySmall,
                    ),
                    onTap: () => onSelected(place),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// The map itself plus everything floating over it.
class _MapSurface extends StatelessWidget {
  final MapDiscoveryState state;
  final DateTime? now;
  final MapController mapController;
  final PageController pageController;
  final TileProvider? tileProvider;
  final VoidCallback onMapReady;
  final void Function(MapCamera camera, bool hasGesture) onPositionChanged;
  final ValueChanged<MapPinGroup> onPinTapped;
  final ValueChanged<String> onCardSelected;
  final VoidCallback onRetry;
  final VoidCallback onMyLocation;
  final VoidCallback onDismissNotice;
  final bool searchAreaOffered;
  final VoidCallback onSearchThisArea;

  const _MapSurface({
    required this.state,
    required this.now,
    required this.mapController,
    required this.pageController,
    required this.tileProvider,
    required this.onMapReady,
    required this.onPositionChanged,
    required this.onPinTapped,
    required this.onCardSelected,
    required this.onRetry,
    required this.onMyLocation,
    required this.onDismissNotice,
    required this.searchAreaOffered,
    required this.onSearchThisArea,
  });

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Positioned.fill(
          // The map canvas is a geographic plane, not a document: mirroring it
          // would put east on the left. Only the chrome around it is RTL.
          child: Directionality(
            textDirection: TextDirection.ltr,
            child: FlutterMap(
              key: const Key('map-canvas'),
              mapController: mapController,
              options: MapOptions(
                initialCenter: LatLng(state.latitude, state.longitude),
                initialZoom: MapDiscoveryPage.initialZoom,
                minZoom: MapDiscoveryPage.minZoom,
                maxZoom: MapDiscoveryPage.maxZoom,
                onMapReady: onMapReady,
                onPositionChanged: onPositionChanged,
                interactionOptions: const InteractionOptions(
                  // Rotation is off: a rotated map makes the Persian chrome
                  // and the pins disagree about which way is up, and buys
                  // nothing for finding a salon.
                  flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
                ),
              ),
              children: [
                TileLayer(
                  urlTemplate: MapDiscoveryPage.tileUrlTemplate,
                  userAgentPackageName: MapDiscoveryPage.userAgentPackageName,
                  tileProvider: tileProvider,
                  maxNativeZoom: 19,
                ),
                _ClusteredPinLayer(
                  providers: state.providers,
                  selectedProviderId: state.selectedProviderId,
                  onPinTapped: onPinTapped,
                ),
              ],
            ),
          ),
        ),
        if (searchAreaOffered)
          Positioned(
            top: AppSpacing.sm,
            left: 0,
            right: 0,
            child: Center(
              child: _SearchThisAreaButton(onPressed: onSearchThisArea),
            ),
          ),
        Positioned(
          left: 0,
          right: 0,
          bottom: 0,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (state.notice != MapNotice.none)
                _MapNoticeBar(
                  notice: state.notice,
                  onDismiss: onDismissNotice,
                ),
              Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.md,
                  vertical: AppSpacing.xs,
                ),
                child: Row(
                  // Forced LTR for this row only (each child keeps rendering its own text
                  // RTL — this just fixes which physical side they sit on): map controls are a
                  // geographic-canvas convention, not chrome text, and the design calls for the
                  // locate-me button at the bottom-*right* specifically, the same corner every
                  // map app (Google/Apple Maps included) uses regardless of locale.
                  textDirection: TextDirection.ltr,
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    // Flexible: on a narrow phone at a large text scale the
                    // credit wraps instead of pushing the button off screen.
                    const Flexible(child: _MapAttribution()),
                    const SizedBox(width: AppSpacing.xs),
                    _MyLocationButton(onPressed: onMyLocation),
                  ],
                ),
              ),
              _BottomPanel(
                state: state,
                now: now,
                pageController: pageController,
                onCardSelected: onCardSelected,
                onRetry: onRetry,
              ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Rebuilds against the live camera (via [MapCamera.of]) so the grid is
/// recomputed on every pan and zoom — which is what makes clusters split apart
/// as the customer zooms in.
class _ClusteredPinLayer extends StatelessWidget {
  final List<ProviderSummary> providers;
  final String? selectedProviderId;
  final ValueChanged<MapPinGroup> onPinTapped;

  const _ClusteredPinLayer({
    required this.providers,
    required this.selectedProviderId,
    required this.onPinTapped,
  });

  @override
  Widget build(BuildContext context) {
    final camera = MapCamera.of(context);
    final pins = clusterProviders(
      providers: providers,
      project: (provider) => camera.latLngToScreenOffset(
        LatLng(provider.latitude!, provider.longitude!),
      ),
      cellSize: kMapPinSize,
    );

    // The selected pin is drawn last so it is never hidden underneath a
    // neighbour.
    pins.sort((a, b) {
      final aSelected = a.contains(selectedProviderId) ? 1 : 0;
      final bSelected = b.contains(selectedProviderId) ? 1 : 0;
      return aSelected - bSelected;
    });

    return MarkerLayer(
      markers: [
        for (final pin in pins)
          Marker(
            key: Key('map-pin-${pin.id}'),
            point: LatLng(pin.latitude, pin.longitude),
            width: kMapPinSize,
            height: kMapPinSize,
            alignment: Alignment.topCenter,
            child: pin.isCluster
                ? MapClusterPin(
                    count: pin.count,
                    selected: pin.contains(selectedProviderId),
                    onTap: () => onPinTapped(pin),
                  )
                : MapProviderPin(
                    category: pin.provider.category,
                    selected: pin.contains(selectedProviderId),
                    semanticLabel: AppStrings.mapPinLabel(pin.provider.name),
                    onTap: () => onPinTapped(pin),
                  ),
          ),
      ],
    );
  }
}

class _SearchThisAreaButton extends StatelessWidget {
  final VoidCallback onPressed;

  const _SearchThisAreaButton({required this.onPressed});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Material(
      color: theme.colorScheme.surface,
      elevation: AppElevation.medium,
      borderRadius: BorderRadius.circular(AppRadius.full),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        key: const Key('map-search-this-area'),
        onTap: onPressed,
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.xs,
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.refresh,
                size: AppIconSize.action,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(width: AppSpacing.xs),
              Text(
                AppStrings.mapSearchThisArea,
                style: theme.textTheme.labelLarge
                    ?.copyWith(color: theme.colorScheme.primary),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _MyLocationButton extends StatelessWidget {
  final VoidCallback onPressed;

  const _MyLocationButton({required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return FloatingActionButton.small(
      key: const Key('map-my-location-fab'),
      // No hero: the shell can hold another FAB, and two would collide.
      heroTag: null,
      tooltip: AppStrings.mapMyLocation,
      onPressed: onPressed,
      child: const Icon(Icons.my_location),
    );
  }
}

/// OpenStreetMap's usage policy requires the attribution to stay visible.
class _MapAttribution extends StatelessWidget {
  const _MapAttribution();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.xs,
        vertical: AppSpacing.xxs,
      ),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface.withValues(alpha: 0.85),
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Text(
        '© ${AppStrings.mapAttribution}',
        style: theme.textTheme.bodySmall,
        // OpenStreetMap's credit must stay readable: wrap, never cut.
        maxLines: 2,
      ),
    );
  }
}

class _MapNoticeBar extends StatelessWidget {
  final MapNotice notice;
  final VoidCallback onDismiss;

  const _MapNoticeBar({required this.notice, required this.onDismiss});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final message = switch (notice) {
      MapNotice.none => '',
      MapNotice.permissionDenied => AppStrings.locationPermissionNeeded,
      MapNotice.serviceDisabled => AppStrings.locationServiceDisabled,
      MapNotice.locationUnavailable => AppStrings.mapLocationFallbackNotice,
      MapNotice.locationImprecise => AppStrings.mapLocationImpreciseNotice,
      MapNotice.areaNotFound => AppStrings.mapAreaNotFound,
    };

    return Container(
      key: const Key('map-notice'),
      margin: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: theme.colorScheme.secondaryContainer,
        borderRadius: BorderRadius.circular(AppRadius.md),
      ),
      child: Row(
        children: [
          Icon(
            Icons.info_outline,
            size: AppIconSize.action,
            color: theme.colorScheme.onSecondaryContainer,
          ),
          const SizedBox(width: AppSpacing.xs),
          Expanded(
            child: Text(
              message,
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSecondaryContainer,
              ),
            ),
          ),
          IconButton(
            key: const Key('map-notice-dismiss'),
            icon: const Icon(Icons.close),
            iconSize: AppIconSize.action,
            tooltip: AppStrings.cancel,
            onPressed: onDismiss,
          ),
        ],
      ),
    );
  }
}

/// The bottom band over the map: the provider carousel, or the loading /
/// empty / error stand-in for it. Never blank — the map stays visible behind
/// whichever of these is showing.
class _BottomPanel extends StatelessWidget {
  final MapDiscoveryState state;
  final DateTime? now;
  final PageController pageController;
  final ValueChanged<String> onCardSelected;
  final VoidCallback onRetry;

  const _BottomPanel({
    required this.state,
    required this.now,
    required this.pageController,
    required this.onCardSelected,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    // A pan or a filter change keeps the previous pins and cards on screen
    // while the next page loads — only the very first load has nothing to show.
    final showSkeleton = state.status == MapDiscoveryStatus.loading &&
        state.providers.isEmpty;

    if (showSkeleton) {
      return _BottomBand(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
          child: SkeletonLoader(
            child: SkeletonLoader.box(height: 116, radius: AppRadius.card),
          ),
        ),
      );
    }

    if (state.providers.isEmpty) {
      final isError = state.status == MapDiscoveryStatus.error;
      return _BottomBand(
        child: _MessagePanel(
          key: Key(isError ? 'map-error-panel' : 'map-empty-panel'),
          icon: isError ? Icons.error_outline : Icons.search_off,
          title: isError
              ? (state.errorMessage ?? AppStrings.genericError)
              : AppStrings.mapEmptyTitle,
          subtitle: isError ? null : AppStrings.mapEmptySubtitle,
          onRetry: onRetry,
        ),
      );
    }

    return _BottomBand(
      child: SizedBox(
        // Sized to the card at the current text scale: a PageView needs a
        // fixed height, and a fixed 148 clipped the card at 1.3x.
        height: MapProviderCard.bandHeight(MediaQuery.textScalerOf(context)),
        child: Semantics(
          container: true,
          label: AppStrings.mapProvidersCarouselLabel,
          child: PageView.builder(
            key: const Key('map-provider-carousel'),
            controller: pageController,
            padEnds: true,
            itemCount: state.providers.length,
            onPageChanged: (index) =>
                onCardSelected(state.providers[index].id),
            itemBuilder: (context, index) {
              final provider = state.providers[index];
              return Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xs),
                child: Align(
                  alignment: Alignment.bottomCenter,
                  child: MapProviderCard(
                    key: Key('map-card-${provider.id}'),
                    provider: provider,
                    selected: provider.id == state.selectedProviderId,
                    now: now,
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

/// Bottom padding shared by every state of the band, so the carousel and its
/// stand-ins sit at exactly the same height.
class _BottomBand extends StatelessWidget {
  final Widget child;

  const _BottomBand({required this.child});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: child,
    );
  }
}

/// Compact empty/error card. Deliberately not [EmptyState]/[ErrorState]: those
/// are full-screen centred layouts, and here the map must stay visible behind
/// the message.
class _MessagePanel extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? subtitle;
  final VoidCallback onRetry;

  const _MessagePanel({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: AppCard(
        padding: const EdgeInsets.all(AppSpacing.sm),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              icon,
              size: AppIconSize.md,
              color: theme.colorScheme.onSurfaceVariant,
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(title, style: theme.textTheme.titleSmall),
                  if (subtitle != null) ...[
                    const SizedBox(height: AppSpacing.xxs),
                    Text(subtitle!, style: theme.textTheme.bodySmall),
                  ],
                ],
              ),
            ),
            const SizedBox(width: AppSpacing.xs),
            AppButton.text(
              key: const Key('map-retry'),
              label: AppStrings.retry,
              onPressed: onRetry,
            ),
          ],
        ),
      ),
    );
  }
}
