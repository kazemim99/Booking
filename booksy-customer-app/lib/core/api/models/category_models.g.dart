// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'category_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CategoryDto _$CategoryDtoFromJson(Map<String, dynamic> json) => CategoryDto(
      name: json['name'] as String,
      description: json['description'] as String?,
      iconUrl: json['iconUrl'] as String?,
      color: json['color'] as String,
      slug: json['slug'] as String,
      providerCount: (json['providerCount'] as num?)?.toInt(),
      gradient: json['gradient'] as String?,
      displayOrder: (json['displayOrder'] as num?)?.toInt(),
    );

Map<String, dynamic> _$CategoryDtoToJson(CategoryDto instance) =>
    <String, dynamic>{
      'name': instance.name,
      'description': instance.description,
      'iconUrl': instance.iconUrl,
      'color': instance.color,
      'slug': instance.slug,
      'providerCount': instance.providerCount,
      'gradient': instance.gradient,
      'displayOrder': instance.displayOrder,
    };

PopularCategoryDto _$PopularCategoryDtoFromJson(Map<String, dynamic> json) =>
    PopularCategoryDto(
      name: json['name'] as String,
      slug: json['slug'] as String,
      icon: json['icon'] as String,
      gradient: json['gradient'] as String,
      providerCount: (json['providerCount'] as num).toInt(),
      description: json['description'] as String?,
      color: json['color'] as String?,
      displayOrder: (json['displayOrder'] as num?)?.toInt(),
    );

Map<String, dynamic> _$PopularCategoryDtoToJson(PopularCategoryDto instance) =>
    <String, dynamic>{
      'name': instance.name,
      'slug': instance.slug,
      'icon': instance.icon,
      'gradient': instance.gradient,
      'providerCount': instance.providerCount,
      'description': instance.description,
      'color': instance.color,
      'displayOrder': instance.displayOrder,
    };
