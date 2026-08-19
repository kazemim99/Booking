import 'package:equatable/equatable.dart';

/// Provider summary for home screen recommendations
class ProviderSummary extends Equatable {
  final String id;
  final String name;
  final String? imageUrl;
  final double rating;
  final int reviewCount;
  final double? distance; // in kilometers
  final int startingPrice;
  final bool isOpen;
  final String? closingTime;

  /// Where the business actually is. Nullable because only `/Providers/by-location` returns coordinates —
  /// `/Providers/search` does not — so a summary built from a plain search has none. The map needs them to
  /// place a pin; everything else ignores them.
  final double? latitude;
  final double? longitude;

  /// The provider's category as the API's `ServiceCategory` enum name (e.g. `Barbershop`), not a display
  /// label. Used to pick a pin glyph and to echo the active filter.
  final String? category;

  /// City / street line, when the payload carried an address.
  final String? addressLine;

  const ProviderSummary({
    required this.id,
    required this.name,
    this.imageUrl,
    required this.rating,
    required this.reviewCount,
    this.distance,
    required this.startingPrice,
    required this.isOpen,
    this.closingTime,
    this.latitude,
    this.longitude,
    this.category,
    this.addressLine,
  });

  /// True when this summary can be drawn on a map.
  bool get hasCoordinates => latitude != null && longitude != null;

  @override
  List<Object?> get props => [
        id,
        name,
        imageUrl,
        rating,
        reviewCount,
        distance,
        startingPrice,
        isOpen,
        closingTime,
        latitude,
        longitude,
        category,
        addressLine,
      ];
}
