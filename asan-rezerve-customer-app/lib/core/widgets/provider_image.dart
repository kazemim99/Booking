import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/foundation.dart' show kIsWeb, visibleForTesting;
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

  /// Makes widget tests take the web branch, which `kIsWeb` never does on the
  /// test VM.
  @visibleForTesting
  static bool? debugIsWebOverride;

  @override
  Widget build(BuildContext context) {
    final url = imageUrl;

    return SizedBox(
      width: width,
      height: height,
      child: url == null || url.isEmpty
          ? _placeholder(context)
          : (debugIsWebOverride ?? kIsWeb)
              ? Image.network(
                  url,
                  fit: BoxFit.cover,
                  width: width,
                  height: height,
                  loadingBuilder: (context, child, progress) =>
                      progress == null ? child : _loading(),
                  errorBuilder: (_, __, ___) => _placeholder(context),
                  // Photos are cached "public" and every *.nahalkmi.ir app
                  // shares one browser cache, so a copy an <img> fetched on
                  // the admin or Vue site (no CORS header) can fail Flutter's
                  // CORS fetch; an HTML image still shows it
                  // (salon-images-load, G7).
                  webHtmlElementStrategy: WebHtmlElementStrategy.fallback,
                )
              : CachedNetworkImage(
                  imageUrl: url,
                  fit: BoxFit.cover,
                  width: width,
                  height: height,
                  placeholder: (_, __) => _loading(),
                  errorWidget: (_, __, ___) => _placeholder(context),
                ),
    );
  }

  Widget _loading() => SkeletonLoader(
        child: SkeletonLoader.box(width: width, height: height ?? 96, radius: 0),
      );

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
