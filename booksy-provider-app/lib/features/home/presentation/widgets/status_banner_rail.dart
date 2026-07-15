import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../domain/entities/home_enums.dart';

/// Zone: banner rail — surfaces system/lifecycle conditions in the resolved
/// severity order. Renders the top two expanded; further banners collapse into
/// a count chip (spec: banner-rail precedence).
class StatusBannerRail extends StatelessWidget {
  final List<HomeBannerKind> banners;
  final VoidCallback? onSupportTap;

  const StatusBannerRail({
    super.key,
    required this.banners,
    this.onSupportTap,
  });

  @override
  Widget build(BuildContext context) {
    if (banners.isEmpty) return const SizedBox.shrink();
    final expanded = banners.take(2).toList();
    final overflow = banners.length - expanded.length;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (final kind in expanded) ...[
          _Banner(kind: kind, onSupportTap: onSupportTap),
          if (kind != expanded.last) const SizedBox(height: AppSpacing.sm),
        ],
        if (overflow > 0)
          Padding(
            padding: const EdgeInsets.only(top: AppSpacing.xs),
            child: Text(
              '+$overflow',
              style: const TextStyle(fontSize: 12, color: AppColors.muted),
            ),
          ),
      ],
    );
  }
}

class _Banner extends StatelessWidget {
  final HomeBannerKind kind;
  final VoidCallback? onSupportTap;

  const _Banner({required this.kind, this.onSupportTap});

  @override
  Widget build(BuildContext context) {
    final (fill, icon, title, body) = switch (kind) {
      HomeBannerKind.pending => (
          AppColors.primarySoft,
          Icons.info_outline,
          AppStrings.homePendingBannerTitle,
          AppStrings.homePendingBannerBody,
        ),
      HomeBannerKind.offline => (
          AppColors.surfaceSoft,
          Icons.wifi_off_outlined,
          AppStrings.homeOfflineBanner,
          AppStrings.homeStaleBanner,
        ),
      // Not yet reachable (availability is backend-managed and always OPEN
      // for now; nudges have no source) — copy kept minimal until they ship.
      _ => (
          AppColors.surfaceSoft,
          Icons.info_outline,
          AppStrings.homeStaleBanner,
          null,
        ),
    };

    return Container(
      key: Key('home-banner-${kind.name}'),
      padding: const EdgeInsets.all(AppSpacing.card),
      decoration: BoxDecoration(
        color: fill,
        borderRadius: BorderRadius.circular(AppRadius.md),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: AppIconSize.md, color: AppColors.primary),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    color: AppColors.ink,
                  ),
                ),
                if (body != null) ...[
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    body,
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppColors.muted,
                      height: 1.5,
                    ),
                  ),
                ],
                if (kind == HomeBannerKind.pending && onSupportTap != null)
                  Align(
                    alignment: AlignmentDirectional.centerEnd,
                    child: TextButton(
                      onPressed: onSupportTap,
                      child: const Text(AppStrings.contactSupport),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
