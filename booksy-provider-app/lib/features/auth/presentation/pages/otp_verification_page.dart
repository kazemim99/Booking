import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/phone_number.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/otp_input.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';
import '../bloc/auth_state.dart';

/// OTP verification (mirrors VerificationView.vue). Auto-submits on completion;
/// the router's redirect performs navigation once the Bloc resolves auth state.
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
  bool _submitting = false;

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
    if (_submitting) return; // guard duplicate submit (manual + auto-complete)
    if (code.length != 6) {
      setState(() => _inlineError = AppStrings.otpLengthInvalid);
      return;
    }
    setState(() {
      _inlineError = null;
      _submitting = true;
    });
    context.read<AuthBloc>().add(
          VerifyCodeRequested(phoneNumber: widget.phoneNumber, code: code),
        );
  }

  void _resend() {
    context.read<AuthBloc>().add(ResendCodeRequested(widget.phoneNumber));
  }

  void _editNumber() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(Routes.login);
    }
  }

  String get _maskedPhone => PhoneNumber.tryParse(widget.phoneNumber)?.displayFa ??
      widget.phoneNumber;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.otpPageTitle)),
      body: SafeArea(
        child: BlocListener<AuthBloc, AuthState>(
          listener: (context, state) {
            if (state is Authenticated ||
                state is NeedsOnboarding ||
                state is AccountBlocked) {
              // Router redirect handles navigation.
              AppSnackbar.success(context, AppStrings.loginSuccess);
            } else if (state is OtpResent) {
              AppSnackbar.info(context, AppStrings.otpResent);
              _startCountdown();
            } else if (state is AuthError) {
              setState(() {
                _inlineError = state.message;
                _submitting = false;
              });
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
                  Icon(Icons.sms_outlined,
                      size: 72, color: theme.colorScheme.primary),
                  const SizedBox(height: AppSpacing.lg),
                  Text(AppStrings.otpTitle,
                      style: theme.textTheme.headlineSmall,
                      textAlign: TextAlign.center),
                  const SizedBox(height: AppSpacing.xs),
                  Text(AppStrings.otpSubtitle(_maskedPhone),
                      style: theme.textTheme.bodyMedium,
                      textAlign: TextAlign.center),
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
                    builder: (context, state) => AppButton(
                      label: AppStrings.verifyAndContinue,
                      loading: state is AuthLoading,
                      onPressed: () => _verify(_otpController.text),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  if (_secondsLeft > 0)
                    Text(AppStrings.resendCountdown('$_secondsLeft'),
                        style: theme.textTheme.bodySmall,
                        textAlign: TextAlign.center)
                  else
                    AppButton.text(
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
