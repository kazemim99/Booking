import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/push/push_notice.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A push that arrives while the app is on screen. Neither Android nor the browser shows a banner for the page in
/// front, so the app shows one itself — a snackbar, which never takes the screen the person is using, with a way to
/// open what it is about.
void main() {
  group('pushNoticeFrom', () {
    test('a push with a title and body is shown', () {
      final notice = pushNoticeFrom(title: 'نوبت تأیید شد', body: 'سالن نوبت شما را تأیید کرد', data: {'bookingId': 'b1'});

      expect(notice, isNotNull);
      expect(notice!.text, 'نوبت تأیید شد\nسالن نوبت شما را تأیید کرد');
      expect(notice.data, {'bookingId': 'b1'});
    });

    test('a title alone is enough', () {
      expect(pushNoticeFrom(title: 'نوبت جدید', body: null, data: const {})!.text, 'نوبت جدید');
    });

    test('a push with nothing to read is not shown (the badge still moves)', () {
      expect(pushNoticeFrom(title: null, body: null, data: {'bookingId': 'b1'}), isNull);
      expect(pushNoticeFrom(title: '  ', body: '', data: const {}), isNull);
    });
  });

  testWidgets('the notice offers to open what it is about', (tester) async {
    final messenger = GlobalKey<ScaffoldMessengerState>();
    Map<String, dynamic>? opened;

    await tester.pumpWidget(MaterialApp(
      scaffoldMessengerKey: messenger,
      home: const Scaffold(body: SizedBox()),
    ));

    showPushNotice(
      messenger.currentState,
      pushNoticeFrom(title: 'نوبت تأیید شد', body: null, data: {'bookingId': 'b1'})!,
      onOpen: (data) => opened = data,
    );
    await tester.pumpAndSettle();

    expect(find.text('نوبت تأیید شد'), findsOneWidget);
    await tester.tap(find.text(AppStrings.pushNoticeOpen));
    await tester.pumpAndSettle();

    expect(opened, {'bookingId': 'b1'});
  });

  testWidgets('with no screen to show it on, nothing happens', (tester) async {
    expect(
      () => showPushNotice(null, pushNoticeFrom(title: 't', body: null, data: const {})!, onOpen: (_) {}),
      returnsNormally,
    );
  });
}
