import 'package:dio/dio.dart';

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

/// Geocoding via OpenStreetMap's Nominatim service — keyless and consistent with
/// the OSM tiles used by the map picker. (The previous Neshan demo key was rate-
/// limited: `code 481 "API Key limit exceeded"`.) Persian output via
/// `accept-language=fa`; results biased to Iran. Best-effort: any error returns
/// null so the user keeps whatever they typed.
///
/// Nominatim's usage policy requires an identifying User-Agent and a light
/// request rate — fine for the occasional taps on an onboarding form.
class GeocodingService {
  final Dio _dio;

  static const String _base = 'https://nominatim.openstreetmap.org';
  static const String _userAgent = 'BooksyProviderApp/1.0 (onboarding)';

  GeocodingService(this._dio);

  Options get _options => Options(
        headers: {'User-Agent': _userAgent},
        // Nominatim returns JSON; make sure Dio parses it as a Map/List.
        responseType: ResponseType.json,
      );

  /// Forward geocode: resolve a place name (e.g. `"کاشان, اصفهان"`) to
  /// coordinates so the map can recenter when a city is picked.
  Future<({double lat, double lng})?> geocode(String term) async {
    if (term.trim().isEmpty) return null;
    try {
      final res = await _dio.get(
        '$_base/search',
        queryParameters: {
          'q': term,
          'format': 'jsonv2',
          'accept-language': 'fa',
          'countrycodes': 'ir',
          'limit': 1,
        },
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
        '$_base/reverse',
        queryParameters: {
          'lat': lat,
          'lon': lng,
          'format': 'jsonv2',
          'accept-language': 'fa',
          'addressdetails': 1,
        },
        options: _options,
      );
      final data = res.data;
      if (data is! Map) return null;
      final address = data['address'];
      final addr = address is Map ? address : const {};
      // Iranian postal codes are 10 digits; Nominatim may return "13187-95656".
      final rawPostal = (addr['postcode'] ?? '').toString();
      final postal = rawPostal.replaceAll(RegExp(r'[^0-9]'), '');
      final city =
          (addr['city'] ?? addr['town'] ?? addr['village'] ?? '').toString();
      final state = (addr['state'] ?? '').toString();
      return ReverseGeocodeResult(
        // Nominatim's `display_name` is very long (down to province + country).
        // Keep only the local, human-relevant parts.
        formattedAddress: shortenAddress(
          (data['display_name'] ?? '').toString(),
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
