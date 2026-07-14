import 'package:flutter/material.dart';
import 'app_colors.dart';

/// Typography system using Vazir Persian font.
///
/// Sizes are fixed logical pixels (no ScreenUtil `.sp`) so text respects the
/// OS font-scale setting via MediaQuery.textScaler. Use `.w/.h/.r` for layout
/// only, never for text.
class AppTextStyles {
  AppTextStyles._();

  // Font family
  static const String fontFamily = 'Vazir';

  // Font weights
  static const FontWeight regular = FontWeight.w400;
  static const FontWeight medium = FontWeight.w500;
  static const FontWeight semibold = FontWeight.w600;
  static const FontWeight bold = FontWeight.w700;

  // Heading styles
  static const TextStyle h1 = TextStyle(
    fontFamily: fontFamily,
    fontSize: 28,
    fontWeight: bold,
    color: AppColors.textPrimary,
    height: 1.2,
  );

  static const TextStyle h2 = TextStyle(
    fontFamily: fontFamily,
    fontSize: 22,
    fontWeight: semibold,
    color: AppColors.textPrimary,
    height: 1.3,
  );

  static const TextStyle h3 = TextStyle(
    fontFamily: fontFamily,
    fontSize: 18,
    fontWeight: bold,
    color: AppColors.textPrimary,
    height: 1.4,
  );

  // Body styles
  static const TextStyle body = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16,
    fontWeight: regular,
    color: AppColors.textPrimary,
    height: 1.5,
  );

  static const TextStyle bodyMedium = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16,
    fontWeight: medium,
    color: AppColors.textPrimary,
    height: 1.5,
  );

  static const TextStyle bodySemibold = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16,
    fontWeight: semibold,
    color: AppColors.textPrimary,
    height: 1.5,
  );

  // Caption and helper text
  static const TextStyle caption = TextStyle(
    fontFamily: fontFamily,
    fontSize: 14,
    fontWeight: regular,
    color: AppColors.textSecondary,
    height: 1.4,
  );

  static const TextStyle captionMedium = TextStyle(
    fontFamily: fontFamily,
    fontSize: 14,
    fontWeight: medium,
    color: AppColors.textSecondary,
    height: 1.4,
  );

  static const TextStyle small = TextStyle(
    fontFamily: fontFamily,
    fontSize: 12,
    fontWeight: regular,
    color: AppColors.textTertiary,
    height: 1.3,
  );

  // Button text
  static const TextStyle button = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16,
    fontWeight: semibold,
    height: 1.2,
  );

  static const TextStyle buttonSmall = TextStyle(
    fontFamily: fontFamily,
    fontSize: 14,
    fontWeight: medium,
    height: 1.2,
  );
}
