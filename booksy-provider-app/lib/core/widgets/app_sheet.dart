import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Bottom-sheet scaffolds (DESIGN_LANGUAGE.md §5.8). Top radius comes from
/// the theme (14). Two variants:
///
/// - [AppSheetScaffold] — modal task sheet: leading-aligned 18-bold title,
///   grey close disc, optional 2.6px accent divider.
/// - [AppSheetScaffold.picker] — drag handle (40×5, r8) with a centered
///   title over the accent divider.
///
/// Both respect the keyboard inset and cap their height at 90% of the
/// screen. Present with [showAppSheet].
class AppSheetScaffold extends StatelessWidget {
  final String? title;
  final Widget child;
  final bool showDivider;
  final VoidCallback? onClose;
  final bool _isPicker;

  const AppSheetScaffold({
    super.key,
    this.title,
    required this.child,
    this.showDivider = false,
    this.onClose,
  }) : _isPicker = false;

  const AppSheetScaffold.picker({
    super.key,
    this.title,
    required this.child,
    this.showDivider = true,
  })  : onClose = null,
        _isPicker = true;

  @override
  Widget build(BuildContext context) {
    final screenHeight = MediaQuery.sizeOf(context).height;

    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.viewInsetsOf(context).bottom,
      ),
      child: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: screenHeight * 0.9),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (_isPicker) ..._pickerHeader() else ..._modalHeader(context),
            Flexible(child: child),
          ],
        ),
      ),
    );
  }

  List<Widget> _modalHeader(BuildContext context) {
    return [
      Padding(
        padding: const EdgeInsets.fromLTRB(20, AppSpacing.md, AppSpacing.md, 0),
        child: Row(
          children: [
            if (title != null)
              Expanded(
                child: Text(
                  title!,
                  style: const TextStyle(
                    color: AppColors.ink,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              )
            else
              const Spacer(),
            _CloseDisc(onClose: onClose),
          ],
        ),
      ),
      if (showDivider) ...[
        const SizedBox(height: AppSpacing.card),
        const Divider(height: 2.6, thickness: 2.6, color: AppColors.border),
      ] else
        const SizedBox(height: AppSpacing.card),
    ];
  }

  List<Widget> _pickerHeader() {
    return [
      Container(
        width: 40,
        height: 5,
        margin: EdgeInsets.only(
          top: AppSpacing.md,
          bottom: title == null ? AppSpacing.md : AppSpacing.sm,
        ),
        decoration: BoxDecoration(
          color: AppColors.borderFocus,
          borderRadius: BorderRadius.circular(AppRadius.sm),
        ),
      ),
      if (title != null) ...[
        Text(
          title!,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: AppColors.ink,
            fontSize: 18,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        if (showDivider)
          const Divider(height: 2.6, thickness: 2.6, color: AppColors.border),
      ],
    ];
  }
}

class _CloseDisc extends StatelessWidget {
  final VoidCallback? onClose;

  const _CloseDisc({this.onClose});

  @override
  Widget build(BuildContext context) {
    // 21px disc per spec, wrapped to keep a 44dp tap target.
    return SizedBox(
      width: 44,
      height: 44,
      child: Center(
        child: Material(
          color: AppColors.icon,
          shape: const CircleBorder(),
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: onClose ?? () => Navigator.of(context).maybePop(),
            child: const SizedBox(
              width: 21,
              height: 21,
              child: Icon(Icons.close, size: 15, color: Colors.white),
            ),
          ),
        ),
      ),
    );
  }
}

/// Presents [sheet] as a modal bottom sheet with the spec barrier dim.
Future<T?> showAppSheet<T>(BuildContext context, Widget sheet) {
  return showModalBottomSheet<T>(
    context: context,
    isScrollControlled: true,
    barrierColor: AppColors.sheetBarrier,
    builder: (_) => sheet,
  );
}
