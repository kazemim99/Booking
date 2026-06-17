import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../../../core/utils/persian_formatter.dart';
import '../../../../core/utils/price_formatter.dart';
import '../../domain/entities/provider_summary.dart';

class TopProvidersWidget extends StatelessWidget {
  final List<ProviderSummary> providers;
  final Function(ProviderSummary) onProviderTap;

  const TopProvidersWidget({
    super.key,
    required this.providers,
    required this.onProviderTap,
  });

  @override
  Widget build(BuildContext context) {
    if (providers.isEmpty) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: EdgeInsets.symmetric(horizontal: 16.w),
          child: Text(
            'ارائه‌دهندگان برتر',
            style: AppTextStyles.h3,
          ),
        ),
        SizedBox(height: 12.h),
        SizedBox(
          height: 240.h,
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            padding: EdgeInsets.symmetric(horizontal: 12.w),
            itemCount: providers.length,
            itemBuilder: (context, index) {
              final provider = providers[index];
              return _ProviderCard(
                provider: provider,
                onTap: () => onProviderTap(provider),
              );
            },
          ),
        ),
      ],
    );
  }
}

class _ProviderCard extends StatelessWidget {
  final ProviderSummary provider;
  final VoidCallback onTap;

  const _ProviderCard({
    required this.provider,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 180.w,
        margin: EdgeInsets.symmetric(horizontal: 4.w),
        decoration: BoxDecoration(
          color: AppColors.background,
          border: Border.all(color: AppColors.border),
          borderRadius: BorderRadius.circular(16.r),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Provider Image
            ClipRRect(
              borderRadius: BorderRadius.vertical(top: Radius.circular(16.r)),
              child: provider.imageUrl != null
                  ? CachedNetworkImage(
                      imageUrl: provider.imageUrl!,
                      width: 180.w,
                      height: 120.h,
                      fit: BoxFit.cover,
                      placeholder: (context, url) => Container(
                        width: 180.w,
                        height: 120.h,
                        color: AppColors.surface,
                        child: const Center(
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
                          ),
                        ),
                      ),
                      errorWidget: (context, url, error) => _PlaceholderImage(),
                    )
                  : _PlaceholderImage(),
            ),
            // Provider Info
            Padding(
              padding: EdgeInsets.all(12.w),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Provider Name
                  Text(
                    provider.name,
                    style: AppTextStyles.h3.copyWith(fontSize: 14.sp),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  SizedBox(height: 6.h),
                  // Rating
                  Row(
                    children: [
                      Icon(
                        Icons.star,
                        size: 16.w,
                        color: AppColors.warning,
                      ),
                      SizedBox(width: 4.w),
                      Text(
                        provider.rating.toStringAsFixed(1).toPersianDigits(),
                        style: AppTextStyles.caption.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      SizedBox(width: 4.w),
                      Text(
                        '(${provider.reviewCount.toPersianString()} نظر)',
                        style: AppTextStyles.small.copyWith(
                          color: AppColors.textTertiary,
                        ),
                      ),
                    ],
                  ),
                  if (provider.distance != null) ...[
                    SizedBox(height: 4.h),
                    Row(
                      children: [
                        Icon(
                          Icons.location_on,
                          size: 14.w,
                          color: AppColors.textSecondary,
                        ),
                        SizedBox(width: 4.w),
                        Text(
                          '${provider.distance!.toStringAsFixed(1).toPersianDigits()} کیلومتر',
                          style: AppTextStyles.small,
                        ),
                      ],
                    ),
                  ],
                  SizedBox(height: 6.h),
                  // Price and Status
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        provider.startingPrice.formatPriceFrom(),
                        style: AppTextStyles.caption.copyWith(
                          fontWeight: FontWeight.w600,
                          color: AppColors.primary,
                        ),
                      ),
                      Container(
                        padding: EdgeInsets.symmetric(
                          horizontal: 8.w,
                          vertical: 4.h,
                        ),
                        decoration: BoxDecoration(
                          color: provider.isOpen
                              ? AppColors.success.withValues(alpha: 0.1)
                              : AppColors.error.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(6.r),
                        ),
                        child: Text(
                          provider.isOpen
                              ? 'باز'
                              : provider.closingTime != null
                                  ? 'بسته'
                                  : 'بسته',
                          style: AppTextStyles.small.copyWith(
                            color: provider.isOpen
                                ? AppColors.success
                                : AppColors.error,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _PlaceholderImage() {
    return Container(
      width: 180.w,
      height: 120.h,
      color: AppColors.surface,
      child: Icon(
        Icons.business,
        size: 48.w,
        color: AppColors.textTertiary,
      ),
    );
  }
}
