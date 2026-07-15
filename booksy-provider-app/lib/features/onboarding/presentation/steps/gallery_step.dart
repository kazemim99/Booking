import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 6 — gallery (optional). Pick multiple images, preview/remove them, and
/// upload on "next" (multipart → the draft). Mirrors the Vue GalleryStep.
/// Skippable: advancing with no images just moves on.
class GalleryStep extends StatefulWidget {
  const GalleryStep({super.key});

  /// Returns [items] reordered so the element at [mainIndex] comes first,
  /// preserving the relative order of the rest. The picked "main" image uploads
  /// first and gets DisplayOrder 0 (the backend cover).
  static List<T> mainFirst<T>(List<T> items, int mainIndex) {
    if (mainIndex <= 0 || mainIndex >= items.length) return List<T>.of(items);
    return [
      items[mainIndex],
      for (var i = 0; i < items.length; i++)
        if (i != mainIndex) items[i],
    ];
  }

  @override
  State<GalleryStep> createState() => _GalleryStepState();
}

class _GalleryStepState extends State<GalleryStep> {
  static const int _maxImages = 20;

  final ImagePicker _picker = ImagePicker();
  final List<_PickedImage> _images = [];

  /// Index of the "main" image. It is uploaded first so it gets DisplayOrder 0,
  /// which the backend uses as the provider's cover image.
  int _mainIndex = 0;

  Future<void> _pick() async {
    final remaining = _maxImages - _images.length;
    if (remaining <= 0) {
      _snack(AppStrings.galleryLimit(_maxImages));
      return;
    }
    final picked = await _picker.pickMultiImage(imageQuality: 80);
    if (picked.isEmpty || !mounted) return;

    final toAdd = picked.take(remaining);
    final loaded = <_PickedImage>[];
    for (final x in toAdd) {
      loaded.add(_PickedImage(name: x.name, bytes: await x.readAsBytes()));
    }
    if (!mounted) return;
    setState(() => _images.addAll(loaded));
    if (picked.length > remaining) _snack(AppStrings.galleryLimit(_maxImages));
  }

  void _remove(int index) {
    setState(() {
      _images.removeAt(index);
      // Keep the main pointer valid: reset to the first image if the main one
      // was removed, or shift it down if an earlier image was removed.
      if (index == _mainIndex) {
        _mainIndex = 0;
      } else if (index < _mainIndex) {
        _mainIndex--;
      }
      if (_mainIndex >= _images.length) _mainIndex = 0;
    });
  }

  void _setMain(int index) => setState(() => _mainIndex = index);

  void _snack(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    return BlocConsumer<OnboardingCubit, OnboardingState>(
      listenWhen: (prev, curr) => curr.phase == OnboardingPhase.error,
      listener: (context, state) {
        if (state.errorMessage != null) _snack(state.errorMessage!);
      },
      builder: (context, state) {
        final hasImages = _images.isNotEmpty;
        return StepScaffold(
          title: AppStrings.galleryTitle,
          subtitle: AppStrings.gallerySubtitle,
          loading: state.isSaving,
          onBack: cubit.back,
          // Same handler for both: empty list skips, non-empty uploads. The
          // main image goes first so it becomes the cover (DisplayOrder 0).
          onNext: () => cubit.uploadGalleryAndAdvance(
            GalleryStep.mainFirst(
              _images
                  .map((i) => GalleryImageUpload(name: i.name, bytes: i.bytes))
                  .toList(),
              _mainIndex,
            ),
          ),
          nextLabel: hasImages ? AppStrings.next : AppStrings.skip,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              OutlinedButton.icon(
                key: const Key('gallery-add'),
                onPressed: state.isSaving ? null : _pick,
                icon: const Icon(Icons.add_photo_alternate_outlined),
                label: Text(
                  hasImages
                      ? '${AppStrings.addPhotos} (${AppStrings.galleryCount(_images.length)})'
                      : AppStrings.addPhotos,
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              if (hasImages) ...[
                Text(
                  AppStrings.mainImageHint,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                ),
                const SizedBox(height: AppSpacing.sm),
                _grid(),
              ] else
                const Padding(
                  padding: EdgeInsets.symmetric(vertical: AppSpacing.lg),
                  child: AppEmptyState(
                    icon: Icons.photo_library_outlined,
                    message: AppStrings.galleryEmptyCaption,
                    description: AppStrings.gallerySubtitle,
                  ),
                ),
            ],
          ),
        );
      },
    );
  }

  Widget _grid() {
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        crossAxisSpacing: AppSpacing.sm,
        mainAxisSpacing: AppSpacing.sm,
      ),
      itemCount: _images.length,
      itemBuilder: (context, i) => _gridItem(context, i),
    );
  }

  Widget _gridItem(BuildContext context, int i) {
    final theme = Theme.of(context);
    final isMain = i == _mainIndex;
    return Container(
      key: Key('gallery-image-$i'),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(AppRadius.md),
        border: Border.all(
          color: isMain ? theme.colorScheme.primary : Colors.transparent,
          width: 2,
        ),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(AppRadius.md - 2),
        child: Stack(
          fit: StackFit.expand,
          children: [
            Image.memory(_images[i].bytes, fit: BoxFit.cover),
            // Remove (top-left in the visual layout).
            Positioned(
              top: 2,
              left: 2,
              child: Material(
                color: Colors.black45,
                shape: const CircleBorder(),
                child: IconButton(
                  key: Key('gallery-remove-$i'),
                  iconSize: 18,
                  visualDensity: VisualDensity.compact,
                  icon: const Icon(Icons.close, color: Colors.white),
                  onPressed: () => _remove(i),
                ),
              ),
            ),
            // Set-as-main star (top-right).
            Positioned(
              top: 2,
              right: 2,
              child: Material(
                color: Colors.black45,
                shape: const CircleBorder(),
                child: IconButton(
                  key: Key('gallery-main-$i'),
                  iconSize: 18,
                  visualDensity: VisualDensity.compact,
                  tooltip: AppStrings.setAsMainImage,
                  icon: Icon(
                    isMain ? Icons.star : Icons.star_border,
                    color: isMain ? AppColors.warning : Colors.white,
                  ),
                  onPressed: isMain ? null : () => _setMain(i),
                ),
              ),
            ),
            // "Main image" badge along the bottom.
            if (isMain)
              Positioned(
                bottom: 0,
                left: 0,
                right: 0,
                child: Container(
                  color: theme.colorScheme.primary,
                  padding: const EdgeInsets.symmetric(vertical: 2),
                  child: Text(
                    AppStrings.mainImage,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.labelSmall
                        ?.copyWith(color: theme.colorScheme.onPrimary),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _PickedImage {
  final String name;
  final Uint8List bytes;
  const _PickedImage({required this.name, required this.bytes});
}
