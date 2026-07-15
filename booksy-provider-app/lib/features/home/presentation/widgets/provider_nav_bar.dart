import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_snackbar.dart';

/// Live bottom-nav destinations.
enum NavTab { home, calendar }

/// The provider app's shared bottom navigation (spec: provider-calendar,
/// shared bottom navigation). Center notch hosts the ⊕ create action.
/// Clients/More stay visibly inactive until their tabs land.
class ProviderNavBar extends StatelessWidget {
  final NavTab active;

  const ProviderNavBar({super.key, required this.active});

  @override
  Widget build(BuildContext context) {
    Widget item(
      IconData icon,
      String label, {
      NavTab? tab,
    }) {
      final isActive = tab == active;
      return Expanded(
        child: InkWell(
          onTap: isActive
              ? null
              : () => switch (tab) {
                    NavTab.home => context.go(Routes.dashboard),
                    NavTab.calendar => context.go(Routes.calendar),
                    null =>
                      AppSnackbar.info(context, AppStrings.comingSoon),
                  },
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(icon,
                    size: AppIconSize.md,
                    color: isActive ? AppColors.primary : AppColors.muted),
                Text(
                  label,
                  style: TextStyle(
                    fontSize: 11,
                    color: isActive ? AppColors.primary : AppColors.muted,
                  ),
                ),
              ],
            ),
          ),
        ),
      );
    }

    return BottomAppBar(
      color: Colors.white,
      shape: const CircularNotchedRectangle(),
      notchMargin: 6,
      padding: EdgeInsets.zero,
      child: Row(
        children: [
          item(Icons.home_outlined, AppStrings.navHome, tab: NavTab.home),
          item(Icons.calendar_month_outlined, AppStrings.navCalendar,
              tab: NavTab.calendar),
          const Expanded(child: SizedBox()), // notch space for the ⊕
          item(Icons.people_outline, AppStrings.navClients),
          item(Icons.more_horiz, AppStrings.navMore),
        ],
      ),
    );
  }
}
