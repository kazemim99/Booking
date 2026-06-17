import 'package:json_annotation/json_annotation.dart';

part 'category_models.g.dart';

/// Service Category
@JsonSerializable()
class CategoryDto {
  final String name;
  final String? description;
  final String? iconUrl;
  final String color;
  final String slug;
  final int? providerCount;
  final String? gradient;
  final int? displayOrder;

  CategoryDto({
    required this.name,
    this.description,
    this.iconUrl,
    required this.color,
    required this.slug,
    this.providerCount,
    this.gradient,
    this.displayOrder,
  });

  factory CategoryDto.fromJson(Map<String, dynamic> json) =>
      _$CategoryDtoFromJson(json);

  Map<String, dynamic> toJson() => _$CategoryDtoToJson(this);
}

/// Popular Category (extends CategoryDto with additional fields)
@JsonSerializable()
class PopularCategoryDto {
  final String name;
  final String slug;
  final String icon;
  final String gradient;
  final int providerCount;
  final String? description;
  final String? color;
  final int? displayOrder;

  PopularCategoryDto({
    required this.name,
    required this.slug,
    required this.icon,
    required this.gradient,
    required this.providerCount,
    this.description,
    this.color,
    this.displayOrder,
  });

  factory PopularCategoryDto.fromJson(Map<String, dynamic> json) =>
      _$PopularCategoryDtoFromJson(json);

  Map<String, dynamic> toJson() => _$PopularCategoryDtoToJson(this);
}
