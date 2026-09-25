import 'dart:async';

import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';

/// The device's current position, for centering the onboarding map.
///
/// Best effort by design: every failure — location services off, permission
/// denied, no fix within the time limit, unsupported platform — yields `null`,
/// and the caller keeps its default view. It never throws.
class DeviceLocationService {
  /// Upper bound on the whole lookup, including a permission prompt the user
  /// leaves unanswered, so the map never waits indefinitely.
  static const Duration _timeLimit = Duration(seconds: 15);

  Future<LatLng?> current() async {
    try {
      return await _lookup().timeout(_timeLimit, onTimeout: () => null);
    } catch (_) {
      return null;
    }
  }

  Future<LatLng?> _lookup() async {
    if (!await Geolocator.isLocationServiceEnabled()) return null;
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      return null;
    }
    final position = await Geolocator.getCurrentPosition(
      // City-block accuracy is plenty to center a map, and is faster than GPS.
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.medium,
      ),
    );
    return LatLng(position.latitude, position.longitude);
  }
}
