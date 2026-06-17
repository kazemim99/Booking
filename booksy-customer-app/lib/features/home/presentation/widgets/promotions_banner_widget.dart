import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../domain/entities/promotion.dart';

class PromotionsBannerWidget extends StatefulWidget {
  final List<Promotion> promotions;
  final Function(Promotion) onPromotionTap;

  const PromotionsBannerWidget({
    super.key,
    required this.promotions,
    required this.onPromotionTap,
  });

  @override
  State<PromotionsBannerWidget> createState() => _PromotionsBannerWidgetState();
}

class _PromotionsBannerWidgetState extends State<PromotionsBannerWidget> {
  final PageController _pageController = PageController();
  int _currentPage = 0;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (widget.promotions.isEmpty) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: EdgeInsets.symmetric(horizontal: 16.w),
          child: Text(
            'پیشنهاد ویژه',
            style: AppTextStyles.h3,
          ),
        ),
        SizedBox(height: 12.h),
        SizedBox(
          height: 160.h,
          child: PageView.builder(
            controller: _pageController,
            onPageChanged: (index) {
              setState(() {
                _currentPage = index;
              });
            },
            itemCount: widget.promotions.length,
            itemBuilder: (context, index) {
              final promotion = widget.promotions[index];
              return _PromotionCard(
                promotion: promotion,
                onTap: () => widget.onPromotionTap(promotion),
              );
            },
          ),
        ),
        if (widget.promotions.length > 1) ...[
          SizedBox(height: 12.h),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: List.generate(
              widget.promotions.length,
              (index) => Container(
                width: 8.w,
                height: 8.h,
                margin: EdgeInsets.symmetric(horizontal: 4.w),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: _currentPage == index
                      ? AppColors.primary
                      : AppColors.border,
                ),
              ),
            ),
          ),
        ],
      ],
    );
  }
}

class _PromotionCard extends StatelessWidget {
  final Promotion promotion;
  final VoidCallback onTap;

  const _PromotionCard({
    required this.promotion,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: EdgeInsets.symmetric(horizontal: 16.w),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(16.r),
          border: Border.all(color: AppColors.border),
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(16.r),
          child: Stack(
            children: [
              // Background Image
              CachedNetworkImage(
                imageUrl: promotion.imageUrl,
                width: double.infinity,
                height: 160.h,
                fit: BoxFit.cover,
                placeholder: (context, url) => Container(
                  color: AppColors.surface,
                  child: const Center(
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
                    ),
                  ),
                ),
                errorWidget: (context, url, error) => Container(
                  color: AppColors.surface,
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        Icons.card_giftcard,
                        size: 48.w,
                        color: AppColors.primary,
                      ),
                      SizedBox(height: 8.h),
                      Text(
                        promotion.title,
                        style: AppTextStyles.h3,
                        textAlign: TextAlign.center,
                      ),
                    ],
                  ),
                ),
              ),
              // Gradient Overlay for better text readability
              Positioned(
                bottom: 0,
                left: 0,
                right: 0,
                child: Container(
                  padding: EdgeInsets.all(16.w),
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [
                        Colors.transparent,
                        Colors.black.withValues(alpha: 0.7),
                      ],
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        promotion.title,
                        style: AppTextStyles.h3.copyWith(
                          color: Colors.white,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      if (promotion.description != null) ...[
                        SizedBox(height: 4.h),
                        Text(
                          promotion.description!,
                          style: AppTextStyles.caption.copyWith(
                            color: Colors.white.withValues(alpha: 0.9),
                          ),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
