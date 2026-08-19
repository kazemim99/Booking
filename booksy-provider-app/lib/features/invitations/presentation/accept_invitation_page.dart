import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../config/routes/app_router.dart';
import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_snackbar.dart';
import 'accept_invitation_cubit.dart';

/// Opened from an invitation SMS link (`/invite/:invitationId`). Handles the
/// existing-registered-person accept path; a brand-new invitee is sent to
/// registration (register-and-accept) separately.
class AcceptInvitationPage extends StatelessWidget {
  final String invitationId;
  const AcceptInvitationPage({super.key, required this.invitationId});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<AcceptInvitationCubit>(
      create: (_) => getIt<AcceptInvitationCubit>(param1: invitationId)..load(),
      child: const AcceptInvitationView(),
    );
  }
}

/// Separated from [AcceptInvitationPage] so tests can pump it with a fake cubit.
class AcceptInvitationView extends StatelessWidget {
  const AcceptInvitationView({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.acceptInvitationTitle)),
      body: BlocConsumer<AcceptInvitationCubit, AcceptInvitationState>(
        listenWhen: (p, c) => p.error != c.error && c.error != null,
        listener: (context, state) => AppSnackbar.error(context, state.error!),
        builder: (context, state) {
          final cubit = context.read<AcceptInvitationCubit>();
          switch (state.phase) {
            case AcceptPhase.loading:
              return const Center(child: CircularProgressIndicator());
            case AcceptPhase.notFound:
              return const _Message(
                icon: Icons.link_off,
                text: AppStrings.acceptInvitationNotFound,
              );
            case AcceptPhase.error:
              return _Message(
                icon: Icons.error_outline,
                text: state.error ?? AppStrings.acceptInvitationNotFound,
                actionLabel: AppStrings.retry,
                onAction: cubit.load,
              );
            default:
              return _Body(state: state);
          }
        },
      ),
    );
  }
}

class _Body extends StatelessWidget {
  final AcceptInvitationState state;
  const _Body({required this.state});

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<AcceptInvitationCubit>();
    final s = state.summary!;
    final theme = Theme.of(context);
    final accepted = state.phase == AcceptPhase.accepted;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const SizedBox(height: AppSpacing.lg),
            CircleAvatar(
              radius: 36,
              backgroundColor: AppColors.primarySoft,
              child: Text(
                s.organizationName.isNotEmpty
                    ? s.organizationName.characters.first
                    : '؟',
                style: const TextStyle(
                  color: AppColors.primary,
                  fontSize: 28,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            Text(
              AppStrings.acceptInvitationInvitedTo(s.organizationName),
              textAlign: TextAlign.center,
              style: theme.textTheme.titleMedium,
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(
              s.maskedPhone,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.muted),
            ),
            const Spacer(),
            if (accepted) ...[
              const Icon(Icons.check_circle,
                  color: AppColors.success, size: 48),
              const SizedBox(height: AppSpacing.sm),
              const Text(
                AppStrings.acceptInvitationAccepted,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.md),
              AppButton(
                key: const Key('accept-go-dashboard'),
                label: AppStrings.acceptInvitationGoDashboard,
                onPressed: () => context.go(Routes.dashboard),
              ),
            ] else if (!s.isValid) ...[
              const Text(
                AppStrings.acceptInvitationExpired,
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.danger),
              ),
            ] else if (state.needsLogin) ...[
              const Text(
                AppStrings.acceptInvitationLoginPrompt,
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.muted),
              ),
              const SizedBox(height: AppSpacing.md),
              AppButton(
                key: const Key('accept-login'),
                label: AppStrings.acceptInvitationLogin,
                onPressed: () => context.go(Routes.login),
              ),
            ] else ...[
              AppButton(
                key: const Key('accept-invitation'),
                label: AppStrings.acceptInvitationAccept,
                loading: state.phase == AcceptPhase.accepting,
                onPressed: cubit.accept,
              ),
            ],
            const SizedBox(height: AppSpacing.lg),
          ],
        ),
      ),
    );
  }
}

class _Message extends StatelessWidget {
  final IconData icon;
  final String text;
  final String? actionLabel;
  final VoidCallback? onAction;
  const _Message({
    required this.icon,
    required this.text,
    this.actionLabel,
    this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 48, color: AppColors.muted),
            const SizedBox(height: AppSpacing.md),
            Text(text, textAlign: TextAlign.center),
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: AppSpacing.md),
              TextButton(onPressed: onAction, child: Text(actionLabel!)),
            ],
          ],
        ),
      ),
    );
  }
}
