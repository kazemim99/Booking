import '../../../../core/api/models/provider_models.dart';
import '../../domain/entities/provider_summary.dart';

/// Extension to convert ProviderDto to ProviderSummary entity
extension ProviderMapper on ProviderDto {
  ProviderSummary toEntity() {
    return ProviderSummary(
      id: id,
      name: businessName,
      imageUrl: logoUrl ?? profileImageUrl,
      rating: averageRating ?? 0.0,
      reviewCount: totalReviews ?? 0,
      distance: null, // Not provided in search response
      startingPrice: 0, // Not provided in search response
      isOpen: status == 'Active', // Assume active providers are open
      closingTime: null, // Not provided in search response
    );
  }
}
