import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Dialog header (DESIGN_LANGUAGE.md §5.9): 40px band with a centered
/// 18-bold ink title and a trailing grey close disc, 20px gap below,
/// optional 2px divider. Place as the first child of a dialog's Column.
class AppDialogHeader extends StatelessWidget {
  final String title;
  final VoidCallback? onClose;
  final bool showDivider;

  const AppDialogHeader({
    super.key,
    required this.title,
    this.onClose,
    this.showDivider = false,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          height: 40,
          margin: const EdgeInsets.only(bottom: 20),
          child: Stack(
            children: [
              Center(
                child: Text(
                  title,
                  style: const TextStyle(
                    color: AppColors.ink,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              Align(
                alignment: AlignmentDirectional.centerEnd,
                child: Material(
                  color: AppColors.icon,
                  shape: const CircleBorder(),
                  child: InkWell(
                    customBorder: const CircleBorder(),
                    onTap: onClose ?? () => Navigator.of(context).maybePop(),
                    child: const SizedBox(
                      width: 24,
                      height: 24,
                      child: Icon(Icons.close, size: 15, color: Colors.white),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
        if (showDivider)
          const Divider(height: 1, thickness: 2, color: AppColors.border),
      ],
    );
  }
}
