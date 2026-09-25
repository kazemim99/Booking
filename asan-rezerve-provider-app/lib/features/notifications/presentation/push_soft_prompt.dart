import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import 'push_permission_cubit.dart';

/// The one-time card on Home. A salon gets no SMS for a new booking request — push is its only way to hear about one
/// while the app is closed — so Home offers it once, until it is answered or dismissed; the More row stays for later.
class PushSoftPrompt extends StatefulWidget {
  /// Tests pass their own; the app uses the shared one, and a composition without one shows no card.
  final PushPermissionCubit? cubit;

  const PushSoftPrompt({super.key, this.cubit});

  @override
  State<PushSoftPrompt> createState() => _PushSoftPromptState();
}

class _PushSoftPromptState extends State<PushSoftPrompt> {
  PushPermissionCubit? _cubit;

  @override
  void initState() {
    super.initState();
    _cubit = widget.cubit ?? (getIt.isRegistered<PushPermissionCubit>() ? getIt<PushPermissionCubit>() : null);
    _cubit?.load();
  }

  @override
  void didUpdateWidget(covariant PushSoftPrompt oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.cubit != null && widget.cubit != oldWidget.cubit) {
      _cubit = widget.cubit;
      _cubit!.load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final cubit = _cubit;
    if (cubit == null) return const SizedBox.shrink();

    return BlocBuilder<PushPermissionCubit, PushPermissionState>(
      bloc: cubit,
      builder: (context, state) {
        if (!state.showsSoftPrompt) return const SizedBox.shrink();

        return Padding(
          padding: const EdgeInsets.only(bottom: AppSpacing.md),
          child: AppCard(
            key: const Key('push-prompt'),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Row(
                  children: [
                    Icon(Icons.notifications_active_outlined, color: AppColors.primary, size: AppIconSize.md),
                    SizedBox(width: AppSpacing.sm),
                    Expanded(
                      child: Text(
                        AppStrings.pushPromptTitle,
                        style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700, color: AppColors.ink),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.xs),
                const Text(AppStrings.pushPromptBody, style: TextStyle(fontSize: 13, color: AppColors.subtitle)),
                const SizedBox(height: AppSpacing.sm),
                // Stacked, never side by side: this app's themed buttons are full-width (Size.fromHeight) and throw
                // inside a Row.
                AppButton(
                  key: const Key('push-prompt-enable'),
                  label: AppStrings.pushEnableAction,
                  size: AppButtonSize.medium,
                  loading: state.busy,
                  onPressed: cubit.enable,
                ),
                AppButton.text(
                  key: const Key('push-prompt-dismiss'),
                  label: AppStrings.pushPromptLater,
                  size: AppButtonSize.medium,
                  onPressed: cubit.dismissPrompt,
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
