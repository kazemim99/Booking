import 'package:flutter/material.dart';
import '../constants/app_strings.dart';
import 'empty_state.dart';
import 'error_state.dart';

/// The four standardized async-screen states. Every data-backed screen's
/// bloc state must map onto one of these — blank screens and indefinite
/// spinners are prohibited.
enum ViewStatus { loading, content, empty, error }

/// Maps a [ViewStatus] to the standardized rendering:
/// skeleton / content / empty / error-with-retry.
class StateSwitcher extends StatelessWidget {
  final ViewStatus status;

  /// Content-shaped skeleton shown while loading.
  final Widget skeleton;

  /// Built only when [status] is [ViewStatus.content].
  final WidgetBuilder contentBuilder;

  /// Shown when [status] is [ViewStatus.empty]. Defaults to a generic
  /// empty state, but screens should pass a purposeful one.
  final Widget? empty;

  final String? errorMessage;
  final VoidCallback onRetry;

  const StateSwitcher({
    super.key,
    required this.status,
    required this.skeleton,
    required this.contentBuilder,
    required this.onRetry,
    this.empty,
    this.errorMessage,
  });

  @override
  Widget build(BuildContext context) {
    switch (status) {
      case ViewStatus.loading:
        return skeleton;
      case ViewStatus.content:
        return contentBuilder(context);
      case ViewStatus.empty:
        return empty ??
            const EmptyState(
              icon: Icons.inbox_outlined,
              title: AppStrings.noResultsTitle,
            );
      case ViewStatus.error:
        return ErrorState(message: errorMessage, onRetry: onRetry);
    }
  }
}
