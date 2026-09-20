import 'package:dio/dio.dart';

import '../api/config/api_constants.dart';

/// A resolved geographic coordinate.
class GeoCoordinates {
  final double latitude;
  final double longitude;
  const GeoCoordinates(this.latitude, this.longitude);
}

/// One place the search could mean: what to show, and where it is.
class PlaceSuggestion {
  final String label;
  final GeoCoordinates coordinates;

  const PlaceSuggestion({required this.label, required this.coordinates});
}

/// Resolves a place name (area / district / city) to coordinates.
/// Abstract so area/district search can be unit-tested with a fake.
abstract class GeocodingService {
  Future<GeoCoordinates?> geocode(String term);

  /// Places matching what has been typed so far, for the search suggestions.
  /// Empty when nothing matches or the lookup fails — suggestions are a help,
  /// never a blocker.
  Future<List<PlaceSuggestion>> suggest(String term);
}

/// Forward geocoding through OUR API (`/v1/Geocoding/search`), which proxies
/// Nominatim server-side with the required User-Agent and a 24-hour cache.
///
/// The browser must not call Nominatim itself: its usage policy expects an
/// identifying User-Agent a browser cannot set, and the provider app's direct
/// calls failed in production for exactly that reason. Best-effort throughout:
/// any error means no coordinates and no suggestions, never an exception.
class NominatimGeocodingService implements GeocodingService {
  final Dio _dio;

  NominatimGeocodingService(this._dio);

  @override
  Future<GeoCoordinates?> geocode(String term) async {
    final places = await suggest(term, limit: 1);
    return places.isEmpty ? null : places.first.coordinates;
  }

  @override
  Future<List<PlaceSuggestion>> suggest(String term, {int limit = 5}) async {
    if (term.trim().length < 2) return const [];
    try {
      final res = await _dio.get(
        ApiConstants.geocodingSearch,
        queryParameters: {'q': term.trim(), 'limit': limit},
      );
      final data = res.data is Map<String, dynamic>
          ? (res.data as Map<String, dynamic>)['data'] ?? res.data
          : res.data;
      if (data is! List) return const [];

      return data
          .whereType<Map>()
          .map((place) {
            final lat = double.tryParse('${place['lat']}');
            final lng = double.tryParse('${place['lon']}');
            final label = (place['display_name'] ?? place['name'] ?? '').toString();
            if (lat == null || lng == null || label.isEmpty) return null;
            return PlaceSuggestion(
              label: label,
              coordinates: GeoCoordinates(lat, lng),
            );
          })
          .whereType<PlaceSuggestion>()
          .toList();
    } catch (_) {
      return const [];
    }
  }
}
