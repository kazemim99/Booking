import 'package:flutter/material.dart';
import 'package:shimmer/shimmer.dart';
import '../../config/theme/app_colors.dart';
import '../../config/theme/app_tokens.dart';

/// Shimmering placeholder blocks. Compose these into content-shaped
/// skeletons; never show a bare spinner for first loads.
///
/// Honors MediaQuery.disableAnimations by rendering static blocks.
class SkeletonLoader extends StatelessWidget {
  final Widget child;

  const SkeletonLoader({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    if (MediaQuery.of(context).disableAnimations) {
      return child;
    }
    return Shimmer.fromColors(
      baseColor: AppColors.border,
      highlightColor: AppColors.borderSubtle,
      child: child,
    );
  }

  /// A single skeleton block.
  static Widget box({
    double? width,
    double height = 16,
    double radius = AppRadius.sm,
  }) {
    return _SkeletonBox(width: width, height: height, radius: radius);
  }

  /// A skeleton stand-in for a list of cards.
  static Widget list({int items = 3, double itemHeight = 96}) {
    return SkeletonLoader(
      child: Column(
        children: List.generate(
          items,
          (_) => Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: _SkeletonBox(height: itemHeight, radius: AppRadius.lg),
          ),
        ),
      ),
    );
  }
}

class _SkeletonBox extends StatelessWidget {
  final double? width;
  final double height;
  final double radius;

  const _SkeletonBox({this.width, required this.height, required this.radius});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: width ?? double.infinity,
      height: height,
      decoration: BoxDecoration(
        color: AppColors.border,
        borderRadius: BorderRadius.circular(radius),
      ),
    );
  }
}
