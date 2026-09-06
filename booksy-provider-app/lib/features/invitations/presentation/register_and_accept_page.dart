import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../config/routes/app_router.dart';
import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_loading.dart';
import '../../../core/widgets/app_snackbar.dart';
import '../../../core/widgets/app_text_field.dart';
import '../../../core/widgets/otp_input.dart';
import 'register_and_accept_cubit.dart';

/// New-user path opened from an invitation SMS link
/// (`/invite/:invitationId/register`): name → OTP sent to the invitation's
/// own phone → verify → account + membership created. No session token is
/// returned, so success routes to the standard sign-in screen.
class RegisterAndAcceptPage extends StatelessWidget {
  final String invitationId;
  const RegisterAndAcceptPage({super.key, required this.invitationId});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<RegisterAndAcceptCubit>(
      create: (_) =>
          getIt<RegisterAndAcceptCubit>(param1: invitationId)..load(),
      child: const RegisterAndAcceptView(),
    );
  }
}

/// Separated from [RegisterAndAcceptPage] so tests can pump it with a fake cubit.
class RegisterAndAcceptView extends StatefulWidget {
  const RegisterAndAcceptView({super.key});

  @override
  State<RegisterAndAcceptView> createState() => _RegisterAndAcceptViewState();
}

class _RegisterAndAcceptViewState extends State<RegisterAndAcceptView> {
  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _otpCtrl = TextEditingController();

  @override
  void dispose() {
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    _emailCtrl.dispose();
    _otpCtrl.dispose();
    super.dispose();
  }

  void _submitInfo(RegisterAndAcceptCubit cubit) {
    if (_firstNameCtrl.text.trim().isEmpty || _lastNameCtrl.text.trim().isEmpty) {
      AppSnackbar.error(context, AppStrings.registerAcceptFieldsRequired);
      return;
    }
    cubit.sendOtp();
  }

  void _submitOtp(RegisterAndAcceptCubit cubit, String code) {
    cubit.register(
      firstName: _firstNameCtrl.text.trim(),
      lastName: _lastNameCtrl.text.trim(),
      email: _emailCtrl.text.trim().isEmpty ? null : _emailCtrl.text.trim(),
      otpCode: code,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.registerAcceptTitle)),
      body: BlocConsumer<RegisterAndAcceptCubit, RegisterAndAcceptState>(
        listenWhen: (p, c) => p.error != c.error && c.error != null,
        listener: (context, state) => AppSnackbar.error(context, state.error!),
        builder: (context, state) {
          final cubit = context.read<RegisterAndAcceptCubit>();
          switch (state.phase) {
            case RegisterAcceptPhase.loading:
              return const AppLoading.page();
            case RegisterAcceptPhase.notFound:
              return _Message(
                icon: Icons.link_off,
                text: AppStrings.acceptInvitationNotFound,
              );
            case RegisterAcceptPhase.loadError:
              return _Message(
                icon: Icons.error_outline,
                text: state.error ?? AppStrings.acceptInvitationNotFound,
                actionLabel: AppStrings.retry,
                onAction: cubit.load,
              );
            case RegisterAcceptPhase.registered:
              return _SuccessBody();
            case RegisterAcceptPhase.verifyingOtp:
            case RegisterAcceptPhase.registering:
              return _OtpBody(
                state: state,
                otpCtrl: _otpCtrl,
                onCompleted: (code) => _submitOtp(cubit, code),
                onChangeInfo: cubit.backToInfo,
              );
            case RegisterAcceptPhase.enteringInfo:
            case RegisterAcceptPhase.sendingOtp:
              return _InfoBody(
                state: state,
                firstNameCtrl: _firstNameCtrl,
                lastNameCtrl: _lastNameCtrl,
                emailCtrl: _emailCtrl,
                onSubmit: () => _submitInfo(cubit),
              );
          }
        },
      ),
    );
  }
}

class _InfoBody extends StatelessWidget {
  final RegisterAndAcceptState state;
  final TextEditingController firstNameCtrl;
  final TextEditingController lastNameCtrl;
  final TextEditingController emailCtrl;
  final VoidCallback onSubmit;

  const _InfoBody({
    required this.state,
    required this.firstNameCtrl,
    required this.lastNameCtrl,
    required this.emailCtrl,
    required this.onSubmit,
  });

  @override
  Widget build(BuildContext context) {
    final org = state.summary?.organizationName ?? '';
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (org.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.lg),
                child: Text(
                  AppStrings.acceptInvitationInvitedTo(org),
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
            AppTextField(
              key: const Key('register-accept-first-name'),
              controller: firstNameCtrl,
              label: AppStrings.registerAcceptFirstName,
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('register-accept-last-name'),
              controller: lastNameCtrl,
              label: AppStrings.registerAcceptLastName,
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('register-accept-email'),
              controller: emailCtrl,
              label: AppStrings.registerAcceptEmailOptional,
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: AppSpacing.lg),
            AppButton(
              key: const Key('register-accept-send-code'),
              label: AppStrings.registerAcceptSendCode,
              loading: state.phase == RegisterAcceptPhase.sendingOtp,
              onPressed: onSubmit,
            ),
          ],
        ),
      ),
    );
  }
}

class _OtpBody extends StatelessWidget {
  final RegisterAndAcceptState state;
  final TextEditingController otpCtrl;
  final ValueChanged<String> onCompleted;
  final VoidCallback onChangeInfo;

  const _OtpBody({
    required this.state,
    required this.otpCtrl,
    required this.onCompleted,
    required this.onChangeInfo,
  });

  @override
  Widget build(BuildContext context) {
    final registering = state.phase == RegisterAcceptPhase.registering;
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const SizedBox(height: AppSpacing.lg),
            Text(
              AppStrings.registerAcceptCodeSentTo(state.maskedPhone ?? ''),
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: AppSpacing.lg),
            OtpInput(
              key: const Key('register-accept-otp'),
              controller: otpCtrl,
              errorText: state.otpError,
              onCompleted: registering ? (_) {} : onCompleted,
            ),
            if (registering) ...[
              const SizedBox(height: AppSpacing.lg),
              Text(
                AppStrings.registerAcceptVerifying,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
            const Spacer(),
            TextButton(
              key: const Key('register-accept-change-info'),
              onPressed: registering ? null : onChangeInfo,
              child: const Text(AppStrings.registerAcceptChangeInfo),
            ),
          ],
        ),
      ),
    );
  }
}

class _SuccessBody extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.check_circle, color: AppColors.success, size: 48),
            const SizedBox(height: AppSpacing.sm),
            const Text(
              AppStrings.registerAcceptSuccess,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.xs),
            const Text(
              AppStrings.registerAcceptSuccessHint,
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.muted),
            ),
            const SizedBox(height: AppSpacing.md),
            AppButton(
              key: const Key('register-accept-go-login'),
              label: AppStrings.registerAcceptGoToLogin,
              onPressed: () => context.go(Routes.login),
            ),
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
