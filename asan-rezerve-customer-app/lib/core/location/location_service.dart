import 'package:geolocator/geolocator.dart';

/// Outcome of a device-location request. Never surfaces a raw platform
/// exception — the cubit switches on these variants to decide whether to show
/// results, a permission prompt, or the manual area/district fallback.
sealed class LocationResult {
  const LocationResult();
}

class LocationSuccess extends LocationResult {
  final double latitude;
  final double longitude;

  /// How far off the fix may be, in metres. A browser with no GPS or Wi-Fi
  /// data falls back to the IP address — which, behind a VPN, is the exit
  /// country — and reports a radius of tens of kilometres. Callers use this to
  /// tell a real position from a guess.
  final double? accuracyMeters;

  const LocationSuccess(this.latitude, this.longitude, {this.accuracyMeters});
}

/// Permission denied (this run or forever) — the caller should fall back to
/// manual area/district search.
class LocationPermissionDenied extends LocationResult {
  const LocationPermissionDenied();
}

/// The OS location service (GPS) is turned off.
class LocationServiceDisabled extends LocationResult {
  const LocationServiceDisabled();
}

class LocationError extends LocationResult {
  final String message;
  const LocationError(this.message);
}

/// Reads the device's current position. The interface is abstract so nearby
/// search can be unit-tested with a fake, without platform channels.
abstract class LocationService {
  Future<LocationResult> currentPosition();
}

/// [LocationService] backed by the `geolocator` plugin. Maps every platform
/// failure mode to a [LocationResult] variant instead of throwing.
class GeolocatorLocationService implements LocationService {
  const GeolocatorLocationService();

  @override
  Future<LocationResult> currentPosition() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        return const LocationServiceDisabled();
      }

      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        return const LocationPermissionDenied();
      }

      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
      );
      return LocationSuccess(
        position.latitude,
        position.longitude,
        accuracyMeters: position.accuracy,
      );
    } catch (e) {
      return LocationError(e.toString());
    }
  }
}
