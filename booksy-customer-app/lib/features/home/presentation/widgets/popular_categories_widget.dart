import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../domain/entities/category.dart';

class PopularCategoriesWidget extends StatelessWidget {
  final List<Category> categories;
  final Function(Category) onCategoryTap;

  const PopularCategoriesWidget({
    super.key,
    required this.categories,
    required this.onCategoryTap,
  });

  @override
  Widget build(BuildContext context) {
    if (categories.isEmpty) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: EdgeInsets.symmetric(horizontal: 16.w),
          child: Text(
            'دسته‌بندی‌های محبوب',
            style: AppTextStyles.h3,
          ),
        ),
        SizedBox(height: 12.h),
        SizedBox(
          height: 110.h, // Increased height to accommodate text overflow
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            padding: EdgeInsets.symmetric(horizontal: 12.w),
            itemCount: categories.length,
            itemBuilder: (context, index) {
              final category = categories[index];
              return _CategoryItem(
                category: category,
                onTap: () => onCategoryTap(category),
              );
            },
          ),
        ),
      ],
    );
  }
}

class _CategoryItem extends StatelessWidget {
  final Category category;
  final VoidCallback onTap;

  const _CategoryItem({
    required this.category,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 85.w, // Increased width to prevent text overflow
        margin: EdgeInsets.symmetric(horizontal: 4.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Category Icon Circle
            Container(
              width: 64.w,
              height: 64.h,
              decoration: BoxDecoration(
                color: AppColors.surface,
                shape: BoxShape.circle,
                border: Border.all(
                  color: AppColors.border,
                  width: 1,
                ),
              ),
              child: Center(
                child: category.icon != null
                    ? Text(
                        category.icon!,
                        style: TextStyle(fontSize: 28.sp),
                      )
                    : Icon(
                        Icons.category,
                        size: 28.w,
                        color: AppColors.primary,
                      ),
              ),
            ),
            SizedBox(height: 8.h),
            // Category Name - Flexible to prevent overflow
            Flexible(
              child: Text(
                category.name,
                style: AppTextStyles.caption.copyWith(fontSize: 11.sp),
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
