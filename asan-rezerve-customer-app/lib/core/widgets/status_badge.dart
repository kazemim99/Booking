import 'package:flutter/material.dart';
import '../../config/theme/app_colors.dart';
import '../../config/theme/app_text_styles.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';

enum BookingStatus { confirmed, pending, completed, cancelled, noShow }

/// Booking-status badge: tinted background + AA-contrast text + status icon,
/// so status is never conveyed by color alone.
class StatusBadge extends StatelessWidget {
  final BookingStatus status;

  const StatusBadge({super.key, required this.status});

  static BookingStatus? tryParse(String? raw) {
    switch (raw?.toLowerCase()) {
      case 'confirmed':
        return BookingStatus.confirmed;
      case 'pending':
      case 'requested':
        return BookingStatus.pending;
      case 'completed':
        return BookingStatus.completed;
      case 'cancelled':
      case 'canceled':
        return BookingStatus.cancelled;
      case 'noshow':
      case 'no_show':
        return BookingStatus.noShow;
      default:
        return null;
    }
  }

  String get label {
    switch (status) {
      case BookingStatus.confirmed:
        return AppStrings.statusConfirmed;
      case BookingStatus.pending:
        return AppStrings.statusPending;
      case BookingStatus.completed:
        return AppStrings.statusCompleted;
      case BookingStatus.cancelled:
        return AppStrings.statusCancelled;
      case BookingStatus.noShow:
        return AppStrings.statusNoShow;
    }
  }

  @override
  Widget build(BuildContext context) {
    final (Color bg, Color fg, IconData icon) = switch (status) {
      BookingStatus.confirmed => (
          AppColors.successTint,
          AppColors.successText,
          Icons.check_circle_outline,
        ),
      BookingStatus.pending => (
          AppColors.warningTint,
          AppColors.warningText,
          Icons.schedule,
        ),
      BookingStatus.completed => (
          AppColors.infoTint,
          AppColors.infoText,
          Icons.task_alt,
        ),
      BookingStatus.cancelled => (
          AppColors.errorTint,
          AppColors.errorText,
          Icons.cancel_outlined,
        ),
      BookingStatus.noShow => (
          AppColors.errorTint,
          AppColors.errorText,
          Icons.person_off_outlined,
        ),
    };

    return Semantics(
      label: label,
      child: Container(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.xs,
          vertical: AppSpacing.xxs,
        ),
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(AppRadius.full),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 14, color: fg),
            const SizedBox(width: AppSpacing.xxs),
            Text(
              label,
              style: AppTextStyles.small.copyWith(
                color: fg,
                fontWeight: AppTextStyles.medium,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
