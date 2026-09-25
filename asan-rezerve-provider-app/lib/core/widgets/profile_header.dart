import 'dart:math' as math;

import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Identity header on the blue chrome (DESIGN_LANGUAGE.md §5.12): centered
/// avatar with optional green completeness ring and % pill, camera/edit
/// badge, name line and subtitle in white, optional top-trailing action
/// (e.g. logout). Render inside the chrome area above the content sheet.
class ProfileHeader extends StatelessWidget {
  final Widget? avatar;
  final String name;
  final String? subtitle;

  /// 0..1 — when non-null and < 1, draws the ring arc and the % pill.
  final double? completion;
  final VoidCallback? onEditAvatar;
  final Widget? action;
  final double avatarSize;

  const ProfileHeader({
    super.key,
    this.avatar,
    required this.name,
    this.subtitle,
    this.completion,
    this.onEditAvatar,
    this.action,
    this.avatarSize = 124,
  });

  @override
  Widget build(BuildContext context) {
    final clamped = completion?.clamp(0.0, 1.0);
    final showRing = clamped != null;
    final showPill = clamped != null && clamped < 1;

    return ColoredBox(
      color: AppColors.appBar,
      child: SafeArea(
        bottom: false,
        child: Stack(
          children: [
            if (action != null)
              PositionedDirectional(top: 8, end: 16, child: action!),
            Padding(
              padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Center(
                    child: SizedBox(
                      width: avatarSize + 12,
                      height: avatarSize + 12,
                      child: Stack(
                        alignment: Alignment.center,
                        clipBehavior: Clip.none,
                        children: [
                          if (showRing)
                            CustomPaint(
                              size: Size.square(avatarSize + 12),
                              painter: _CompletionRingPainter(clamped),
                            ),
                          Container(
                            width: avatarSize,
                            height: avatarSize,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              color: Colors.white,
                              border: Border.all(
                                color: AppColors.avatarBorder,
                                width: 2,
                              ),
                            ),
                            clipBehavior: Clip.antiAlias,
                            child: avatar ??
                                const Icon(
                                  Icons.storefront_outlined,
                                  size: 48,
                                  color: AppColors.icon,
                                ),
                          ),
                          if (showPill)
                            Positioned(
                              top: -6,
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 8,
                                  vertical: 2,
                                ),
                                decoration: BoxDecoration(
                                  color: AppColors.success,
                                  borderRadius: BorderRadius.circular(999),
                                ),
                                child: Text(
                                  '٪${(clamped * 100).round()}',
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                            ),
                          if (onEditAvatar != null)
                            PositionedDirectional(
                              bottom: 2,
                              end: 2,
                              child: Material(
                                color: AppColors.success,
                                shape: const CircleBorder(),
                                child: InkWell(
                                  onTap: onEditAvatar,
                                  customBorder: const CircleBorder(),
                                  child: const SizedBox(
                                    width: 25,
                                    height: 25,
                                    child: Icon(
                                      Icons.photo_camera_outlined,
                                      size: 14,
                                      color: Colors.white,
                                    ),
                                  ),
                                ),
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    name,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  if (subtitle != null) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      subtitle!,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: Colors.white70,
                        fontSize: 14,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CompletionRingPainter extends CustomPainter {
  final double completion;

  _CompletionRingPainter(this.completion);

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3
      ..strokeCap = StrokeCap.round
      ..color = AppColors.success;
    final rect = Offset.zero & size;
    // Sweep clockwise from the top.
    canvas.drawArc(
      rect.deflate(1.5),
      -math.pi / 2,
      2 * math.pi * completion,
      false,
      paint,
    );
  }

  @override
  bool shouldRepaint(_CompletionRingPainter oldDelegate) =>
      oldDelegate.completion != completion;
}
