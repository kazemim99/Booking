/// A single selectable city, flattened out of the province hierarchy.
///
/// Mirrors the Vue LocationStep, which flattens every city across every
/// province into ONE searchable list and shows `"City (Province)"`. The
/// province is never a separate field — it is derived from the picked city and
/// still sent to the backend (which requires it).
class CityOption {
  final int id;
  final String name;
  final String provinceName;

  const CityOption({
    required this.id,
    required this.name,
    required this.provinceName,
  });

  /// Label shown in the dropdown, e.g. `تهران (تهران)` — same as Vue.
  String get label => '$name ($provinceName)';

  /// Flattens the `/v1/Locations/hierarchy` payload
  /// (`[{ id, name, cities: [{ id, name }] }]`) into a flat, alphabetically
  /// ordered list of cities. The live backend wraps the array in an envelope
  /// (`{ success, statusCode, data: [...] }`) — both shapes are accepted.
  /// Malformed/partial entries are skipped defensively.
  static List<CityOption> listFromHierarchy(dynamic hierarchy) {
    // Unwrap the API envelope if present.
    if (hierarchy is Map && hierarchy['data'] is List) {
      hierarchy = hierarchy['data'];
    }
    if (hierarchy is! List) return const [];

    final cities = <CityOption>[];
    for (final province in hierarchy) {
      if (province is! Map) continue;
      final provinceName = (province['name'] ?? '').toString();
      final rawCities = province['cities'];
      if (rawCities is! List) continue;

      for (final city in rawCities) {
        if (city is! Map) continue;
        final id = city['id'];
        final name = (city['name'] ?? '').toString();
        if (id is! num || name.isEmpty) continue;
        cities.add(CityOption(
          id: id.toInt(),
          name: name,
          provinceName: provinceName,
        ));
      }
    }

    cities.sort((a, b) => a.name.compareTo(b.name));
    return cities;
  }
}
