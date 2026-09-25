import 'package:asan_rezerve_provider_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/features/home/domain/entities/home_booking.dart';
import 'package:asan_rezerve_provider_app/features/home/presentation/widgets/action_queue.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// "How do I know who I'm confirming?" (QA 2026-09-24). A request names its customer on a line of its own, in bold,
/// above the service and time — and says «بدون نام» rather than leaving the salon to guess from a service and a time.
void main() {
  Future<void> pump(WidgetTester tester, HomeBooking request) {
    return tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(
            body: SingleChildScrollView(
              child: ActionQueue(requests: [request], onConfirm: (_) {}, onDecline: (_) {}),
            ),
          ),
        ),
      ),
    );
  }

  HomeBooking request({String clientName = 'مصطفی کاظمی'}) => HomeBooking(
        id: 'r1',
        start: DateTime(2026, 9, 25, 10),
        clientName: clientName,
        serviceName: 'اصلاح سر',
        status: HomeBookingStatus.pending,
      );

  testWidgets('the customer is named on a line of their own, in bold', (tester) async {
    await pump(tester, request());

    final name = tester.widget<Text>(find.text('مصطفی کاظمی'));
    expect(name.style?.fontWeight, FontWeight.w700);
    expect(find.text('اصلاح سر · 10:00'), findsOneWidget);
  });

  testWidgets('a request without a name says so instead of showing only the service', (tester) async {
    await pump(tester, request(clientName: ''));

    expect(find.text(AppStrings.memberNameMissing), findsOneWidget);
  });
}
