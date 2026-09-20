import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/widgets/provider_image.dart';

/// The salon's photos at the top of its profile, swiped through
/// (openspec/changes/customer-app-discovery-pass). The photo the salon chose
/// comes first, because the catalogue returns them in that order.
///
/// With a single photo it is exactly the old hero image, minus the dots: a row
/// of one dot says nothing. With none, [ProviderImage] draws its placeholder,
/// so the header keeps its shape whatever the salon has uploaded.
class ProviderGallery extends StatefulWidget {
  final List<String> images;

  /// Shown when [images] is empty — the salon's single picture, if it has one.
  final String? fallbackImageUrl;
  final double height;

  const ProviderGallery({
    super.key,
    required this.images,
    this.fallbackImageUrl,
    this.height = 200,
  });

  @override
  State<ProviderGallery> createState() => _ProviderGalleryState();
}

class _ProviderGalleryState extends State<ProviderGallery> {
  final _controller = PageController();
  int _page = 0;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final images = widget.images;
    if (images.length < 2) {
      return ProviderImage(
        key: const Key('provider-hero-image'),
        imageUrl: images.isNotEmpty ? images.first : widget.fallbackImageUrl,
        width: double.infinity,
        height: widget.height,
        placeholderIconSize: AppIconSize.hero,
      );
    }

    return SizedBox(
      height: widget.height,
      child: Stack(
        alignment: Alignment.bottomCenter,
        children: [
          PageView.builder(
            key: const Key('provider-gallery'),
            controller: _controller,
            itemCount: images.length,
            onPageChanged: (index) => setState(() => _page = index),
            itemBuilder: (context, index) => ProviderImage(
              key: Key('provider-gallery-$index'),
              imageUrl: images[index],
              width: double.infinity,
              height: widget.height,
              placeholderIconSize: AppIconSize.hero,
            ),
          ),
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                for (var i = 0; i < images.length; i++)
                  Container(
                    width: 7,
                    height: 7,
                    margin: const EdgeInsets.symmetric(horizontal: 3),
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: i == _page
                          ? Colors.white
                          : Colors.white.withValues(alpha: 0.45),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
