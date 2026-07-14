import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_digits.dart';
import '../../../../core/utils/phone_number.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';
import '../bloc/auth_state.dart';

/// Provider login — phone entry (mirrors ProviderLoginView.vue).
class ProviderLoginPage extends StatefulWidget {
  /// Return-to-intent target after successful auth.
  final String? redirect;

  const ProviderLoginPage({super.key, this.redirect});

  @override
  State<ProviderLoginPage> createState() => _ProviderLoginPageState();
}

class _ProviderLoginPageState extends State<ProviderLoginPage> {
  final _phoneController = TextEditingController();
  final _phoneFocus = FocusNode();
  String? _inlineError;

  @override
  void dispose() {
    _phoneController.dispose();
    _phoneFocus.dispose();
    super.dispose();
  }

  String? _validate(String value) {
    if (value.trim().isEmpty) return AppStrings.phoneRequired;
    if (!PhoneNumber.isValid(value)) return AppStrings.phoneInvalid;
    return null;
  }

  void _sendOtp() {
    final error = _validate(_phoneController.text);
    setState(() => _inlineError = error);
    if (error != null) return;
    final phone = PhoneNumber.parse(_phoneController.text).value;
    context.read<AuthBloc>().add(SendVerificationCodeRequested(phone));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(),
      body: SafeArea(
        child: BlocListener<AuthBloc, AuthState>(
          listenWhen: (_, s) => s is OtpSent || s is AuthError,
          listener: (context, state) {
            if (state is OtpSent) {
              final phone = Uri.encodeComponent(state.phoneNumber);
              final params = <String>['phone=$phone'];
              if (widget.redirect != null && widget.redirect!.isNotEmpty) {
                params.add('redirect=${Uri.encodeComponent(widget.redirect!)}');
              }
              context.push('${Routes.otp}?${params.join('&')}');
            } else if (state is AuthError) {
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
                  Icon(Icons.storefront_rounded,
                      size: 72, color: theme.colorScheme.primary),
                  const SizedBox(height: AppSpacing.lg),
                  Text(AppStrings.loginTitle,
                      style: theme.textTheme.headlineSmall,
                      textAlign: TextAlign.center),
                  const SizedBox(height: AppSpacing.xs),
                  Text(AppStrings.loginSubtitle,
                      style: theme.textTheme.bodyMedium,
                      textAlign: TextAlign.center),
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
                    inputFormatters: const [DigitsOnlyInputFormatter()],
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
                    builder: (context, state) => AppButton(
                      label: AppStrings.sendOtp,
                      loading: state is AuthLoading,
                      onPressed: _sendOtp,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  Text(AppStrings.termsNotice,
                      style: theme.textTheme.bodySmall,
                      textAlign: TextAlign.center),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
