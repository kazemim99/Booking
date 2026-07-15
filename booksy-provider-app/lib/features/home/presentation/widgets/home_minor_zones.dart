import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_card.dart';

/// Zone: end-of-day summary — positive closure once every appointment today is
/// completed (replaces NowNext).
class EndOfDaySummary extends StatelessWidget {
  final int completedCount;

  const EndOfDaySummary({super.key, required this.completedCount});

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('home-end-of-day'),
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: AppColors.successSoft,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        children: [
          const Icon(Icons.check_circle,
              size: AppIconSize.md, color: AppColors.success),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Text(
              AppStrings.homeEndOfDay(completedCount),
              style: const TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.w600,
                color: AppColors.ink,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Zone: coming-up peek — a one-row glance at tomorrow.
class ComingUpPeek extends StatelessWidget {
  final int tomorrowApptCount;
  final VoidCallback? onTap;

  const ComingUpPeek({super.key, required this.tomorrowApptCount, this.onTap});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: EdgeInsets.zero,
      child: InkWell(
        key: const Key('home-coming-up'),
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppRadius.card),
        child: Container(
          constraints: const BoxConstraints(minHeight: 48),
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.card,
            vertical: AppSpacing.sm,
          ),
          child: Row(
            children: [
              const Icon(Icons.calendar_month_outlined,
                  size: AppIconSize.md, color: AppColors.primary),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  tomorrowApptCount > 0
                      ? AppStrings.homeTomorrowCount(tomorrowApptCount)
                      : AppStrings.homeTomorrowEmpty,
                  style: const TextStyle(fontSize: 14, color: AppColors.ink),
                ),
              ),
              const Icon(Icons.chevron_left,
                  size: AppIconSize.action, color: AppColors.muted),
            ],
          ),
        ),
      ),
    );
  }
}

/// Full-body loading skeleton shown on first load (system LOADING): flat,
/// shadowless placeholder blocks in the composition's shape.
class HomeSkeleton extends StatelessWidget {
  const HomeSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    Widget block(double height, {double? width}) => Container(
          height: height,
          width: width,
          margin: const EdgeInsets.only(bottom: AppSpacing.md),
          decoration: BoxDecoration(
            color: AppColors.surfaceSoft,
            borderRadius: BorderRadius.circular(AppRadius.card),
            border: Border.all(color: AppColors.border),
          ),
        );

    return ListView(
      key: const Key('home-skeleton'),
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        block(64),
        block(180),
        block(56),
        block(120),
      ],
    );
  }
}
