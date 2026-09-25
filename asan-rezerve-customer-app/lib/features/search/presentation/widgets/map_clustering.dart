import 'dart:ui' show Offset;

import '../../../home/domain/entities/provider_summary.dart';

/// One pin on the map: either a single provider or a group of providers that
/// would otherwise overlap at the current zoom.
class MapPinGroup {
  /// Centroid of the members, in degrees.
  final double latitude;
  final double longitude;

  /// Members, in the order they arrived from the API (nearest first).
  final List<ProviderSummary> providers;

  const MapPinGroup({
    required this.latitude,
    required this.longitude,
    required this.providers,
  });

  bool get isCluster => providers.length > 1;
  int get count => providers.length;

  /// The single provider this pin represents. Only meaningful when
  /// [isCluster] is false.
  ProviderSummary get provider => providers.first;

  /// Stable across rebuilds for the same membership, so marker widgets keep
  /// their identity while the camera is idle.
  String get id => providers.map((p) => p.id).join('+');

  /// Whether the currently selected provider is inside this pin.
  bool contains(String? providerId) =>
      providerId != null && providers.any((p) => p.id == providerId);
}

/// Groups providers whose projected positions fall in the same screen-space
/// grid cell.
///
/// Deliberately hand-rolled rather than pulled in as a plugin: the flutter_map
/// clustering plugins available today do not resolve against flutter_map 8.x,
/// and pinning an incompatible one would be worse than ~30 lines of grid
/// bucketing. Grid clustering is O(n), deterministic (so widget tests are not
/// flaky) and re-evaluated on every camera change, which is what makes pins
/// split apart as the customer zooms in.
///
/// [project] converts a provider's coordinates to screen pixels — supplied by
/// the map camera in the app, and by a plain function in tests.
/// [cellSize] is the grid pitch in logical pixels; it should be roughly the
/// width of a pin so that two pins in the same cell really would overlap.
List<MapPinGroup> clusterProviders({
  required List<ProviderSummary> providers,
  required Offset Function(ProviderSummary provider) project,
  double cellSize = 72,
}) {
  assert(cellSize > 0, 'cellSize must be positive');

  // Insertion-ordered: the resulting pin list follows the API's ordering, so
  // the same input always produces the same output.
  final buckets = <String, List<ProviderSummary>>{};

  for (final provider in providers) {
    if (!provider.hasCoordinates) continue;
    final offset = project(provider);
    // Non-finite offsets happen when the map has not been laid out yet;
    // bucketing them would collapse every provider into one bogus pin.
    if (!offset.dx.isFinite || !offset.dy.isFinite) continue;

    final column = (offset.dx / cellSize).floor();
    final row = (offset.dy / cellSize).floor();
    buckets.putIfAbsent('$column:$row', () => []).add(provider);
  }

  return [
    for (final members in buckets.values)
      MapPinGroup(
        latitude: _mean(members.map((p) => p.latitude!)),
        longitude: _mean(members.map((p) => p.longitude!)),
        providers: List.unmodifiable(members),
      ),
  ];
}

double _mean(Iterable<double> values) {
  var sum = 0.0;
  var count = 0;
  for (final value in values) {
    sum += value;
    count++;
  }
  return count == 0 ? 0 : sum / count;
}
