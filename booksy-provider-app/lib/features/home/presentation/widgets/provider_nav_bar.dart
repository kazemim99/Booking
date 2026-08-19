import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_bottom_bar.dart';

/// Live bottom-nav destinations.
enum NavTab { home, calendar, clients, more }

/// The provider app's shared bottom navigation, rendered as the floating
/// blue pill (DESIGN_LANGUAGE.md §5.11) via [AppBottomBar]. Pages that own
/// a create flow pass [onCreate]; the ⊕ then sits mid-pill as a white disc
/// (replacing the old center-docked FAB).
class ProviderNavBar extends StatelessWidget {
  final NavTab active;
  final VoidCallback? onCreate;
  final Key? createKey;

  const ProviderNavBar({
    super.key,
    required this.active,
    this.onCreate,
    this.createKey,
  });

  static const _tabs = [NavTab.home, NavTab.calendar, NavTab.clients, NavTab.more];

  @override
  Widget build(BuildContext context) {
    return AppBottomBar(
      items: const [
        AppBottomBarItem(
          icon: Icons.home_outlined,
          semanticLabel: AppStrings.navHome,
        ),
        AppBottomBarItem(
          icon: Icons.calendar_month_outlined,
          semanticLabel: AppStrings.navCalendar,
        ),
        AppBottomBarItem(
          icon: Icons.people_outline,
          semanticLabel: AppStrings.navClients,
        ),
        AppBottomBarItem(
          icon: Icons.more_horiz,
          semanticLabel: AppStrings.navMore,
        ),
      ],
      activeIndex: _tabs.indexOf(active),
      onTap: (index) => switch (_tabs[index]) {
        NavTab.home => context.go(Routes.dashboard),
        NavTab.calendar => context.go(Routes.calendar),
        NavTab.clients => context.go(Routes.clients),
        NavTab.more => context.go(Routes.more),
      },
      center: onCreate == null
          ? null
          : Tooltip(
              message: AppStrings.homeCreateTitle,
              child: Material(
                key: createKey,
                color: Colors.white,
                shape: const CircleBorder(),
                child: InkWell(
                  onTap: onCreate,
                  customBorder: const CircleBorder(),
                  child: const SizedBox(
                    width: 44,
                    height: 44,
                    child: Icon(
                      Icons.add,
                      size: AppIconSize.md,
                      color: AppColors.primary,
                    ),
                  ),
                ),
              ),
            ),
    );
  }
}
