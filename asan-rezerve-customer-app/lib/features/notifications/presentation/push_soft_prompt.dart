import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/widgets/widgets.dart';
import 'push_permission_cubit.dart';

/// The one-time card after a booking is made: the moment the customer has something to be told about — the salon's
/// answer. Offered only while nobody has been asked and until it is dismissed; the profile row stays for later.
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
        final theme = Theme.of(context);

        return Padding(
          padding: const EdgeInsets.only(bottom: AppSpacing.md),
          child: AppCard(
            key: const Key('push-prompt'),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Icon(Icons.notifications_active_outlined, color: theme.colorScheme.primary),
                    const SizedBox(width: AppSpacing.xs),
                    Expanded(child: Text(AppStrings.pushPromptTitle, style: theme.textTheme.titleSmall)),
                  ],
                ),
                const SizedBox(height: AppSpacing.xxs),
                Text(AppStrings.pushPromptBody, style: theme.textTheme.bodyMedium),
                const SizedBox(height: AppSpacing.sm),
                AppButton(
                  key: const Key('push-prompt-enable'),
                  label: AppStrings.pushEnableAction,
                  loading: state.busy,
                  onPressed: cubit.enable,
                ),
                AppButton.text(
                  key: const Key('push-prompt-dismiss'),
                  label: AppStrings.pushPromptLater,
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
