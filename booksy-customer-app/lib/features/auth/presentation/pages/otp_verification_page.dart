import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/widgets.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';
import '../bloc/auth_state.dart';

/// OTP verification: segmented input with SMS autofill/paste, auto-submit
/// on completion, resend countdown, and inline error recovery.
///
/// Successful verification emits Authenticated; the router's redirect then
/// honors the ?redirect= return-to-intent — no manual navigation here.
class OtpVerificationPage extends StatefulWidget {
  final String phoneNumber;
  final String? redirect;

  const OtpVerificationPage({
    super.key,
    required this.phoneNumber,
    this.redirect,
  });

  @override
  State<OtpVerificationPage> createState() => _OtpVerificationPageState();
}

class _OtpVerificationPageState extends State<OtpVerificationPage> {
  static const int _resendSeconds = 60;

  final _otpController = TextEditingController();
  final _otpFocus = FocusNode();
  String? _inlineError;
  Timer? _timer;
  int _secondsLeft = _resendSeconds;

  @override
  void initState() {
    super.initState();
    _startCountdown();
  }

  @override
  void dispose() {
    _timer?.cancel();
    _otpController.dispose();
    _otpFocus.dispose();
    super.dispose();
  }

  void _startCountdown() {
    _timer?.cancel();
    setState(() => _secondsLeft = _resendSeconds);
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_secondsLeft <= 1) {
        timer.cancel();
        setState(() => _secondsLeft = 0);
      } else {
        setState(() => _secondsLeft--);
      }
    });
  }

  void _verify(String code) {
    setState(() => _inlineError = null);
    context.read<AuthBloc>().add(
          VerifyCodeEvent(phoneNumber: widget.phoneNumber, code: code),
        );
  }

  void _resend() {
    context.read<AuthBloc>().add(ResendOtpEvent(widget.phoneNumber));
  }

  void _editNumber() {
    if (context.canPop()) {
      // Login page below keeps its state — number stays pre-filled.
      context.pop();
    } else {
      context.go(Routes.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.otpPageTitle)),
      body: SafeArea(
        child: BlocListener<AuthBloc, AuthState>(
          listener: (context, state) {
            if (state is Authenticated) {
              AppSnackbar.success(context, AppStrings.loginSuccess);
              // Router redirect performs the actual navigation.
            } else if (state is OtpResentSuccess) {
              AppSnackbar.info(context, AppStrings.otpResent);
              _startCountdown();
            } else if (state is AuthError) {
              setState(() => _inlineError = state.message);
              _otpController.clear();
              _otpFocus.requestFocus();
            }
          },
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Icon(
                    Icons.sms_outlined,
                    size: 72,
                    color: theme.colorScheme.primary,
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  Text(
                    AppStrings.otpTitle,
                    style: theme.textTheme.displaySmall,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    AppStrings.otpSubtitle(widget.phoneNumber),
                    style: theme.textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                  Center(
                    child: TextButton.icon(
                      onPressed: _editNumber,
                      icon: const Icon(Icons.edit_outlined, size: 16),
                      label: const Text(AppStrings.otpEditNumber),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  OtpInput(
                    controller: _otpController,
                    focusNode: _otpFocus,
                    errorText: _inlineError,
                    onCompleted: _verify,
                    onChanged: (_) {
                      if (_inlineError != null) {
                        setState(() => _inlineError = null);
                      }
                    },
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  BlocBuilder<AuthBloc, AuthState>(
                    builder: (context, state) {
                      return AppButton(
                        label: AppStrings.verifyAndContinue,
                        loading: state is AuthLoading,
                        onPressed: _otpController.text.length == 6
                            ? () => _verify(_otpController.text)
                            : null,
                      );
                    },
                  ),
                  const SizedBox(height: AppSpacing.md),
                  _secondsLeft > 0
                      ? Text(
                          AppStrings.resendCountdown('$_secondsLeft'),
                          style: theme.textTheme.bodySmall,
                          textAlign: TextAlign.center,
                        )
                      : AppButton.text(
                          label: AppStrings.resendOtp,
                          onPressed: _resend,
                        ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
