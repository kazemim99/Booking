import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_digits.dart';
import '../../../../core/widgets/widgets.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';
import '../bloc/auth_state.dart';

/// Phone-number login.
///
/// Used both as a routed screen (/login, optionally with ?redirect=) and
/// embedded inside the profile/appointments tabs for guests ([embedded]).
class LoginPage extends StatefulWidget {
  /// Where to land after successful authentication (return-to-intent).
  final String? redirect;

  /// True when rendered in place inside a tab (no back affordance; the tab
  /// itself swaps to the authenticated content after login).
  final bool embedded;

  const LoginPage({super.key, this.redirect, this.embedded = false});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _phoneController = TextEditingController();
  final _phoneFocus = FocusNode();
  String? _inlineError;

  @override
  void initState() {
    super.initState();
    // On-blur inline validation.
    _phoneFocus.addListener(() {
      if (!_phoneFocus.hasFocus && _phoneController.text.isNotEmpty) {
        setState(() => _inlineError = _validate(_phoneController.text));
      }
    });
  }

  @override
  void dispose() {
    _phoneController.dispose();
    _phoneFocus.dispose();
    super.dispose();
  }

  String? _validate(String value) {
    if (value.trim().isEmpty) return AppStrings.phoneRequired;
    if (!PersianDigits.isValidIranianMobile(value)) {
      return AppStrings.phoneInvalid;
    }
    return null;
  }

  String get _redirectTarget =>
      widget.redirect ?? (widget.embedded ? Routes.profile : Routes.home);

  void _sendOtp() {
    final error = _validate(_phoneController.text);
    setState(() => _inlineError = error);
    if (error != null) return;

    final phone = PersianDigits.canonicalMobile(_phoneController.text);
    context.read<AuthBloc>().add(SendVerificationCodeEvent(phoneNumber: phone));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: widget.embedded ? null : AppBar(),
      body: SafeArea(
        child: BlocListener<AuthBloc, AuthState>(
          listener: (context, state) {
            if (state is OtpSentSuccess) {
              final phone = Uri.encodeComponent(state.phoneNumber);
              final target = Uri.encodeComponent(_redirectTarget);
              context.push('${Routes.otp}?phone=$phone&redirect=$target');
            } else if (state is AuthError) {
              // Entered number stays in the controller — nothing is lost.
              AppSnackbar.error(context, state.message);
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
                    Icons.calendar_today_rounded,
                    size: 72,
                    color: theme.colorScheme.primary,
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  Text(
                    AppStrings.loginTitle,
                    style: theme.textTheme.displaySmall,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    AppStrings.loginSubtitle,
                    style: theme.textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.xl),
                  AppTextField(
                    controller: _phoneController,
                    focusNode: _phoneFocus,
                    label: AppStrings.phoneLabel,
                    hint: AppStrings.phoneHint,
                    errorText: _inlineError,
                    prefixIcon: Icons.phone_outlined,
                    keyboardType: TextInputType.phone,
                    contentDirection: TextDirection.ltr,
                    autofillHints: const [AutofillHints.telephoneNumber],
                    inputFormatters: [PersianDigitsInputFormatter()],
                    maxLength: 11,
                    onChanged: (_) {
                      if (_inlineError != null) {
                        setState(() => _inlineError = null);
                      }
                    },
                    onSubmitted: (_) => _sendOtp(),
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  BlocBuilder<AuthBloc, AuthState>(
                    builder: (context, state) {
                      return AppButton(
                        label: AppStrings.sendOtp,
                        loading: state is AuthLoading,
                        onPressed: _sendOtp,
                      );
                    },
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  Text(
                    AppStrings.termsNotice,
                    style: theme.textTheme.bodySmall,
                    textAlign: TextAlign.center,
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
