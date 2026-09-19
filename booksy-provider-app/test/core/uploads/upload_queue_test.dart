import 'dart:async';

import 'package:booksy_provider_app/core/uploads/upload_queue.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// A fake uploader the test drives by hand: each call parks until the test
/// reports progress, finishes or fails it.
class _FakeUploader {
  final calls = <_Call>[];

  Future<void> call(
    GalleryImageUpload image, {
    required ProgressCallback onProgress,
    required CancelToken cancelToken,
  }) {
    final call = _Call(image, onProgress, cancelToken);
    calls.add(call);
    cancelToken.whenCancel.then((_) {
      if (!call.done.isCompleted) {
        call.done.completeError(
          DioException.requestCancelled(
            requestOptions: RequestOptions(),
            reason: 'cancelled',
          ),
        );
      }
    });
    return call.done.future;
  }
}

class _Call {
  final GalleryImageUpload image;
  final ProgressCallback onProgress;
  final CancelToken cancelToken;
  final done = Completer<void>();
  _Call(this.image, this.onProgress, this.cancelToken);
}

GalleryImageUpload _img(String name, int size) =>
    GalleryImageUpload(name: name, bytes: List<int>.filled(size, 1));

Future<void> _settle() => Future<void>.delayed(Duration.zero);

void main() {
  late _FakeUploader uploader;
  late UploadQueue queue;

  setUp(() {
    uploader = _FakeUploader();
    queue = UploadQueue(uploader.call);
  });

  tearDown(() => queue.dispose());

  test(
    'every picked photo is listed at once, before any upload finishes (U1)',
    () async {
      queue.add([_img('a.jpg', 100), _img('b.jpg', 200), _img('c.jpg', 300)]);
      await _settle();

      expect(queue.items.map((i) => i.image.name), ['a.jpg', 'b.jpg', 'c.jpg']);
      expect(queue.items.first.status, UploadStatus.uploading);
      expect(queue.items[1].status, UploadStatus.queued);
    },
  );

  test('one request per photo, sent in the order picked (U6)', () async {
    queue.add([_img('a.jpg', 100), _img('b.jpg', 200)]);
    await _settle();
    expect(
      uploader.calls.map((c) => c.image.name),
      ['a.jpg'],
      reason: 'one at a time: parallel requests race on the server',
    );

    uploader.calls.first.done.complete();
    await _settle();
    expect(uploader.calls.map((c) => c.image.name), ['a.jpg', 'b.jpg']);
  });

  test('each photo reports its own progress (U2)', () async {
    queue.add([_img('a.jpg', 1000), _img('b.jpg', 1000)]);
    await _settle();

    uploader.calls.first.onProgress(720, 1000);
    await _settle();

    expect(queue.items.first.progress, closeTo(0.72, 0.001));
    expect(queue.items[1].progress, 0);
  });

  test(
    'finished photos are marked done; the footer counts what is left (U3, U4)',
    () async {
      queue.add([_img('a.jpg', 1000), _img('b.jpg', 1000)]);
      await _settle();

      uploader.calls.first.onProgress(1000, 1000);
      uploader.calls.first.done.complete();
      await _settle();

      expect(queue.items.first.status, UploadStatus.done);
      expect(queue.doneCount, 1);
      expect(queue.remainingCount, 1);
      expect(queue.overallProgress, closeTo(0.5, 0.001));
    },
  );

  test('overall progress is weighted by size, not photo count (U4)', () async {
    queue.add([_img('big.jpg', 3000), _img('small.jpg', 1000)]);
    await _settle();

    uploader.calls.first.onProgress(1500, 3000); // half of the big one
    await _settle();

    expect(queue.overallProgress, closeTo(1500 / 4000, 0.001));
  });

  test('a failed photo says so and can be retried (U3)', () async {
    queue.add([_img('a.jpg', 100), _img('b.jpg', 100)]);
    await _settle();

    uploader.calls.first.done.completeError(Exception('network'));
    await _settle();
    expect(queue.items.first.status, UploadStatus.failed);
    expect(
      queue.items[1].status,
      UploadStatus.uploading,
      reason: 'one failure must not stall the rest',
    );

    uploader.calls[1].done.complete();
    await _settle();
    queue.retry(queue.items.first.id);
    await _settle();

    expect(queue.items.first.status, UploadStatus.uploading);
    expect(uploader.calls.last.image.name, 'a.jpg');
  });

  test('cancelling a photo stops its request and removes it (U5)', () async {
    queue.add([_img('a.jpg', 100), _img('b.jpg', 100)]);
    await _settle();

    queue.cancel(queue.items.first.id);
    await _settle();

    expect(uploader.calls.first.cancelToken.isCancelled, isTrue);
    expect(queue.items.map((i) => i.image.name), ['b.jpg']);
    expect(queue.items.single.status, UploadStatus.uploading);
  });

  test(
    'cancel all stops the running request and drops the queue (U4, U5)',
    () async {
      queue.add([_img('a.jpg', 100), _img('b.jpg', 100), _img('c.jpg', 100)]);
      await _settle();

      queue.cancelAll();
      await _settle();

      expect(uploader.calls.single.cancelToken.isCancelled, isTrue);
      expect(queue.items, isEmpty);
      expect(queue.isBusy, isFalse);
    },
  );

  test(
    'onUploaded fires per photo as it lands, so the grid can refresh',
    () async {
      final landed = <String>[];
      queue.dispose();
      queue = UploadQueue(
        uploader.call,
        onUploaded: (i) => landed.add(i.image.name),
      );

      queue.add([_img('a.jpg', 100), _img('b.jpg', 100)]);
      await _settle();
      uploader.calls.first.done.complete();
      await _settle();

      expect(landed, ['a.jpg']);
    },
  );

  test('whenIdle completes once nothing is queued or uploading', () async {
    queue.add([_img('a.jpg', 100)]);
    await _settle();
    var idle = false;
    unawaited(queue.whenIdle().then((_) => idle = true));
    await _settle();
    expect(idle, isFalse);

    uploader.calls.first.done.complete();
    await _settle();
    await _settle();
    expect(idle, isTrue);
  });
}
