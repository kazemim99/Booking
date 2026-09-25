import 'package:equatable/equatable.dart';

/// Recently visited provider entity
class RecentlyVisitedProvider extends Equatable {
  final String providerId;
  final String providerName;
  final String? providerType;
  final String? logoUrl;
  final String? city;
  final String? state;
  final double? averageRating;
  final int? totalReviews;
  final DateTime lastVisitedAt;
  final int visitCount;

  const RecentlyVisitedProvider({
    required this.providerId,
    required this.providerName,
    this.providerType,
    this.logoUrl,
    this.city,
    this.state,
    this.averageRating,
    this.totalReviews,
    required this.lastVisitedAt,
    required this.visitCount,
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
        lastVisitedAt,
        visitCount,
      ];
}
