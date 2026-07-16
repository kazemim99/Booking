import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_snackbar.dart';
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

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        automaticallyImplyLeading: false,
        title: const Text(
          AppStrings.moreTitle,
          style: TextStyle(
            fontSize: 17,
            fontWeight: FontWeight.w700,
            color: AppColors.ink,
          ),
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.md),
        children: [
          _sectionHeader(AppStrings.moreBusinessSection),
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
          // Edit flows not shipped yet — visibly disabled (design D1).
          _row(context,
              key: 'more-profile',
              icon: Icons.storefront_outlined,
              label: AppStrings.moreBusinessProfile,
              enabled: false),
          _row(context,
              key: 'more-hours',
              icon: Icons.schedule_outlined,
              label: AppStrings.moreWorkingHours,
              enabled: false),
          _row(context,
              key: 'more-gallery',
              icon: Icons.photo_library_outlined,
              label: AppStrings.moreGallery,
              enabled: false),
          const SizedBox(height: AppSpacing.lg),
          _sectionHeader(AppStrings.moreAccountSection),
          if (session != null)
            Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.sm,
                vertical: AppSpacing.xs,
              ),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 20,
                    backgroundColor: AppColors.primarySoft,
                    child: Text(
                      session.user.displayName.isNotEmpty
                          ? session.user.displayName.characters.first
                          : '؟',
                      style: const TextStyle(
                        fontSize: 16,
                        color: AppColors.primary,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          session.user.displayName,
                          style: const TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w600,
                            color: AppColors.ink,
                          ),
                        ),
                        Text(
                          [
                            session.user.phoneNumber,
                            if (session.providerStatus != null)
                              AppStrings.providerStatusLabel(
                                  session.providerStatus!.wireName),
                          ].join(' · '),
                          style: const TextStyle(
                              fontSize: 12, color: AppColors.muted),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          _row(
            context,
            key: 'more-logout',
            icon: Icons.logout,
            label: AppStrings.logout,
            color: AppColors.danger,
            onTap: () =>
                context.read<AuthBloc>().add(const LogoutRequested()),
          ),
          const SizedBox(height: AppSpacing.xl),
        ],
      ),
      bottomNavigationBar: const ProviderNavBar(active: NavTab.more),
    );
  }

  Widget _sectionHeader(String text) => Padding(
        padding: const EdgeInsets.only(
          bottom: AppSpacing.xs,
          top: AppSpacing.xs,
        ),
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
    final effectiveColor =
        enabled ? (color ?? AppColors.ink) : AppColors.disabled;
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
            Icon(icon,
                size: AppIconSize.md,
                color: enabled
                    ? (color ?? AppColors.primary)
                    : AppColors.disabled),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: Text(
                label,
                style: TextStyle(fontSize: 15, color: effectiveColor),
              ),
            ),
            if (enabled && color == null)
              const Icon(Icons.chevron_left,
                  size: AppIconSize.action, color: AppColors.muted)
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
        ClipboardData(text: ApiConstants.publicProviderUrl(providerId)));
    AppSnackbar.info(context, AppStrings.linkCopied);
  }
}
