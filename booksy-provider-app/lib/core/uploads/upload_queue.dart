import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../../features/onboarding/domain/entities/onboarding_data.dart';

enum UploadStatus { queued, uploading, done, failed }

/// One picked photo on its way to the server.
class UploadItem {
  final String id;
  final GalleryImageUpload image;
  UploadStatus status;

  /// 0..1, from the request's own byte counter.
  double progress;
  String? error;
  CancelToken? _cancelToken;

  UploadItem._(this.id, this.image)
    : status = UploadStatus.queued,
      progress = 0;

  int get sizeBytes => image.bytes.length;
  int get sentBytes => (sizeBytes * progress).round();
}

/// Uploads one photo; reports bytes sent and honours [cancelToken].
typedef UploadFunction =
    Future<void> Function(
      GalleryImageUpload image, {
      required ProgressCallback onProgress,
      required CancelToken cancelToken,
    });

/// Sends picked photos to the server one request each, showing every photo at
/// once and each one's progress as it goes.
///
/// Previously every photo went in ONE request with no progress, and the screen
/// showed nothing until the whole batch finished — long enough, on a phone, for
/// the provider to think the app had frozen.
///
/// One after another, not in parallel: the server adds each photo to the
/// provider aggregate and numbers it after the last one, so parallel requests
/// would race for the same aggregate version (a concurrency failure) and
/// scramble the picked order. Sequential still gives each photo its own
/// progress; the ones waiting show their thumbnail as queued.
class UploadQueue extends ChangeNotifier {
  UploadQueue(this._upload, {this.onUploaded});

  final UploadFunction _upload;

  /// Called as each photo lands, so a screen can show it without waiting for
  /// the rest.
  final void Function(UploadItem item)? onUploaded;

  final List<UploadItem> _items = [];
  var _nextId = 0;
  var _running = false;
  var _disposed = false;
  Completer<void>? _idle;

  List<UploadItem> get items => List.unmodifiable(_items);

  int get doneCount =>
      _items.where((i) => i.status == UploadStatus.done).length;

  /// Queued or uploading.
  int get remainingCount => _items
      .where(
        (i) =>
            i.status == UploadStatus.queued ||
            i.status == UploadStatus.uploading,
      )
      .length;

  int get failedCount =>
      _items.where((i) => i.status == UploadStatus.failed).length;

  bool get isBusy => remainingCount > 0;

  /// Bytes sent over bytes to send, across every photo still in the list — so a
  /// large photo weighs more than a small one, as it does for the network.
  double get overallProgress {
    final total = _items.fold<int>(0, (sum, i) => sum + i.sizeBytes);
    if (total == 0) return 0;
    final sent = _items.fold<int>(
      0,
      (sum, i) =>
          sum + (i.status == UploadStatus.done ? i.sizeBytes : i.sentBytes),
    );
    return sent / total;
  }

  void add(List<GalleryImageUpload> images) {
    for (final image in images) {
      _items.add(UploadItem._('${_nextId++}', image));
    }
    _notify();
    _pump();
  }

  /// Stops a photo's request if it is running, and drops it from the list.
  void cancel(String id) {
    final item = _find(id);
    if (item == null) return;
    item._cancelToken?.cancel('cancelled by the user');
    _items.remove(item);
    _notify();
    _completeIfIdle();
  }

  void cancelAll() {
    for (final item in _items) {
      item._cancelToken?.cancel('cancelled by the user');
    }
    _items.clear();
    _notify();
    _completeIfIdle();
  }

  void retry(String id) {
    final item = _find(id);
    if (item == null || item.status != UploadStatus.failed) return;
    item
      ..status = UploadStatus.queued
      ..progress = 0
      ..error = null;
    _notify();
    _pump();
  }

  /// Completes when nothing is queued or uploading (immediately if already so).
  Future<void> whenIdle() {
    if (!isBusy) return Future.value();
    return (_idle ??= Completer<void>()).future;
  }

  Future<void> _pump() async {
    if (_running) return;
    _running = true;
    try {
      while (!_disposed) {
        final next = _items.cast<UploadItem?>().firstWhere(
          (i) => i!.status == UploadStatus.queued,
          orElse: () => null,
        );
        if (next == null) break;
        await _send(next);
      }
    } finally {
      _running = false;
      _completeIfIdle();
    }
  }

  Future<void> _send(UploadItem item) async {
    final token = CancelToken();
    item
      .._cancelToken = token
      ..status = UploadStatus.uploading
      ..progress = 0;
    _notify();

    try {
      await _upload(
        item.image,
        cancelToken: token,
        onProgress: (sent, total) {
          if (total <= 0) return;
          item.progress = (sent / total).clamp(0.0, 1.0);
          _notify();
        },
      );
      if (!_items.contains(item)) return; // cancelled while finishing
      item
        ..status = UploadStatus.done
        ..progress = 1;
      _notify();
      onUploaded?.call(item);
    } catch (e) {
      if (token.isCancelled || !_items.contains(item)) return;
      item
        ..status = UploadStatus.failed
        ..error = e is DioException
            ? (e.message ?? e.toString())
            : e.toString();
      _notify();
    } finally {
      item._cancelToken = null;
    }
  }

  UploadItem? _find(String id) {
    for (final item in _items) {
      if (item.id == id) return item;
    }
    return null;
  }

  void _completeIfIdle() {
    if (!isBusy && _idle != null && !_idle!.isCompleted) {
      _idle!.complete();
      _idle = null;
    }
  }

  void _notify() {
    if (!_disposed) notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    for (final item in _items) {
      item._cancelToken?.cancel('disposed');
    }
    super.dispose();
  }
}
