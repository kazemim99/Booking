import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/profile_header.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_event.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../widgets/provider_nav_bar.dart';

/// The More (بیشتر) hub — configuration & reflection, one level down
/// (spec: provider-more-hub). Session-only: no network load (design D1).
class MorePage extends StatelessWidget {
  const MorePage({super.key});

  @override
  Widget build(BuildContext context) {
    final authState = context.watch<AuthBloc>().state;
    final session = authState is Authenticated ? authState.session : null;

    // Profile hub (DESIGN_LANGUAGE.md §5): blue chrome carrying the identity,
    // then a white sheet whose rows live in ONE card per group with hairline
    // dividers — not a flat run of rows on the page background.
    return Scaffold(
      // White, not blue: [ProfileHeader] paints its own chrome (including the
      // status-bar inset), and a blue scaffold would tint the gutter around
      // the floating nav pill, destroying its "floating" read.
      backgroundColor: Colors.white,
      body: Column(
        children: [
          ProfileHeader(
            name: session?.user.displayName ?? AppStrings.moreTitle,
            subtitle: session == null
                ? null
                : [
                    session.user.phoneNumber,
                    if (session.providerStatus != null)
                      AppStrings.providerStatusLabel(
                        session.providerStatus!.wireName,
                      ),
                  ].join(' · '),
            avatarSize: 88,
            // No logout disc here: the Account card below carries a labelled
            // logout row. The Figma uses a bare icon because its hub has no
            // such row — duplicating the action in both places would be worse
            // for discoverability than either alone.
          ),
          // Blue backs only the sheet area so its rounded top reads against
          // the chrome. It stops at the body's edge, leaving the gutter around
          // the floating nav pill white.
          Expanded(
            child: ColoredBox(
              color: AppColors.appBar,
              child: Container(
                width: double.infinity,
                decoration: const BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.vertical(
                    top: Radius.circular(AppRadius.panel + 4),
                  ),
                ),
                clipBehavior: Clip.antiAlias,
                child: ListView(
                  // Extra bottom room so the last row clears the floating nav
                  // pill, which overlays the sheet rather than sitting below it.
                  padding: const EdgeInsets.fromLTRB(
                    AppSpacing.md,
                    AppSpacing.md,
                    AppSpacing.md,
                    AppSpacing.xl,
                  ),
                  children: [
                    _sectionHeader(AppStrings.moreBusinessSection),
                    _card([
                      _row(
                        context,
                        key: 'more-services',
                        icon: Icons.design_services_outlined,
                        label: AppStrings.moreServices,
                        onTap: () => context.push(Routes.moreServices),
                      ),
                      _row(
                        context,
                        key: 'more-staff',
                        icon: Icons.people_outline,
                        label: AppStrings.moreStaff,
                        onTap: () => context.push(Routes.moreStaff),
                      ),
                      _row(
                        context,
                        key: 'more-memberships',
                        icon: Icons.storefront_outlined,
                        label: AppStrings.moreMemberships,
                        onTap: () => context.push(Routes.moreMemberships),
                      ),
                      _row(
                        context,
                        key: 'more-insights',
                        icon: Icons.insights_outlined,
                        label: AppStrings.moreInsights,
                        onTap: () => context.push(Routes.moreInsights),
                      ),
                      _row(
                        context,
                        key: 'more-share',
                        icon: Icons.link_outlined,
                        label: AppStrings.moreShareLink,
                        onTap: () => _shareLink(context, session?.providerId),
                      ),
                      _row(
                        context,
                        key: 'more-profile',
                        icon: Icons.storefront_outlined,
                        label: AppStrings.moreBusinessProfile,
                        onTap: () => context.push(Routes.moreBusiness),
                      ),
                      _row(
                        context,
                        key: 'more-hours',
                        icon: Icons.schedule_outlined,
                        label: AppStrings.moreWorkingHours,
                        onTap: () => context.push(Routes.moreHours),
                      ),
                      _row(
                        context,
                        key: 'more-holidays',
                        icon: Icons.beach_access_outlined,
                        label: AppStrings.moreHolidays,
                        onTap: () => context.push(Routes.moreHolidays),
                      ),
                      _row(
                        context,
                        key: 'more-gallery',
                        icon: Icons.photo_library_outlined,
                        label: AppStrings.moreGallery,
                        onTap: () => context.push(Routes.moreGallery),
                      ),
                    ]),
                    const SizedBox(height: AppSpacing.lg),
                    _sectionHeader(AppStrings.moreAccountSection),
                    _card([
                      _row(
                        context,
                        key: 'more-logout',
                        icon: Icons.logout,
                        label: AppStrings.logout,
                        color: AppColors.danger,
                        onTap: () => context.read<AuthBloc>().add(
                          const LogoutRequested(),
                        ),
                      ),
                    ]),
                    const SizedBox(height: AppSpacing.xl),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
      bottomNavigationBar: const ProviderNavBar(active: NavTab.more),
    );
  }

  /// One card holding a group of rows, separated by hairline dividers
  /// (§4.2 "one card, many rows" — never a stack of one-row cards).
  Widget _card(List<Widget> rows) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.menuBorder),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          for (var i = 0; i < rows.length; i++) ...[
            if (i > 0)
              const Divider(
                height: 1,
                thickness: 1,
                indent: AppSpacing.card,
                endIndent: AppSpacing.card,
                color: AppColors.menuBorder,
              ),
            rows[i],
          ],
        ],
      ),
    );
  }

  Widget _sectionHeader(String text) => Padding(
    padding: const EdgeInsets.only(bottom: AppSpacing.xs, top: AppSpacing.xs),
    child: Text(
      text,
      style: const TextStyle(
        fontSize: 13,
        fontWeight: FontWeight.w700,
        color: AppColors.muted,
      ),
    ),
  );

  Widget _row(
    BuildContext context, {
    required String key,
    required IconData icon,
    required String label,
    VoidCallback? onTap,
    bool enabled = true,
    Color? color,
  }) {
    final effectiveColor = enabled
        ? (color ?? AppColors.ink)
        : AppColors.disabled;
    return InkWell(
      key: Key(key),
      onTap: enabled ? onTap : null,
      borderRadius: BorderRadius.circular(AppRadius.sm),
      child: Container(
        constraints: const BoxConstraints(minHeight: 48),
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.sm,
          vertical: AppSpacing.xs,
        ),
        child: Row(
          children: [
            Icon(
              icon,
              size: AppIconSize.md,
              color: enabled
                  ? (color ?? AppColors.primary)
                  : AppColors.disabled,
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: Text(
                label,
                style: TextStyle(fontSize: 15, color: effectiveColor),
              ),
            ),
            // Chevron sits in a muted disc (§5.2): the affordance column is
            // decoration, so it never competes with the row label.
            if (enabled && color == null)
              Container(
                width: 24,
                height: 24,
                decoration: const BoxDecoration(
                  color: AppColors.icon,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.chevron_left,
                  size: AppIconSize.sm,
                  color: Colors.white,
                ),
              )
            else if (!enabled)
              const Text(
                AppStrings.comingSoon,
                style: TextStyle(fontSize: 11, color: AppColors.disabled),
              ),
          ],
        ),
      ),
    );
  }

  void _shareLink(BuildContext context, String? providerId) {
    if (providerId == null) {
      AppSnackbar.error(context, AppStrings.genericError);
      return;
    }
    Clipboard.setData(
      ClipboardData(text: ApiConstants.publicProviderUrl(providerId)),
    );
    AppSnackbar.info(context, AppStrings.linkCopied);
  }
}
