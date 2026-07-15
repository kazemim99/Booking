import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/persian_text.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_loading.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../data/datasources/geocoding_service.dart';
import '../../data/datasources/location_api_service.dart';
import '../../data/models/location_models.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../widgets/step_scaffold.dart';

/// Step 3 — address. Mirrors the Vue LocationStep:
/// * ONE searchable "all cities" dropdown (no province field — the province is
///   derived from the picked city and still sent to the backend).
/// * A tap-to-pin map (OpenStreetMap tiles via flutter_map) that reverse-
///   geocodes (OSM/Nominatim) to auto-fill a short, readable address.
///
/// Required to advance: a selected city + address line 1. Coordinates are
/// optional (the backend defaults lat/lng to 0).
///
/// Advancing from this step creates the organization draft on the server.
class LocationStep extends StatefulWidget {
  const LocationStep({super.key});

  @override
  State<LocationStep> createState() => _LocationStepState();
}

class _LocationStepState extends State<LocationStep> {
  /// Geographic center of Iran — the neutral initial focus before the user has
  /// picked anything (no Tehran bias).
  static const LatLng _iranCenter = LatLng(32.4279, 53.6880);

  /// Semantic zoom levels (OSM scale). The map opens country-wide (there is no
  /// location yet), flies to city level once a city is picked, and sits at
  /// street level whenever a precise pin is known (restored draft / map tap).
  static const double _countryZoom = 5;
  static const double _cityZoom = 12;
  static const double _streetZoom = 16;

  /// Cap the dropdown options so a ~5k-city list stays responsive.
  static const int _maxOptions = 50;

  late final TextEditingController _line1;
  late final TextEditingController _cityCtrl;
  late final FocusNode _cityFocus;
  late final MapController _mapController;

  final LocationApiService _locationApi = getIt<LocationApiService>();
  final GeocodingService _geocoding = getIt<GeocodingService>();

  List<CityOption> _cities = const [];
  bool _loadingCities = true;
  bool _citiesFailed = false;

  /// Cities matching the current query, shown inline under the field.
  List<CityOption> _cityMatches = const [];
  CityOption? _selectedCity;
  LatLng? _pin;
  bool _isGeocoding = false;

  /// The inline results list is visible whenever the user is searching, i.e.
  /// there is query text and no city has been committed for it yet.
  bool get _showCityList =>
      _selectedCity == null &&
      _cityCtrl.text.trim().isNotEmpty &&
      _cityMatches.isNotEmpty;

  @override
  void initState() {
    super.initState();
    final a = context.read<OnboardingCubit>().state.data.address;
    _line1 = TextEditingController(text: a.addressLine1);
    _cityCtrl = TextEditingController();
    _cityFocus = FocusNode();
    _mapController = MapController();
    if (a.latitude != null && a.longitude != null &&
        (a.latitude != 0 || a.longitude != 0)) {
      _pin = LatLng(a.latitude!, a.longitude!);
    }
    _loadCities(restoreCity: a.city);
  }

  @override
  void dispose() {
    _line1.dispose();
    _cityCtrl.dispose();
    _cityFocus.dispose();
    _mapController.dispose();
    super.dispose();
  }

  Future<void> _loadCities({required String restoreCity}) async {
    try {
      final cities = await _locationApi.getAllCities();
      if (!mounted) return;
      setState(() {
        _cities = cities;
        _loadingCities = false;
        // Re-select the draft-restored city (matched by name) so navigating
        // back into this step keeps the field populated.
        if (restoreCity.isNotEmpty) {
          for (final c in cities) {
            if (c.name == restoreCity) {
              _selectedCity = c;
              _cityCtrl.text = c.label;
              break;
            }
          }
        }
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _loadingCities = false;
        _citiesFailed = true;
      });
    }
  }

  /// Filters the inline city list as the user types. Editing the text after a
  /// selection clears the selection so the results reappear.
  void _onCityQueryChanged(String value) {
    final query = value.trim();
    setState(() {
      if (_selectedCity != null && value != _selectedCity!.label) {
        _selectedCity = null;
      }
      _cityMatches = query.isEmpty
          ? const []
          : _cities
              .where((c) => PersianText.contains(c.label, query))
              .take(_maxOptions)
              .toList();
    });
    _commit();
  }

  void _retryLoadCities() {
    setState(() {
      _loadingCities = true;
      _citiesFailed = false;
    });
    _loadCities(
      restoreCity: context.read<OnboardingCubit>().state.data.address.city,
    );
  }

  Future<void> _selectCity(CityOption city) async {
    _cityFocus.unfocus();
    setState(() {
      _selectedCity = city;
      _cityCtrl.text = city.label;
      _cityMatches = const [];
      _isGeocoding = true;
    });
    _commit();

    // Recenter the map on the picked city (parity with the Vue watcher). Use
    // "city, province" for better disambiguation, and only move the pin if the
    // user hasn't already dropped one manually.
    final coords = await _geocoding.geocode('${city.name}, ${city.provinceName}');
    if (!mounted) return;
    setState(() {
      if (coords != null) {
        final target = LatLng(coords.lat, coords.lng);
        // Fly to city level so the whole city is visible to orient and refine.
        _mapController.move(target, _cityZoom);
        // Drop a pin at the city center; the user can tap to refine it.
        _pin = target;
      }
      _isGeocoding = false;
    });
    _commit();
  }

  Future<void> _onMapTap(LatLng point) async {
    // Center on the tapped point. Zoom in to street level on the first tap from
    // a wider view so the choice is precise; leave the zoom alone once the user
    // is already close so repeated taps only fine-tune the pin.
    final zoom = _mapController.camera.zoom < _streetZoom
        ? _streetZoom
        : _mapController.camera.zoom;
    setState(() {
      _pin = point;
      _isGeocoding = true;
    });
    _mapController.move(point, zoom);
    _commit();

    final result = await _geocoding.reverseGeocode(point.latitude, point.longitude);
    if (!mounted) return;
    if (result != null) {
      // Only overwrite the address when the geocode actually returned one —
      // never wipe what the user typed (parity with the Vue picker).
      if (result.hasAddress) _line1.text = result.formattedAddress;
    }
    setState(() => _isGeocoding = false);
    _commit();
  }

  void _commit() {
    context.read<OnboardingCubit>().updateAddress(
          OnboardingAddress(
            addressLine1: _line1.text.trim(),
            city: _selectedCity?.name ?? '',
            // Province is derived from the picked city, never entered by hand.
            province: _selectedCity?.provinceName ?? '',
            latitude: _pin?.latitude,
            longitude: _pin?.longitude,
          ),
        );
  }

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    final saving = context.select<OnboardingCubit, bool>((c) => c.state.isSaving);
    return StepScaffold(
      title: AppStrings.locationTitle,
      subtitle: AppStrings.locationSubtitle,
      loading: saving,
      onBack: cubit.back,
      onNext: () {
        _commit();
        cubit.next();
      },
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _buildCityField(),
          const SizedBox(height: AppSpacing.md),
          _buildMap(),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _line1,
            label: AppStrings.addressLine1,
            keyboardType: TextInputType.multiline,
            minLines: 3,
            maxLines: 5,
            onChanged: (_) => _commit(),
            key: const Key('onboarding-address-line1'),
          ),
        ],
      ),
    );
  }

  Widget _buildCityField() {
    if (_loadingCities) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: AppLoading(size: 18, message: AppStrings.citiesLoading),
      );
    }
    if (_citiesFailed) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: AppErrorState(
          message: AppStrings.cityLoadError,
          onRetry: _retryLoadCities,
        ),
      );
    }

    // Inline searchable field: the results render directly under the field
    // (not in an overlay), so matches are always visible when typing.
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          key: const Key('onboarding-city'),
          controller: _cityCtrl,
          focusNode: _cityFocus,
          onChanged: _onCityQueryChanged,
          decoration: InputDecoration(
            labelText: AppStrings.city,
            hintText: AppStrings.cityHint,
            prefixIcon: const Icon(Icons.location_city_outlined),
            suffixIcon: _cityCtrl.text.isEmpty
                ? null
                : IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () {
                      setState(() {
                        _cityCtrl.clear();
                        _selectedCity = null;
                        _cityMatches = const [];
                      });
                      _commit();
                    },
                  ),
          ),
        ),
        if (_showCityList)
          Container(
            key: const Key('onboarding-city-results'),
            margin: const EdgeInsets.only(top: AppSpacing.xs),
            constraints: const BoxConstraints(maxHeight: 240),
            decoration: BoxDecoration(
              border: Border.all(color: Theme.of(context).colorScheme.outline),
              borderRadius: BorderRadius.circular(AppRadius.md),
            ),
            child: ListView.builder(
              padding: EdgeInsets.zero,
              shrinkWrap: true,
              itemCount: _cityMatches.length,
              itemBuilder: (context, i) => ListTile(
                dense: true,
                title: Text(_cityMatches[i].label),
                onTap: () => _selectCity(_cityMatches[i]),
              ),
            ),
          ),
      ],
    );
  }

  Widget _buildMap() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(AppStrings.mapLabel,
            style: Theme.of(context).textTheme.bodyMedium),
        const SizedBox(height: AppSpacing.xs),
        Text(
          AppStrings.mapHint,
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
        ),
        const SizedBox(height: AppSpacing.sm),
        ClipRRect(
          borderRadius: BorderRadius.circular(AppRadius.md),
          child: SizedBox(
            height: 260,
            child: Stack(
              children: [
                FlutterMap(
                  key: const Key('onboarding-map'),
                  mapController: _mapController,
                  options: MapOptions(
                    // A known pin (restored draft) opens at street level;
                    // otherwise show all of Iran to invite navigation.
                    initialCenter: _pin ?? _iranCenter,
                    initialZoom: _pin != null ? _streetZoom : _countryZoom,
                    minZoom: 4,
                    maxZoom: 18,
                    onTap: (_, point) => _onMapTap(point),
                  ),
                  children: [
                    TileLayer(
                      urlTemplate:
                          'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                      userAgentPackageName: 'com.booksy.provider',
                    ),
                    if (_pin != null)
                      MarkerLayer(
                        markers: [
                          Marker(
                            point: _pin!,
                            width: 40,
                            height: 40,
                            alignment: Alignment.topCenter,
                            child: const Icon(
                              Icons.location_on,
                              // Coliride map-pin green.
                              color: AppColors.mapGreen,
                              size: 40,
                            ),
                          ),
                        ],
                      ),
                  ],
                ),
                if (_isGeocoding)
                  const Positioned(
                    top: 8,
                    left: 8,
                    child: AppLoading(size: 20),
                  ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
