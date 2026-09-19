import 'dart:typed_data';

import 'package:flutter/material.dart';

import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import 'upload_queue.dart';

/// Photos on their way to the server: a grid of tiles plus a footer with the
/// overall progress. Draws nothing while the queue is empty.
///
/// Each tile shows the photo itself from the picked file — straight away, not
/// after the upload — with its own percentage over it, so a batch of photos on
/// a slow connection reads as "working", never as "frozen".
class UploadProgressPanel extends StatelessWidget {
  final UploadQueue queue;

  const UploadProgressPanel({super.key, required this.queue});

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: queue,
      builder: (context, _) {
        final items = queue.items;
        if (items.isEmpty) return const SizedBox.shrink();
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            GridView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: AppSpacing.sm,
                crossAxisSpacing: AppSpacing.sm,
                childAspectRatio: 0.95,
              ),
              itemCount: items.length,
              itemBuilder: (context, i) => UploadTile(
                index: i,
                item: items[i],
                onCancel: () => queue.cancel(items[i].id),
                onRetry: () => queue.retry(items[i].id),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            UploadSummaryBar(queue: queue),
          ],
        );
      },
    );
  }
}

/// One photo: its thumbnail, a circular percentage while it uploads, then a
/// done mark — or a failure with a retry.
class UploadTile extends StatelessWidget {
  final int index;
  final UploadItem item;
  final VoidCallback onCancel;
  final VoidCallback onRetry;

  const UploadTile({
    super.key,
    required this.index,
    required this.item,
    required this.onCancel,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final percent = (item.progress * 100).round();

    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: theme.colorScheme.outlineVariant),
        borderRadius: BorderRadius.circular(AppRadius.md),
      ),
      padding: const EdgeInsets.all(AppSpacing.xs),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(AppRadius.sm),
              child: Stack(
                fit: StackFit.expand,
                children: [
                  Image.memory(
                    Uint8List.fromList(item.image.bytes),
                    key: Key('upload-thumb-$index'),
                    fit: BoxFit.cover,
                    gaplessPlayback: true,
                    errorBuilder: (context, error, stack) =>
                        const ColoredBox(color: Colors.black12),
                  ),
                  if (item.status != UploadStatus.done)
                    const ColoredBox(color: Color(0x55000000)),
                  Center(child: _overlay(context, percent)),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.image.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.bodySmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    Text(
                      _statusLine(),
                      maxLines: 1,
                      textDirection: TextDirection.ltr,
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: item.status == UploadStatus.failed
                            ? theme.colorScheme.error
                            : theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              _trailing(context),
            ],
          ),
        ],
      ),
    );
  }

  Widget _overlay(BuildContext context, int percent) {
    switch (item.status) {
      case UploadStatus.done:
        return CircleAvatar(
          key: Key('upload-done-$index'),
          radius: 18,
          backgroundColor: AppColors.success,
          child: const Icon(Icons.check, color: Colors.white),
        );
      case UploadStatus.failed:
        return const Icon(Icons.error_outline, color: Colors.white, size: 36);
      case UploadStatus.queued:
        return Text(
          AppStrings.uploadQueued,
          style: const TextStyle(
            color: Colors.white,
            fontWeight: FontWeight.w600,
          ),
        );
      case UploadStatus.uploading:
        return SizedBox(
          width: 64,
          height: 64,
          child: Stack(
            fit: StackFit.expand,
            children: [
              CircularProgressIndicator(
                value: item.progress,
                strokeWidth: 5,
                color: Colors.white,
                backgroundColor: Colors.white24,
              ),
              Center(
                child: Text(
                  '$percent%',
                  textDirection: TextDirection.ltr,
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
        );
    }
  }

  Widget _trailing(BuildContext context) {
    switch (item.status) {
      case UploadStatus.done:
        return const Icon(
          Icons.check_circle,
          color: AppColors.success,
          size: 20,
        );
      case UploadStatus.failed:
        return IconButton(
          key: Key('upload-retry-$index'),
          tooltip: AppStrings.uploadRetry,
          icon: const Icon(Icons.refresh),
          onPressed: onRetry,
        );
      case UploadStatus.queued:
      case UploadStatus.uploading:
        return IconButton(
          key: Key('upload-cancel-$index'),
          tooltip: AppStrings.cancel,
          icon: const Icon(Icons.close, size: 18),
          onPressed: onCancel,
        );
    }
  }

  String _statusLine() => switch (item.status) {
    UploadStatus.failed => AppStrings.uploadFailed,
    UploadStatus.done => formatBytes(item.sizeBytes),
    _ => '${formatBytes(item.sentBytes)} / ${formatBytes(item.sizeBytes)}',
  };
}

/// "3 of 4 uploading" with the overall percentage, and cancel-all.
class UploadSummaryBar extends StatelessWidget {
  final UploadQueue queue;

  const UploadSummaryBar({super.key, required this.queue});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final percent = (queue.overallProgress * 100).round();
    final total = queue.items.length;
    final remaining = queue.remainingCount;

    return Row(
      children: [
        if (queue.isBusy)
          const SizedBox(
            width: 22,
            height: 22,
            child: CircularProgressIndicator(strokeWidth: 3),
          )
        else
          const Icon(Icons.check_circle, color: AppColors.success),
        const SizedBox(width: AppSpacing.sm),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      queue.isBusy
                          ? AppStrings.uploadRemaining(remaining, total)
                          : AppStrings.uploadAllDone,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  Text('$percent%', textDirection: TextDirection.ltr),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
              LinearProgressIndicator(
                value: queue.overallProgress,
                minHeight: 6,
                borderRadius: BorderRadius.circular(3),
              ),
            ],
          ),
        ),
        if (queue.isBusy) ...[
          const SizedBox(width: AppSpacing.sm),
          OutlinedButton(
            key: const Key('upload-cancel-all'),
            // Intrinsic width: the app's button theme is full-width by default.
            style: OutlinedButton.styleFrom(minimumSize: const Size(0, 40)),
            onPressed: queue.cancelAll,
            child: const Text(AppStrings.uploadCancelAll),
          ),
        ],
      ],
    );
  }
}

/// 3400000 -> "3.2 MB"; small files in KB.
String formatBytes(int bytes) {
  if (bytes >= 1024 * 1024) {
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }
  return '${(bytes / 1024).toStringAsFixed(0)} KB';
}
