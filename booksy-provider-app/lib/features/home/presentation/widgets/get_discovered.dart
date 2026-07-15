import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_card.dart';

/// Zone: get discovered — the Growth-phase hero. Drives the first booking via
/// the share-link CTA, with the profile-completeness meter beneath it.
class GetDiscovered extends StatelessWidget {
  final int completenessPct;
  final VoidCallback onShare;
  final VoidCallback onAddWalkIn;

  const GetDiscovered({
    super.key,
    required this.completenessPct,
    required this.onShare,
    required this.onAddWalkIn,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  AppStrings.homeDiscoverTitle,
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                  ),
                ),
              ),
              const Icon(Icons.verified_outlined,
                  size: AppIconSize.md, color: AppColors.success),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          const Text(
            AppStrings.homeDiscoverBody,
            style: TextStyle(
                fontSize: 13, color: AppColors.muted, height: 1.5),
          ),
          const SizedBox(height: AppSpacing.md),
          AppButton(
            key: const Key('home-share-link'),
            label: AppStrings.homeShareLink,
            onPressed: onShare,
          ),
          if (completenessPct < 100) ...[
            const SizedBox(height: AppSpacing.md),
            Text(
              AppStrings.homeProfileCompleteness('$completenessPct'),
              style: const TextStyle(fontSize: 13, color: AppColors.ink),
            ),
            const SizedBox(height: AppSpacing.xs),
            ClipRRect(
              borderRadius: BorderRadius.circular(AppRadius.sm),
              child: LinearProgressIndicator(
                value: completenessPct / 100,
                minHeight: 6,
                backgroundColor: AppColors.border,
                valueColor: const AlwaysStoppedAnimation(AppColors.success),
              ),
            ),
          ],
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: AppButton.text(
              key: const Key('home-add-walkin'),
              label: '+ ${AppStrings.homeAddWalkIn}',
              onPressed: onAddWalkIn,
            ),
          ),
        ],
      ),
    );
  }
}
