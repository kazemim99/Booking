import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../pages/map_discovery_page.dart';

/// Where the salon is, on the profile: a small map with one tap to hand the
/// point to a navigation app (openspec/changes/customer-app-discovery-pass).
///
/// It sits inside «تماس و موقعیت» (`ContactLocationSection`), under the address
/// row, so it has no heading or address of its own (QA recording 2026-09-23 #7).
///
/// Renders nothing without coordinates — a map of nowhere is worse than no map.
/// The map here is deliberately not interactive: it is a picture of the place,
/// and every gesture on it belongs to the page that scrolls underneath.
class ProviderLocationCard extends StatelessWidget {
  final String businessName;
  final double? latitude;
  final double? longitude;

  /// Injected so a test can see which link was chosen without a browser.
  final Future<void> Function(String url)? openUrl;

  const ProviderLocationCard({
    super.key,
    required this.businessName,
    this.latitude,
    this.longitude,
    this.openUrl,
  });

  static const double _mapHeight = 160;
  static const double _zoom = 16;

  /// First the phone's own chooser of installed map apps, as other apps do
  /// (reviews-and-reschedule-round2 item 5) — a `geo:` link on Android, which
  /// the system offers to every installed navigator, and Apple Maps' link on
  /// iOS, which hands off to it or another installed handler. The platform is
  /// the device's, also on the web build (customer.nahalkmi.ir), so a phone's
  /// browser gets it too; a desktop has no such chooser and starts with the
  /// named apps. Then the map apps people here actually use, Google Maps, and
  /// Waze.
  static List<({String label, String url, bool system})> directionsFor(
    double lat,
    double lng, {
    TargetPlatform? platform,
  }) {
    final system = switch (platform ?? defaultTargetPlatform) {
      TargetPlatform.android => 'geo:$lat,$lng?q=$lat,$lng',
      TargetPlatform.iOS => 'https://maps.apple.com/?daddr=$lat,$lng',
      _ => null,
    };
    return [
      if (system != null)
        (label: AppStrings.directionsPhoneApps, url: system, system: true),
      (
        label: AppStrings.directionsNeshan,
        url: 'https://neshan.org/maps/@$lat,$lng,16z',
        system: false
      ),
      (
        label: AppStrings.directionsBalad,
        url: 'https://balad.ir/location?latitude=$lat&longitude=$lng&zoom=16',
        system: false
      ),
      (
        label: AppStrings.directionsGoogleMaps,
        url: 'https://www.google.com/maps/dir/?api=1&destination=$lat,$lng',
        system: false
      ),
      (
        label: AppStrings.directionsWaze,
        url: 'https://waze.com/ul?ll=$lat,$lng&navigate=yes',
        system: false
      ),
    ];
  }

  @override
  Widget build(BuildContext context) {
    final lat = latitude;
    final lng = longitude;
    if (lat == null || lng == null) return const SizedBox.shrink();

    final theme = Theme.of(context);
    final point = LatLng(lat, lng);

    return Column(
      key: const Key('provider-location-card'),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(AppRadius.lg),
          child: SizedBox(
            height: _mapHeight,
            // A geographic plane is never mirrored, whatever the page around it.
            child: Directionality(
              textDirection: TextDirection.ltr,
              child: FlutterMap(
                key: const Key('provider-location-map'),
                options: MapOptions(
                  initialCenter: point,
                  initialZoom: _zoom,
                  interactionOptions:
                      const InteractionOptions(flags: InteractiveFlag.none),
                ),
                children: [
                  TileLayer(
                    urlTemplate: MapDiscoveryPage.tileUrlTemplate,
                    userAgentPackageName: MapDiscoveryPage.userAgentPackageName,
                    maxNativeZoom: 19,
                  ),
                  MarkerLayer(
                    markers: [
                      Marker(
                        point: point,
                        width: 40,
                        height: 40,
                        child: Icon(
                          Icons.location_on,
                          size: 40,
                          color: theme.colorScheme.primary,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        OutlinedButton.icon(
          key: const Key('provider-directions'),
          onPressed: () => _chooseApp(context, lat, lng),
          icon: const Icon(Icons.directions_outlined),
          label: const Text(AppStrings.directionsAction),
        ),
      ],
    );
  }

  Future<void> _chooseApp(BuildContext context, double lat, double lng) async {
    final choice = await showModalBottomSheet<String>(
      context: context,
      builder: (sheetContext) => SafeArea(
        // Five choices and a title: scrolls rather than overflows on a short
        // screen or at a large text size.
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: Text(
                  AppStrings.directionsSheetTitle,
                  style: Theme.of(sheetContext).textTheme.titleMedium,
                ),
              ),
              for (final option in directionsFor(lat, lng))
                ListTile(
                  key: Key('directions-${option.label}'),
                  leading: Icon(option.system
                      ? Icons.apps_rounded
                      : Icons.navigation_outlined),
                  title: Text(option.label),
                  onTap: () => Navigator.of(sheetContext).pop(option.url),
                ),
            ],
          ),
        ),
      ),
    );
    if (choice == null) return;

    final open = openUrl ??
        (String url) async =>
            launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication);
    await open(choice);
  }
}
