import 'package:flutter/material.dart';
import '../../config/theme/app_colors.dart';
import '../../config/theme/app_text_styles.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import '../network/connectivity_service.dart';

/// Non-blocking banner shown while the device is offline. Wrap screen
/// content (typically the shell body) with this widget; content stays
/// visible and interactive underneath.
class OfflineBanner extends StatelessWidget {
  final ConnectivityService connectivity;
  final Widget child;

  const OfflineBanner({
    super.key,
    required this.connectivity,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return StreamBuilder<bool>(
      stream: connectivity.onStatusChange,
      initialData: true,
      builder: (context, snapshot) {
        final online = snapshot.data ?? true;
        return Column(
          children: [
            if (!online)
              Semantics(
                liveRegion: true,
                child: Container(
                  width: double.infinity,
                  color: AppColors.warningTint,
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.md,
                    vertical: AppSpacing.xs,
                  ),
                  child: SafeArea(
                    bottom: false,
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(
                          Icons.wifi_off,
                          size: 16,
                          color: AppColors.warningText,
                        ),
                        const SizedBox(width: AppSpacing.xs),
                        Text(
                          AppStrings.offlineBanner,
                          style: AppTextStyles.small.copyWith(
                            color: AppColors.warningText,
                            fontWeight: AppTextStyles.medium,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            Expanded(child: child),
          ],
        );
      },
    );
  }
}
