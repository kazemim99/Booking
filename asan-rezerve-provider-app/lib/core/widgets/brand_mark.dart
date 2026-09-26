import 'package:flutter/material.dart';

import '../../config/theme/app_tokens.dart';

/// The AsanRezerve symbol: an «A» of two strokes whose crossbar is a check mark.
///
/// Painted from the designer's geometry (branding/symbol-color.svg, a 100×100 grid) rather than shipped as an
/// image, so it stays sharp at any size without an asset or an SVG dependency. brand_mark_test.dart fails if the
/// SVG and these numbers drift apart.
class BrandMark extends StatelessWidget {
  /// Apex, left foot and right foot of the «A».
  static const List<Offset> letter = [Offset(50, 13), Offset(14, 83), Offset(86, 83)];

  /// The check mark that stands in for the crossbar.
  static const List<Offset> check = [Offset(32, 48), Offset(54, 70), Offset(82, 25)];

  static const double strokeWidth = 8.5;

  final double size;
  final Color letterColor;
  final Color checkColor;

  /// Read by screen readers; null when the name is already spelled out next to the mark.
  final String? semanticLabel;

  const BrandMark({
    super.key,
    this.size = 96,
    this.letterColor = AppColors.appBar,
    this.checkColor = AppColors.success,
    this.semanticLabel,
  });

  @override
  Widget build(BuildContext context) {
    final mark = CustomPaint(
      size: Size.square(size),
      painter: _BrandMarkPainter(letterColor: letterColor, checkColor: checkColor),
    );
    final label = semanticLabel;
    return label == null ? ExcludeSemantics(child: mark) : Semantics(label: label, image: true, child: mark);
  }
}

class _BrandMarkPainter extends CustomPainter {
  final Color letterColor;
  final Color checkColor;

  const _BrandMarkPainter({required this.letterColor, required this.checkColor});

  @override
  void paint(Canvas canvas, Size size) {
    canvas.scale(size.width / 100, size.height / 100);
    final pen = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = BrandMark.strokeWidth
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round
      ..isAntiAlias = true;

    const a = BrandMark.letter;
    pen.color = letterColor;
    canvas
      ..drawLine(a[0], a[1], pen)
      ..drawLine(a[0], a[2], pen);

    const c = BrandMark.check;
    pen.color = checkColor;
    canvas.drawPath(
      Path()
        ..moveTo(c[0].dx, c[0].dy)
        ..lineTo(c[1].dx, c[1].dy)
        ..lineTo(c[2].dx, c[2].dy),
      pen,
    );
  }

  @override
  bool shouldRepaint(_BrandMarkPainter old) => old.letterColor != letterColor || old.checkColor != checkColor;
}
