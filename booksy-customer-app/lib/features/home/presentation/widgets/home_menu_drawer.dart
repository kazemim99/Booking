import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';

/// The home hamburger menu.
///
/// It only ever navigates to destinations that already exist in the router —
/// the four tab roots plus the two discovery entry points — so no route names
/// change and every tap lands somewhere real.
class HomeMenuDrawer extends StatelessWidget {
  const HomeMenuDrawer({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    void goTo(String location) {
      Navigator.of(context).pop();
      context.go(location);
    }

    return Drawer(
      child: SafeArea(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            Container(
              width: double.infinity,
              color: AppColors.appBar,
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    AppStrings.homeTitle,
                    style: AppTextStyles.h3.copyWith(color: Colors.white),
                  ),
                  const SizedBox(height: AppSpacing.xxs),
                  Text(
                    AppStrings.appTagline,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: Colors.white70,
                    ),
                  ),
                ],
              ),
            ),
            ListTile(
              key: const Key('home-menu-home'),
              leading: const Icon(Icons.home_outlined),
              title: const Text(AppStrings.tabHome),
              onTap: () => goTo(Routes.home),
            ),
            ListTile(
              key: const Key('home-menu-explore'),
              leading: const Icon(Icons.search_outlined),
              title: const Text(AppStrings.tabExplore),
              onTap: () => goTo(Routes.explore),
            ),
            // One destination, not two. «جستجو در محله» used to be its own screen; the map page's search
            // field does area/city lookup now, so a second entry would just be the same page under a
            // different name.
            ListTile(
              key: const Key('home-menu-nearby'),
              leading: const Icon(Icons.near_me_outlined),
              title: const Text(AppStrings.nearMe),
              onTap: () => goTo(Routes.exploreMap),
            ),
            const Divider(),
            ListTile(
              key: const Key('home-menu-appointments'),
              leading: const Icon(Icons.calendar_today_outlined),
              title: const Text(AppStrings.tabAppointments),
              onTap: () => goTo(Routes.appointments),
            ),
            ListTile(
              key: const Key('home-menu-profile'),
              leading: const Icon(Icons.person_outline),
              title: const Text(AppStrings.tabProfile),
              onTap: () => goTo(Routes.profile),
            ),
          ],
        ),
      ),
    );
  }
}
