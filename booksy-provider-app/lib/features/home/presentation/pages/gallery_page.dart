import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_page_scaffold.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../onboarding/domain/entities/onboarding_data.dart'
    show GalleryImageUpload;
import '../../domain/entities/more_models.dart';
import '../cubit/more_cubits.dart';

/// Picks images from the device; injectable so widget tests avoid platform
/// channels (design D2).
typedef PickGalleryImages = Future<List<GalleryImageUpload>> Function();

Future<List<GalleryImageUpload>> _pickWithImagePicker() async {
  final picked = await ImagePicker().pickMultiImage(imageQuality: 80);
  return [
    for (final file in picked)
      GalleryImageUpload(name: file.name, bytes: await file.readAsBytes()),
  ];
}

/// More → گالری (spec: provider-gallery-management).
class GalleryPage extends StatelessWidget {
  const GalleryPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<GalleryCubit>(
      create: (_) => getIt<GalleryCubit>()..load(),
      child: const GalleryView(),
    );
  }
}

/// Separated from [GalleryPage] so tests can pump it with a fake cubit and a
/// fake picker.
class GalleryView extends StatelessWidget {
  final PickGalleryImages pickImages;

  const GalleryView({super.key, this.pickImages = _pickWithImagePicker});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<GalleryCubit, MoreState<List<GalleryImage>>>(
      builder: (context, state) {
        final cubit = context.read<GalleryCubit>();
        return AppPageScaffold(
          title: AppStrings.moreGallery,
          actions: [
            IconButton(
              key: const Key('gallery-upload'),
              tooltip: AppStrings.galleryUpload,
              // Green add affordance: brand blue would vanish on the chrome.
              icon: const Icon(Icons.add_photo_alternate,
                  color: AppColors.success),
              onPressed: () => _upload(context, cubit),
            ),
          ],
          body: switch (state.status) {
            MoreStatus.loading =>
              const Center(child: CircularProgressIndicator()),
            MoreStatus.failed => AppErrorState(
                message: state.error ?? AppStrings.homeLoadError,
                onRetry: cubit.load,
              ),
            MoreStatus.ready => (state.data ?? const []).isEmpty
                ? AppEmptyState(
                    icon: Icons.photo_library_outlined,
                    message: AppStrings.galleryEmpty,
                    description: AppStrings.galleryEmptyBody,
                    actionLabel: '+ ${AppStrings.galleryUpload}',
                    onAction: () => _upload(context, cubit),
                  )
                : GridView.builder(
                    key: const Key('gallery-grid'),
                    padding: const EdgeInsets.all(AppSpacing.md),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 3,
                      mainAxisSpacing: AppSpacing.sm,
                      crossAxisSpacing: AppSpacing.sm,
                    ),
                    itemCount: state.data!.length,
                    itemBuilder: (context, i) =>
                        _tile(context, cubit, state.data![i]),
                  ),
          },
        );
      },
    );
  }

  Future<void> _upload(BuildContext context, GalleryCubit cubit) async {
    final images = await pickImages();
    if (images.isEmpty || !context.mounted) return;
    final failure = await cubit.uploadImages(images);
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, AppStrings.galleryUploaded);
    } else {
      AppSnackbar.error(context, failure.message);
    }
  }

  Widget _tile(
      BuildContext context, GalleryCubit cubit, GalleryImage image) {
    return InkWell(
      key: Key('gallery-image-${image.id}'),
      onTap: () => _showImageSheet(context, cubit, image),
      borderRadius: BorderRadius.circular(AppRadius.md),
      child: Stack(
        fit: StackFit.expand,
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(AppRadius.md),
            child: image.thumbnailUrl.isEmpty
                ? _placeholder()
                : Image.network(
                    image.thumbnailUrl,
                    fit: BoxFit.cover,
                    // Degrades to a neutral tile when the URL fails
                    // (also keeps widget tests network-free — design D4).
                    errorBuilder: (_, _, _) => _placeholder(),
                  ),
          ),
          if (image.isPrimary)
            PositionedDirectional(
              top: AppSpacing.xs,
              start: AppSpacing.xs,
              child: Container(
                key: Key('gallery-primary-${image.id}'),
                padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.xs, vertical: 2),
                decoration: BoxDecoration(
                  color: AppColors.success,
                  borderRadius: BorderRadius.circular(AppRadius.sm),
                ),
                child: const Text(
                  AppStrings.galleryPrimaryBadge,
                  style: TextStyle(fontSize: 10, color: Colors.white),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _placeholder() => Container(
        color: AppColors.surfaceSoft,
        child: const Icon(Icons.image_outlined,
            color: AppColors.icon, size: AppIconSize.md),
      );

  void _showImageSheet(
      BuildContext context, GalleryCubit cubit, GalleryImage image) {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (!image.isPrimary)
              ListTile(
                key: const Key('gallery-set-primary'),
                leading:
                    const Icon(Icons.star_outline, color: AppColors.primary),
                title: const Text(AppStrings.gallerySetPrimary),
                onTap: () async {
                  Navigator.pop(sheetContext);
                  final failure = await cubit.setPrimary(image.id);
                  if (!context.mounted) return;
                  failure == null
                      ? AppSnackbar.success(
                          context, AppStrings.galleryPrimarySet)
                      : AppSnackbar.error(context, failure.message);
                },
              ),
            ListTile(
              key: const Key('gallery-delete'),
              leading:
                  const Icon(Icons.delete_outline, color: AppColors.danger),
              title: const Text(
                AppStrings.galleryRemove,
                style: TextStyle(color: AppColors.danger),
              ),
              onTap: () {
                Navigator.pop(sheetContext);
                _confirmRemove(context, cubit, image);
              },
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
    );
  }

  Future<void> _confirmRemove(
      BuildContext context, GalleryCubit cubit, GalleryImage image) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text(AppStrings.galleryRemoveConfirmTitle),
        content: const Text(AppStrings.galleryRemoveConfirmBody),
        actions: [
          TextButton(
            key: const Key('gallery-remove-cancel'),
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text(AppStrings.cancel),
          ),
          TextButton(
            key: const Key('gallery-remove-confirm'),
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text(AppStrings.staffRemoveConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    final failure = await cubit.removeImage(image.id);
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, AppStrings.galleryRemoved);
    } else {
      AppSnackbar.error(context, failure.message);
    }
  }
}
