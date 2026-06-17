import 'package:equatable/equatable.dart';

/// Favorite provider entity
class FavoriteProvider extends Equatable {
  final String providerId;
  final String providerName;
  final String? providerType;
  final String? logoUrl;
  final String? city;
  final String? state;
  final double? averageRating;
  final int? totalReviews;
  final DateTime addedAt;
  final String? notes;

  const FavoriteProvider({
    required this.providerId,
    required this.providerName,
    this.providerType,
    this.logoUrl,
    this.city,
    this.state,
    this.averageRating,
    this.totalReviews,
    required this.addedAt,
    this.notes,
  });

  @override
  List<Object?> get props => [
        providerId,
        providerName,
        providerType,
        logoUrl,
        city,
        state,
        averageRating,
        totalReviews,
        addedAt,
        notes,
      ];
}
