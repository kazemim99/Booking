import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Standard empty-state view (spec: feedback-states): hero icon, bold navy
/// caption, optional muted description, optional compact action button.
///
/// [iconWidget] overrides the default icon rendering so illustrations can
/// replace icons later without an API change.
class AppEmptyState extends StatelessWidget {
  final IconData icon;
  final Widget? iconWidget;
  final String message;
  final String? description;
  final String? actionLabel;
  final VoidCallback? onAction;

  /// Renders the action as the Coliride "add" affordance — a filled green
  /// circle-plus followed by a green label — instead of the default filled
  /// brand-blue pill. Use for actions that create the first item; green is
  /// the add/positive accent (DESIGN_LANGUAGE.md §1.2).
  final bool isAddAction;

  const AppEmptyState({
    super.key,
    this.icon = Icons.inbox_outlined,
    this.iconWidget,
    required this.message,
    this.description,
    this.actionLabel,
    this.onAction,
  }) : isAddAction = false;

  /// Empty state whose action creates the first item.
  const AppEmptyState.add({
    super.key,
    this.icon = Icons.inbox_outlined,
    this.iconWidget,
    required this.message,
    this.description,
    required this.actionLabel,
    required this.onAction,
  }) : isAddAction = true;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          iconWidget ??
              Icon(icon, size: AppIconSize.hero, color: AppColors.icon),
          const SizedBox(height: AppSpacing.md),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: AppColors.ink,
              fontSize: 16,
              fontWeight: FontWeight.bold,
            ),
          ),
          if (description != null) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(
              description!,
              textAlign: TextAlign.center,
              style: Theme.of(context)
                  .textTheme
                  .bodyMedium
                  ?.copyWith(color: AppColors.muted),
            ),
          ],
          if (actionLabel != null && onAction != null) ...[
            const SizedBox(height: AppSpacing.md),
            if (isAddAction)
              // Coliride add affordance: filled green circle-plus + green
              // label, no pill. Reads as "create the first one" rather than
              // a heavyweight primary CTA.
              TextButton.icon(
                key: const Key('empty-state-add'),
                onPressed: onAction,
                icon: const Icon(
                  Icons.add_circle,
                  size: AppIconSize.action,
                  color: AppColors.success,
                ),
                label: Text(actionLabel!),
                style: TextButton.styleFrom(
                  foregroundColor: AppColors.success,
                  minimumSize: const Size(0, 30),
                  padding: const EdgeInsets.symmetric(
                      horizontal: 14, vertical: 8),
                  textStyle: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              )
            else
            // Compact 30dp brand button (ButtonSize.small in the button spec).
            FilledButton(
              onPressed: onAction,
              style: FilledButton.styleFrom(
                minimumSize: const Size(0, 30),
                padding:
                    const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                textStyle: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                ),
              ),
              child: Text(actionLabel!),
            ),
          ],
        ],
      ),
    );
  }
}
