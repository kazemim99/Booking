import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../pages/map_discovery_page.dart';

/// Where the salon is, on the profile: its street address and a small map,
/// with one tap to hand the point to a navigation app
/// (openspec/changes/customer-app-discovery-pass).
///
/// Renders nothing without coordinates — a map of nowhere is worse than no map.
/// The map here is deliberately not interactive: it is a picture of the place,
/// and every gesture on it belongs to the page that scrolls underneath.
class ProviderLocationCard extends StatelessWidget {
  final String businessName;
  final String? address;
  final double? latitude;
  final double? longitude;

  /// Injected so a test can see which link was chosen without a browser.
  final Future<void> Function(String url)? openUrl;

  const ProviderLocationCard({
    super.key,
    required this.businessName,
    this.address,
    this.latitude,
    this.longitude,
    this.openUrl,
  });

  static const double _mapHeight = 160;
  static const double _zoom = 16;

  /// The map apps people here actually use, then Google Maps for everyone else.
  static List<({String label, String url})> directionsFor(double lat, double lng) => [
        (label: AppStrings.directionsNeshan, url: 'https://neshan.org/maps/@$lat,$lng,16z'),
        (
          label: AppStrings.directionsBalad,
          url: 'https://balad.ir/location?latitude=$lat&longitude=$lng&zoom=16'
        ),
        (
          label: AppStrings.directionsGoogleMaps,
          url: 'https://www.google.com/maps/dir/?api=1&destination=$lat,$lng'
        ),
      ];

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
        Text(AppStrings.locationOnMapTitle, style: theme.textTheme.titleLarge),
        const SizedBox(height: AppSpacing.xs),
        if (address != null && address!.isNotEmpty) ...[
          Text(address!, style: theme.textTheme.bodyMedium),
          const SizedBox(height: AppSpacing.sm),
        ],
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
                leading: const Icon(Icons.navigation_outlined),
                title: Text(option.label),
                onTap: () => Navigator.of(sheetContext).pop(option.url),
              ),
          ],
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
