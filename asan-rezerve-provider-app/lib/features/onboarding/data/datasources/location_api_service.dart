import 'package:dio/dio.dart';

import '../models/location_models.dart';

/// Loads the province/city hierarchy from the ServiceCatalog API and flattens
/// it to a single list of cities — parity with the Vue `useLocations`
/// composable (`GET /v1/Locations/hierarchy`, one call for every city).
class LocationApiService {
  final Dio _dio;
  LocationApiService(this._dio);

  static const String hierarchyPath = '/v1/Locations/hierarchy';

  /// All cities across all provinces, alphabetically ordered. Returns an empty
  /// list on failure so the location step degrades to manual entry rather than
  /// blocking onboarding.
  Future<List<CityOption>> getAllCities() async {
    final res = await _dio.get(hierarchyPath);
    return CityOption.listFromHierarchy(res.data);
  }
}
