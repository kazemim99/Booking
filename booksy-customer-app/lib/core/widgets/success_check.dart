import 'package:flutter/material.dart';

/// A check mark that appears for a moment and dismisses itself.
///
/// Signing in used to announce itself with a plain «ورود موفقیت‌آمیز بود» toast over the OTP boxes, which read
/// like any other message and lingered while the screen changed underneath it (QA walkthrough 2026-09-22). A
/// short, quiet tick says the same thing and gets out of the way.
class SuccessCheck extends StatefulWidget {
  /// How long the mark stays before it removes itself.
  static const Duration visibleFor = Duration(milliseconds: 700);

  const SuccessCheck({super.key});

  /// Shows the mark over the current screen. Navigation may continue immediately: the mark sits on its own
  /// route above whatever comes next and closes that route itself.
  static void show(BuildContext context) {
    showDialog<void>(
      context: context,
      barrierDismissible: false,
      barrierColor: Colors.black26,
      builder: (_) => const Center(child: SuccessCheck()),
    );
  }

  @override
  State<SuccessCheck> createState() => _SuccessCheckState();
}

class _SuccessCheckState extends State<SuccessCheck> {
  @override
  void initState() {
    super.initState();
    Future<void>.delayed(SuccessCheck.visibleFor, () {
      if (!mounted) return;
      Navigator.of(context).maybePop();
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return TweenAnimationBuilder<double>(
      key: const Key('success-check'),
      tween: Tween(begin: 0, end: 1),
      duration: const Duration(milliseconds: 320),
      curve: Curves.easeOutBack,
      builder: (context, value, child) {
        final t = value.clamp(0.0, 1.0);
        return Opacity(
          opacity: t,
          child: Transform.scale(scale: 0.6 + (0.4 * t), child: child),
        );
      },
      child: Container(
        width: 96,
        height: 96,
        decoration: BoxDecoration(
          color: theme.colorScheme.surface,
          shape: BoxShape.circle,
          boxShadow: [
            BoxShadow(color: Colors.black.withValues(alpha: 0.12), blurRadius: 24),
          ],
        ),
        child: Icon(Icons.check_rounded, size: 56, color: theme.colorScheme.primary),
      ),
    );
  }
}
