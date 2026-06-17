import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'app_colors.dart';

/// Typography system using Vazir Persian font
/// Responsive sizing with ScreenUtil
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
  static TextStyle h1 = TextStyle(
    fontFamily: fontFamily,
    fontSize: 28.sp,
    fontWeight: bold,
    color: AppColors.textPrimary,
    height: 1.2,
  );

  static TextStyle h2 = TextStyle(
    fontFamily: fontFamily,
    fontSize: 22.sp,
    fontWeight: semibold,
    color: AppColors.textPrimary,
    height: 1.3,
  );

  static TextStyle h3 = TextStyle(
    fontFamily: fontFamily,
    fontSize: 18.sp,
    fontWeight: bold, // Increased from semibold for stronger hierarchy
    color: AppColors.textPrimary,
    height: 1.4,
  );

  // Body styles
  static TextStyle body = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16.sp,
    fontWeight: regular,
    color: AppColors.textPrimary,
    height: 1.5,
  );

  static TextStyle bodyMedium = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16.sp,
    fontWeight: medium,
    color: AppColors.textPrimary,
    height: 1.5,
  );

  static TextStyle bodySemibold = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16.sp,
    fontWeight: semibold,
    color: AppColors.textPrimary,
    height: 1.5,
  );

  // Caption and helper text
  static TextStyle caption = TextStyle(
    fontFamily: fontFamily,
    fontSize: 14.sp,
    fontWeight: regular,
    color: AppColors.textSecondary,
    height: 1.4,
  );

  static TextStyle captionMedium = TextStyle(
    fontFamily: fontFamily,
    fontSize: 14.sp,
    fontWeight: medium,
    color: AppColors.textSecondary,
    height: 1.4,
  );

  static TextStyle small = TextStyle(
    fontFamily: fontFamily,
    fontSize: 12.sp,
    fontWeight: regular,
    color: AppColors.textTertiary,
    height: 1.3,
  );

  // Button text
  static TextStyle button = TextStyle(
    fontFamily: fontFamily,
    fontSize: 16.sp,
    fontWeight: semibold,
    height: 1.2,
  );

  static TextStyle buttonSmall = TextStyle(
    fontFamily: fontFamily,
    fontSize: 14.sp,
    fontWeight: medium,
    height: 1.2,
  );
}
