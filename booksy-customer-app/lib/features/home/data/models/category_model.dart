import '../../../../core/api/models/category_models.dart';
import '../../domain/entities/category.dart';

/// Extension to convert CategoryDto to Category entity
extension CategoryMapper on PopularCategoryDto {
  Category toEntity() {
    return Category(
      id: slug, // Use slug as ID
      name: name,
      icon: icon,
      imageUrl: null, // Not provided in DTO
      providerCount: providerCount,
    );
  }
}
