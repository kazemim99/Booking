import 'dart:convert';
import 'dart:typed_data';

import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/features/booking/data/datasources/booking_remote_datasource.dart';
import 'package:booksy_customer_app/features/booking/data/repositories/booking_repository_impl.dart';
import 'package:booksy_customer_app/features/booking/presentation/widgets/slot_picker.dart';
import 'package:dio/dio.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Why a day has no times (QA walkthrough 2026-09-22): the salon is closed that day, the day is shorter than the
/// visit, nobody is qualified. The server says which; the app used to show only "no free time for this day".
class _Adapter implements HttpClientAdapter {
  Map<String, dynamic> body;
  _Adapter(this.body);

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream,
      Future<void>? cancelFuture) async =>
      ResponseBody.fromString(jsonEncode(body), 200, headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      });

  @override
  void close({bool force = false}) {}
}

void main() {
  BookingRepositoryImpl repositoryWith(Map<String, dynamic> body) {
    final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = _Adapter(body);
    return BookingRepositoryImpl(
      remoteDataSource: BookingRemoteDataSource(serviceCatalogDio: dio),
    );
  }

  test('an empty day comes back with the salon\'s reason', () async {
    final repository = repositoryWith({
      'data': {
        'slots': <dynamic>[],
        'validationMessages': ['مجموعه در این روز تعطیل است.'],
      },
    });

    final day = (await repository.getAvailableSlots(
      providerId: 'p1',
      serviceId: 's1',
      date: DateTime(2026, 9, 23),
    )).getOrElse(() => throw 'no day');

    expect(day.slots, isEmpty);
    expect(day.reason, 'مجموعه در این روز تعطیل است.');
  });

  test('a day with times carries no reason', () async {
    final repository = repositoryWith({
      'data': {
        'slots': [
          {'startTime': '2026-09-23T10:00:00', 'endTime': '2026-09-23T10:45:00', 'isAvailable': true},
        ],
      },
    });

    final day = (await repository.getAvailableSlots(
      providerId: 'p1',
      serviceId: 's1',
      date: DateTime(2026, 9, 23),
    )).getOrElse(() => throw 'no day');

    expect(day.slots, hasLength(1));
    expect(day.reason, isNull);
  });

  Future<void> pumpPicker(WidgetTester tester, {String? reason}) async {
    tester.view.physicalSize = const Size(390 * 3, 1200 * 3);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: Scaffold(
          body: SlotPicker(
            selectedDate: DateTime(2026, 9, 23),
            onDateSelected: (_) {},
            status: SlotPickerStatus.loaded,
            slots: const [],
            selectedSlot: null,
            onSlotSelected: (_) {},
            onRetry: () {},
            emptyReason: reason,
            // The date strip is not what these tests are about, and its fixed-height chips overflow under the
            // test font. The empty state below it is the subject.
            daysToShow: 0,
          ),
        ),
      ),
    ));
    await tester.pump();
  }

  testWidgets('the picker shows the reason instead of the generic line', (tester) async {
    await pumpPicker(tester, reason: 'مجموعه در این روز تعطیل است.');

    expect(find.text('مجموعه در این روز تعطیل است.'), findsOneWidget);
    expect(find.text(AppStrings.bookingNoSlots), findsNothing);
  });

  testWidgets('without a reason it still says there are no times', (tester) async {
    await pumpPicker(tester);

    expect(find.text(AppStrings.bookingNoSlots), findsOneWidget);
  });
}
