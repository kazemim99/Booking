import 'package:flutter/material.dart';

import '../../../../core/constants/app_strings.dart';

/// One browsable service category: what the customer reads, what the API is
/// given, and the line icon used on the home tile row.
///
/// Display text and wire value are separate on purpose. Sending the Persian
/// label as `ServiceCategory` produced
/// `HTTP 500 — Invalid non-ASCII or control character in header` and broke
/// every category filter, so [apiValue] is always the backend
/// `ServiceCategory` enum name.
class ServiceCategoryChoice {
  final String label;
  final String apiValue;
  final IconData icon;

  const ServiceCategoryChoice({
    required this.label,
    required this.apiValue,
    required this.icon,
  });
}

/// The single canonical category list, shared by the home tile row and the
/// explore filter chips so the two can never drift apart (and so a home tile
/// always deep-links to a category explore actually accepts).
///
/// Each [ServiceCategoryChoice.apiValue] appears exactly once: two chips
/// mapping to the same enum name would both highlight as selected. Skincare has
/// no dedicated backend category, so it is folded into `BeautySalon`, which is
/// the closest one the catalog offers.
const List<ServiceCategoryChoice> kServiceCategories = [
  ServiceCategoryChoice(
    label: AppStrings.categoryBarbershop,
    apiValue: 'Barbershop',
    icon: Icons.content_cut,
  ),
  ServiceCategoryChoice(
    label: AppStrings.categoryHairSalon,
    apiValue: 'HairSalon',
    icon: Icons.face_outlined,
  ),
  ServiceCategoryChoice(
    label: AppStrings.categorySpa,
    apiValue: 'Spa',
    icon: Icons.spa_outlined,
  ),
  ServiceCategoryChoice(
    label: AppStrings.categoryNailSalon,
    apiValue: 'NailSalon',
    icon: Icons.back_hand_outlined,
  ),
  ServiceCategoryChoice(
    label: AppStrings.categoryBeautySalon,
    apiValue: 'BeautySalon',
    icon: Icons.water_drop_outlined,
  ),
  ServiceCategoryChoice(
    label: AppStrings.categoryMassage,
    apiValue: 'Massage',
    icon: Icons.self_improvement_outlined,
  ),
];

/// How many categories the home tile row shows before the "more" tile.
const int kHomeCategoryTileCount = 5;
