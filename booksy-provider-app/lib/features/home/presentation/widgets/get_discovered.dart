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

  /// While true the business is not yet approved, so it is not discoverable
  /// and the share CTA would hand out a dead link.
  final bool pendingVerification;

  const GetDiscovered({
    super.key,
    required this.completenessPct,
    required this.onShare,
    required this.onAddWalkIn,
    this.pendingVerification = false,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  pendingVerification
                      ? AppStrings.homeDiscoverPendingTitle
                      : AppStrings.homeDiscoverTitle,
                  key: const Key('home-discover-title'),
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                  ),
                ),
              ),
              Icon(
                pendingVerification
                    ? Icons.hourglass_empty
                    : Icons.verified_outlined,
                size: AppIconSize.md,
                color: pendingVerification
                    ? AppColors.muted
                    : AppColors.success,
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            pendingVerification
                ? AppStrings.homeDiscoverPendingBody
                : AppStrings.homeDiscoverBody,
            style: const TextStyle(
                fontSize: 13, color: AppColors.muted, height: 1.5),
          ),
          const SizedBox(height: AppSpacing.md),
          AppButton(
            key: const Key('home-share-link'),
            label: AppStrings.homeShareLink,
            // Sharing a link to an unapproved business sends customers to a
            // page they cannot book from.
            onPressed: pendingVerification ? null : onShare,
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
