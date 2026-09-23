import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../config/theme/app_colors.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/push/push_registration.dart';
import '../../../core/widgets/widgets.dart';
import 'push_permission_cubit.dart';

/// The notifications-on-this-device row in the profile.
///
/// The one place a customer can always turn notifications on, and in a browser the only kind of place the
/// permission prompt may come from: a tap. Renders nothing on a build or browser without push, so the profile looks
/// exactly as it did before. Brings its own divider, so a hidden row leaves no stray line.
class PushEnableTile extends StatefulWidget {
  /// Tests pass their own; the app uses the shared one, and a composition without one shows no row.
  final PushPermissionCubit? cubit;

  const PushEnableTile({super.key, this.cubit});

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
      case PushStatus.unreachable:
        AppSnackbar.info(context, AppStrings.pushUnreachableHint);
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
          PushStatus.blocked => (Icons.notifications_off_outlined, AppStrings.pushBlockedTitle, AppStrings.pushBlockedHint),
          PushStatus.unreachable => (Icons.cloud_off_outlined, AppStrings.pushUnreachableTitle, AppStrings.pushUnreachableHint),
          _ => (Icons.notifications_outlined, AppStrings.pushEnableAction, AppStrings.pushEnableSubtitle),
        };
        // Never asked, or allowed but not connected (a tap tries again without asking again).
        final actionable =
            (state.status == PushStatus.notAsked || state.status == PushStatus.unreachable) && !state.busy;

        return Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Divider(),
            ListTile(
              key: const Key('push-enable-tile'),
              leading: Icon(icon, color: state.status == PushStatus.enabled ? AppColors.successText : null),
              title: Text(title),
              subtitle: subtitle == null ? null : Text(subtitle),
              trailing: state.busy
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                  : null,
              onTap: actionable ? () => _enable(context, cubit) : null,
            ),
          ],
        );
      },
    );
  }
}
