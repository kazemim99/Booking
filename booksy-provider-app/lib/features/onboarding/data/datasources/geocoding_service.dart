import 'package:dio/dio.dart';

import '../../../../core/api/config/api_constants.dart';

import '../../../../core/utils/persian_digits.dart';

/// Result of a reverse-geocode lookup (coordinates → address).
class ReverseGeocodeResult {
  final String formattedAddress;
  final String city;
  final String state;
  final String postalCode;

  const ReverseGeocodeResult({
    this.formattedAddress = '',
    this.city = '',
    this.state = '',
    this.postalCode = '',
  });

  bool get hasAddress => formattedAddress.trim().isNotEmpty;
}

/// Geocoding through OUR API (`/v1/Geocoding/*`), which calls OpenStreetMap's
/// Nominatim server-side and caches the answers.
///
/// The app used to call nominatim.openstreetmap.org from the browser. That fails
/// wherever the user's network cannot reach that host — as it did in production
/// on 2026-09-19, while the server reached it in under a second — and it made
/// every visitor an unidentified client of a shared free service, which its
/// usage policy does not allow.
///
/// The response shape is Nominatim's, passed through unchanged, so the parsing
/// and address formatting below are untouched. Best-effort: any error returns
/// null so the user keeps whatever they typed.
class GeocodingService {
  final Dio _dio;

  GeocodingService(this._dio);

  Options get _options => Options(
    // The upstream returns JSON; make sure Dio parses it as a Map/List.
    responseType: ResponseType.json,
  );

  /// Forward geocode: resolve a place name (e.g. `"کاشان, اصفهان"`) to
  /// coordinates so the map can recenter when a city is picked.
  Future<({double lat, double lng})?> geocode(String term) async {
    if (term.trim().isEmpty) return null;
    try {
      final res = await _dio.get(
        ApiConstants.geocodingSearch,
        queryParameters: {'q': term, 'limit': 1},
        options: _options,
      );
      final data = res.data;
      if (data is List && data.isNotEmpty && data.first is Map) {
        final first = data.first as Map;
        final lat = double.tryParse('${first['lat']}');
        final lng = double.tryParse('${first['lon']}');
        if (lat != null && lng != null) return (lat: lat, lng: lng);
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  /// Reverse geocode: coordinates → a human address, used to auto-fill the
  /// address + postal code when the user drops a pin.
  Future<ReverseGeocodeResult?> reverseGeocode(double lat, double lng) async {
    try {
      final res = await _dio.get(
        ApiConstants.geocodingReverse,
        queryParameters: {'lat': lat, 'lon': lng},
        options: _options,
      );
      final data = res.data;
      if (data is! Map) return null;
      final address = data['address'];
      final addr = address is Map ? address : const {};
      // Iranian postal codes are 10 digits; Nominatim may return "13187-95656".
      final rawPostal = (addr['postcode'] ?? '').toString();
      final postal = rawPostal.replaceAll(RegExp(r'[^0-9]'), '');
      final city = (addr['city'] ?? addr['town'] ?? addr['village'] ?? '')
          .toString();
      final state = (addr['state'] ?? '').toString();
      return ReverseGeocodeResult(
        formattedAddress: formatAddress(
          addr,
          displayName: (data['display_name'] ?? '').toString(),
          city: city,
          state: state,
        ),
        city: city,
        state: state,
        postalCode: postal,
      );
    } catch (_) {
      return null;
    }
  }

  /// The address line in Iranian order, general to specific: area
  /// (neighbourhood), street/alley, plate, then the place's own name — e.g.
  /// «محله طالقانی، کوچه ۵ سهند». Built from Nominatim's structured `address`
  /// fields, because `display_name` runs the other way (alley first). The city
  /// and everything above it are left out: the city has its own field.
  ///
  /// Falls back to [shortenAddress] on `display_name`, reversed into the same
  /// general-to-specific order, when no structured part is usable.
  static String formatAddress(
    Map<dynamic, dynamic> addr, {
    String displayName = '',
    String city = '',
    String state = '',
  }) {
    String field(List<String> keys) {
      for (final key in keys) {
        final value = (addr[key] ?? '').toString().trim();
        if (value.isNotEmpty) return value;
      }
      return '';
    }

    final cityNames = {
      city,
      for (final k in const ['city', 'town', 'village'])
        (addr[k] ?? '').toString().trim(),
    }..removeWhere((c) => c.isEmpty);

    final area = field(const ['neighbourhood', 'quarter', 'suburb']);
    final street = field(const ['road', 'pedestrian', 'footway', 'path']);
    final plate = field(const ['house_number']);
    final place = field(const [
      'amenity',
      'shop',
      'office',
      'tourism',
      'building',
    ]);

    final parts = <String>[];
    void add(String part) {
      if (part.isEmpty || cityNames.contains(part) || parts.contains(part)) {
        return;
      }
      parts.add(part);
    }

    add(area);
    add(street);
    if (plate.isNotEmpty) add(plate.startsWith('پلاک') ? plate : 'پلاک $plate');
    add(place);

    if (parts.isNotEmpty) return parts.join('، ');
    final fallback = shortenAddress(displayName, city: city, state: state);
    return fallback.split('، ').reversed.join('، ');
  }

  /// Trims Nominatim's `display_name` down to the local, readable parts —
  /// building/POI, house number, street, neighbourhood, municipality — dropping
  /// the administrative tail (district/county/province), the postal code and the
  /// country. Keeps at most the first [maxParts] surviving parts.
  static String shortenAddress(
    String displayName, {
    String city = '',
    String state = '',
    int maxParts = 6,
  }) {
    final kept = <String>[];
    for (final raw in displayName.split(',')) {
      final part = raw.trim();
      if (part.isEmpty || _isAdministrative(part, city, state)) continue;
      kept.add(part);
      if (kept.length >= maxParts) break;
    }
    return kept.join('، ');
  }

  static bool _isAdministrative(String part, String city, String state) {
    if (part == 'ایران' || part.toLowerCase() == 'iran') return true;
    // Postal-code-like: 5+ digits or a dash-joined number (Persian or Western).
    final western = PersianDigits.toWestern(part);
    if (RegExp(r'\d{5,}').hasMatch(western) ||
        RegExp(r'\d+-\d+').hasMatch(western)) {
      return true;
    }
    // Administrative-division prefixes (province / county / district / sub-area).
    for (final p in const ['استان', 'شهرستان', 'بخش', 'ناحیه']) {
      if (part.startsWith(p)) return true;
    }
    // "منطقه ۶ شهر تهران" is administrative, but keep "شهرداری منطقه ...".
    if (part.startsWith('منطقه') && part.contains('شهر')) return true;
    if (city.isNotEmpty && part == city) return true;
    if (state.isNotEmpty && part == state) return true;
    return false;
  }
}
