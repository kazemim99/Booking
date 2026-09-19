import 'dart:async';
import 'dart:typed_data';

import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/uploads/upload_progress_widgets.dart';
import 'package:booksy_provider_app/core/uploads/upload_queue.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

// A 1x1 transparent PNG: a real, decodable image for the thumbnail.
final _png = Uint8List.fromList(const [
  0x89,
  0x50,
  0x4E,
  0x47,
  0x0D,
  0x0A,
  0x1A,
  0x0A,
  0x00,
  0x00,
  0x00,
  0x0D,
  0x49,
  0x48,
  0x44,
  0x52,
  0x00,
  0x00,
  0x00,
  0x01,
  0x00,
  0x00,
  0x00,
  0x01,
  0x08,
  0x06,
  0x00,
  0x00,
  0x00,
  0x1F,
  0x15,
  0xC4,
  0x89,
  0x00,
  0x00,
  0x00,
  0x0A,
  0x49,
  0x44,
  0x41,
  0x54,
  0x78,
  0x9C,
  0x63,
  0x00,
  0x01,
  0x00,
  0x00,
  0x05,
  0x00,
  0x01,
  0x0D,
  0x0A,
  0x2D,
  0xB4,
  0x00,
  0x00,
  0x00,
  0x00,
  0x49,
  0x45,
  0x4E,
  0x44,
  0xAE,
  0x42,
  0x60,
  0x82,
]);

class _Parked {
  final calls = <({ProgressCallback onProgress, Completer<void> done})>[];
  Future<void> call(
    GalleryImageUpload image, {
    required ProgressCallback onProgress,
    required CancelToken cancelToken,
  }) {
    final done = Completer<void>();
    calls.add((onProgress: onProgress, done: done));
    cancelToken.whenCancel.then((_) {
      if (!done.isCompleted) done.completeError(Exception('cancelled'));
    });
    return done.future;
  }
}

void main() {
  late _Parked uploader;
  late UploadQueue queue;

  setUp(() {
    uploader = _Parked();
    queue = UploadQueue(uploader.call);
  });
  tearDown(() => queue.dispose());

  Future<void> pump(WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: UploadProgressPanel(queue: queue)),
        ),
      ),
    );
    await tester.pump();
  }

  GalleryImageUpload photo(String name) =>
      GalleryImageUpload(name: name, bytes: _png.toList());

  testWidgets('a picked photo shows its own thumbnail straight away (U1)', (
    tester,
  ) async {
    queue.add([photo('IMG_2487.jpg'), photo('IMG_2488.jpg')]);
    await pump(tester);

    expect(find.byType(UploadTile), findsNWidgets(2));
    expect(find.byKey(const Key('upload-thumb-0')), findsOneWidget);
    expect(find.text('IMG_2487.jpg'), findsOneWidget);
    // The one waiting its turn says so rather than looking stuck.
    expect(find.text(AppStrings.uploadQueued), findsOneWidget);
  });

  testWidgets('each photo shows its percentage (U2)', (tester) async {
    queue.add([photo('a.jpg')]);
    await pump(tester);

    uploader.calls.single.onProgress(72, 100);
    await tester.pump();

    // On the photo itself (the footer shows the overall % too).
    expect(
      find.descendant(of: find.byType(UploadTile), matching: find.text('72%')),
      findsOneWidget,
    );
  });

  testWidgets('a finished photo gets a done mark (U3)', (tester) async {
    queue.add([photo('a.jpg')]);
    await pump(tester);

    uploader.calls.single.done.complete();
    await tester.pump();
    await tester.pump();

    expect(find.byKey(const Key('upload-done-0')), findsOneWidget);
  });

  testWidgets('a failed photo says so and retries on tap (U3)', (tester) async {
    queue.add([photo('a.jpg')]);
    await pump(tester);

    uploader.calls.single.done.completeError(Exception('network'));
    await tester.pump();
    await tester.pump();

    expect(find.text(AppStrings.uploadFailed), findsOneWidget);
    await tester.tap(find.byKey(const Key('upload-retry-0')));
    await tester.pump();
    expect(uploader.calls, hasLength(2));
  });

  testWidgets(
    'the footer counts what is left, shows the overall %, and cancels all (U4)',
    (tester) async {
      queue.add([photo('a.jpg'), photo('b.jpg')]);
      await pump(tester);

      expect(find.text(AppStrings.uploadRemaining(2, 2)), findsOneWidget);

      await tester.tap(find.byKey(const Key('upload-cancel-all')));
      await tester.pump();

      expect(queue.items, isEmpty);
      expect(find.byType(UploadTile), findsNothing);
    },
  );

  testWidgets('cancelling one photo leaves the others (U5)', (tester) async {
    queue.add([photo('a.jpg'), photo('b.jpg')]);
    await pump(tester);

    await tester.tap(find.byKey(const Key('upload-cancel-0')));
    await tester.pump();

    expect(find.text('a.jpg'), findsNothing);
    expect(find.text('b.jpg'), findsOneWidget);
  });

  testWidgets('nothing is drawn when there is nothing to upload', (
    tester,
  ) async {
    await pump(tester);
    expect(find.byKey(const Key('upload-cancel-all')), findsNothing);
  });
}
