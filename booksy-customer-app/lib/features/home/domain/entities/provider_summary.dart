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
  });

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
      ];
}
