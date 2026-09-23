import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_event.dart';
import '../../../profile/presentation/bloc/profile_cubit.dart';

/// Asks for a first and a last name before a booking is confirmed (QA recording 2026-09-23 #9).
///
/// Signing up is only a phone number, so a customer who skipped the name page is still «مشتری 9384444636» —
/// and that is what the salon saw on the booking. Here both names are required and there is no skip: closing
/// the sheet books nothing. The name is saved to the customer's profile and then to the session, so it is not
/// asked for again.
class BookingNameSheet extends StatefulWidget {
  /// Cubit override for tests; the sheet makes (and closes) its own when null.
  final ProfileCubit? cubit;

  const BookingNameSheet({super.key, this.cubit});

  /// Opens the sheet over [context]; true once the name is saved and the session has it.
  static Future<bool> show(BuildContext context, {ProfileCubit? cubit}) async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      // A drag would close it mid-save, past the PopScope that holds it open.
      enableDrag: false,
      builder: (_) => BookingNameSheet(cubit: cubit),
    );
    return saved ?? false;
  }

  @override
  State<BookingNameSheet> createState() => _BookingNameSheetState();
}

class _BookingNameSheetState extends State<BookingNameSheet> {
  final _first = TextEditingController();
  final _last = TextEditingController();
  late final ProfileCubit _cubit;
  String? _firstError;
  String? _lastError;
  String? _saveError;
  bool _saving = false;

  /// The save on its way, if any: the sheet's own cubit is closed only once it is done.
  Future<void>? _inFlight;

  @override
  void initState() {
    super.initState();
    _cubit = widget.cubit ??
        ProfileCubit(remoteDataSource: getIt(), storageService: getIt());
  }

  @override
  void dispose() {
    if (widget.cubit == null) {
      final inFlight = _inFlight;
      if (inFlight == null) {
        _cubit.close();
      } else {
        inFlight.whenComplete(_cubit.close).ignore();
      }
    }
    _first.dispose();
    _last.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final first = _first.text.trim();
    final last = _last.text.trim();
    setState(() {
      _firstError = first.isEmpty ? AppStrings.firstNameRequired : null;
      _lastError = last.isEmpty ? AppStrings.lastNameRequired : null;
      _saveError = null;
    });
    if (first.isEmpty || last.isEmpty) return;

    final auth = context.read<AuthBloc>();
    setState(() => _saving = true);
    final saving = _cubit.saveProfile(firstName: first, lastName: last);
    _inFlight = saving;
    try {
      await saving;
    } finally {
      if (identical(_inFlight, saving)) _inFlight = null;
    }

    final saved = _cubit.state.editStatus == ProfileEditStatus.success;
    // Saved on the server: the session learns it even if the sheet has gone meanwhile.
    if (saved) auth.add(UserNameChangedEvent(firstName: first, lastName: last));
    if (!mounted) return;
    if (saved) {
      Navigator.of(context).pop(true);
      return;
    }
    setState(() {
      _saving = false;
      _saveError = AppStrings.completeNameSaveFailed;
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return PopScope(
      // Closing now would abandon a save half-way, its answer unheard.
      canPop: !_saving,
      child: SingleChildScrollView(
        key: const Key('booking-name-sheet'),
        padding: EdgeInsets.only(
          left: AppSpacing.md,
          right: AppSpacing.md,
          top: AppSpacing.md,
          bottom: AppSpacing.md + MediaQuery.of(context).viewInsets.bottom,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              AppStrings.bookingNameTitle,
              style: theme.textTheme.titleLarge,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(AppStrings.bookingNameBody, style: theme.textTheme.bodyMedium),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              key: const Key('booking-name-first'),
              controller: _first,
              label: AppStrings.firstNameLabel,
              errorText: _firstError,
              autofillHints: const [AutofillHints.givenName],
              enabled: !_saving,
              autofocus: true,
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('booking-name-last'),
              controller: _last,
              label: AppStrings.lastNameLabel,
              errorText: _lastError,
              autofillHints: const [AutofillHints.familyName],
              enabled: !_saving,
              onSubmitted: (_) => _save(),
            ),
            if (_saveError != null) ...[
              const SizedBox(height: AppSpacing.xs),
              Text(
                _saveError!,
                key: const Key('booking-name-error'),
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.error),
              ),
            ],
            const SizedBox(height: AppSpacing.lg),
            AppButton(
              key: const Key('booking-name-save'),
              label: AppStrings.bookingNameSaveAndBook,
              loading: _saving,
              onPressed: _save,
            ),
            const SizedBox(height: AppSpacing.xs),
            AppButton.secondary(
              key: const Key('booking-name-cancel'),
              label: AppStrings.back,
              onPressed: _saving ? null : () => Navigator.of(context).pop(false),
            ),
          ],
        ),
      ),
    );
  }
}
