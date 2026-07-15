import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_card.dart';

/// One activation task shown in the checklist.
class ActivationItem {
  final String key;
  final String label;
  final bool done;
  const ActivationItem(this.key, this.label, {this.done = false});
}

/// Zone: activation checklist — the Setup-phase hero guiding first-value
/// setup. Items completed during onboarding show as done.
///
/// Until per-item completeness signals ship (staff/gallery sources), the
/// default item set marks services done (onboarding required them) and the
/// rest to-do.
class ActivationChecklist extends StatelessWidget {
  final List<ActivationItem> items;
  final void Function(String key) onItemTap;

  const ActivationChecklist({
    super.key,
    required this.onItemTap,
    this.items = defaultItems,
  });

  static const List<ActivationItem> defaultItems = [
    ActivationItem('services', AppStrings.homeChecklistServices, done: true),
    ActivationItem('staff', AppStrings.homeChecklistStaff),
    ActivationItem('gallery', AppStrings.homeChecklistGallery),
    ActivationItem('share', AppStrings.homeChecklistShare),
  ];

  @override
  Widget build(BuildContext context) {
    final done = items.where((i) => i.done).length;

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  AppStrings.homeChecklistTitle,
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                  ),
                ),
              ),
              Text(
                AppStrings.homeChecklistProgress(done, items.length),
                style: const TextStyle(fontSize: 13, color: AppColors.muted),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          ClipRRect(
            borderRadius: BorderRadius.circular(AppRadius.sm),
            child: LinearProgressIndicator(
              value: items.isEmpty ? 0 : done / items.length,
              minHeight: 6,
              backgroundColor: AppColors.border,
              valueColor: const AlwaysStoppedAnimation(AppColors.success),
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
          for (final item in items)
            InkWell(
              key: Key('checklist-${item.key}'),
              onTap: item.done ? null : () => onItemTap(item.key),
              borderRadius: BorderRadius.circular(AppRadius.sm),
              child: Container(
                constraints: const BoxConstraints(minHeight: 44),
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.sm,
                  vertical: AppSpacing.xs,
                ),
                margin: const EdgeInsets.only(bottom: AppSpacing.xs),
                decoration: BoxDecoration(
                  color: item.done
                      ? AppColors.successSoft
                      : AppColors.surfaceSoft,
                  borderRadius: BorderRadius.circular(AppRadius.sm),
                ),
                child: Row(
                  children: [
                    Icon(
                      item.done
                          ? Icons.check_circle
                          : Icons.radio_button_unchecked,
                      size: AppIconSize.action,
                      color: item.done ? AppColors.success : AppColors.icon,
                    ),
                    const SizedBox(width: AppSpacing.sm),
                    Expanded(
                      child: Text(
                        item.label,
                        style: const TextStyle(
                          fontSize: 15,
                          color: AppColors.ink,
                        ),
                      ),
                    ),
                    if (item.done)
                      const Text(
                        AppStrings.homeChecklistDone,
                        style:
                            TextStyle(fontSize: 12, color: AppColors.success),
                      )
                    else
                      const Icon(
                        Icons.chevron_left,
                        size: AppIconSize.action,
                        color: AppColors.muted,
                      ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}
