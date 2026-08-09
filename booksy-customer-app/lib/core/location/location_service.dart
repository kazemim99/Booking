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
  const LocationSuccess(this.latitude, this.longitude);
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

      final position = await Geolocator.getCurrentPosition();
      return LocationSuccess(position.latitude, position.longitude);
    } catch (e) {
      return LocationError(e.toString());
    }
  }
}
