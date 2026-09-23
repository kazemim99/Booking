import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/push/push_registration.dart';
import '../../../core/widgets/app_snackbar.dart';
import 'push_permission_cubit.dart';

/// The notifications-on-this-device row on the More page.
///
/// The one place a salon can always turn notifications on, and in a browser the only kind of place the permission
/// prompt may come from: a tap. Renders nothing on a build or browser without push — [frame] included, so the More
/// page shows no empty section — and the page then looks exactly as it did before.
class PushEnableTile extends StatefulWidget {
  /// Tests pass their own; the app uses the shared one, and a composition without one shows no row.
  final PushPermissionCubit? cubit;

  /// Wraps the row when there is one (the More page's section header and card).
  final Widget Function(BuildContext context, Widget row)? frame;

  const PushEnableTile({super.key, this.cubit, this.frame});

  @override
  State<PushEnableTile> createState() => _PushEnableTileState();
}

class _PushEnableTileState extends State<PushEnableTile> {
  PushPermissionCubit? _cubit;

  @override
  void initState() {
    super.initState();
    _cubit = widget.cubit ?? (getIt.isRegistered<PushPermissionCubit>() ? getIt<PushPermissionCubit>() : null);
    _cubit?.load();
  }

  @override
  void didUpdateWidget(covariant PushEnableTile oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.cubit != null && widget.cubit != oldWidget.cubit) {
      _cubit = widget.cubit;
      _cubit!.load();
    }
  }

  Future<void> _enable(BuildContext context, PushPermissionCubit cubit) async {
    final status = await cubit.enable();
    if (!context.mounted) return;
    switch (status) {
      case PushStatus.enabled:
        AppSnackbar.success(context, AppStrings.pushEnabledSnack);
      case PushStatus.blocked:
        AppSnackbar.info(context, AppStrings.pushBlockedHint);
      case PushStatus.notAsked:
      case PushStatus.unavailable:
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    final cubit = _cubit;
    if (cubit == null) return const SizedBox.shrink();

    return BlocBuilder<PushPermissionCubit, PushPermissionState>(
      bloc: cubit,
      builder: (context, state) {
        if (state.status == PushStatus.unavailable) return const SizedBox.shrink();

        final (icon, title, subtitle) = switch (state.status) {
          PushStatus.enabled => (Icons.notifications_active_outlined, AppStrings.pushEnabledTitle, null),
          PushStatus.blocked => (
              Icons.notifications_off_outlined,
              AppStrings.pushBlockedTitle,
              AppStrings.pushBlockedHint,
            ),
          _ => (Icons.notifications_outlined, AppStrings.pushEnableAction, AppStrings.pushEnableSubtitle),
        };
        final actionable = state.status == PushStatus.notAsked && !state.busy;

        final row = InkWell(
          key: const Key('push-enable-tile'),
          onTap: actionable ? () => _enable(context, cubit) : null,
          borderRadius: BorderRadius.circular(AppRadius.sm),
          child: Container(
            constraints: const BoxConstraints(minHeight: 48),
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm, vertical: AppSpacing.sm),
            child: Row(
              children: [
                Icon(
                  icon,
                  size: AppIconSize.md,
                  color: state.status == PushStatus.enabled ? AppColors.success : AppColors.primary,
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(title, style: const TextStyle(fontSize: 15, color: AppColors.ink)),
                      if (subtitle != null)
                        Text(subtitle, style: const TextStyle(fontSize: 13, color: AppColors.subtitle)),
                    ],
                  ),
                ),
                if (state.busy)
                  const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)),
              ],
            ),
          ),
        );

        return widget.frame?.call(context, row) ?? row;
      },
    );
  }
}
