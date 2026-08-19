import '../../../home/domain/entities/provider_summary.dart';
import '../datasources/search_remote_datasource.dart';

/// Maps the `/Providers/by-location` payload onto the shared [ProviderSummary].
///
/// Kept separate from `ProviderMapper` (which maps `/Providers/search`'s
/// `ProviderDto`) because the two payloads carry different facts: this one is
/// the **only** source of coordinates and of a server-computed distance, and it
/// has no `profileImageUrl` — just `logoUrl`.
///
/// Nothing is invented here. `startingPrice` stays 0 and `reviewCount` stays 0
/// because the endpoint publishes neither; the shared widgets already hide the
/// rating and the price band when they have no real value, so a zero must never
/// be dressed up as data.
extension ProviderLocationMapper on ProviderLocationDto {
  ProviderSummary toEntity() {
    final address = [city, street]
        .where((part) => part != null && part.trim().isNotEmpty)
        .join('، ');

    return ProviderSummary(
      id: id,
      name: businessName,
      imageUrl: logoUrl,
      rating: averageRating ?? 0,
      reviewCount: 0,
      distance: distanceKm,
      startingPrice: 0,
      // The endpoint only ever returns active providers; open/closed for *today*
      // is not part of this payload, so nothing more precise can be said.
      isOpen: true,
      latitude: latitude,
      longitude: longitude,
      category: category,
      addressLine: address.isEmpty ? null : address,
    );
  }
}
