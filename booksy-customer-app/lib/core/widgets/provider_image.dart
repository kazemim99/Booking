import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../../config/theme/app_tokens.dart';
import 'skeleton_loader.dart';

/// A provider cover/thumbnail image loaded from the network.
///
/// Many providers legitimately have no image, and a URL that 404s must not
/// leave a blank hole in a card, so the three cases are handled in one place:
/// no URL, still loading (shimmer block), and failed (storefront glyph on the
/// soft surface). Callers clip it — the widget itself always fills its box.
class ProviderImage extends StatelessWidget {
  final String? imageUrl;
  final double? width;
  final double? height;

  /// Glyph size for the no-image / failed placeholder.
  final double placeholderIconSize;

  const ProviderImage({
    super.key,
    required this.imageUrl,
    this.width,
    this.height,
    this.placeholderIconSize = AppIconSize.md,
  });

  @override
  Widget build(BuildContext context) {
    final url = imageUrl;

    return SizedBox(
      width: width,
      height: height,
      child: url == null || url.isEmpty
          ? _placeholder(context)
          : CachedNetworkImage(
              imageUrl: url,
              fit: BoxFit.cover,
              width: width,
              height: height,
              placeholder: (_, __) => SkeletonLoader(
                child: SkeletonLoader.box(
                  width: width,
                  height: height ?? 96,
                  radius: 0,
                ),
              ),
              errorWidget: (_, __, ___) => _placeholder(context),
            ),
    );
  }

  Widget _placeholder(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      width: width,
      height: height,
      color: theme.colorScheme.surfaceContainerHighest,
      alignment: Alignment.center,
      child: Icon(
        Icons.storefront_outlined,
        size: placeholderIconSize,
        color: theme.colorScheme.onSurfaceVariant,
      ),
    );
  }
}
