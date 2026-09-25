import 'package:asan_rezerve_provider_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/features/home/domain/entities/home_booking.dart';
import 'package:asan_rezerve_provider_app/features/home/presentation/widgets/now_next.dart';
import 'package:asan_rezerve_provider_app/features/home/presentation/widgets/today_agenda.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The home card and today's agenda offer «تکمیل»/«عدم حضور» only when the server takes them
/// (openspec/changes/_inline/customer-reviews-and-nahal-seed): both were offered on a pending request and hours
/// ahead, where they failed. Marking a visit done is what lets its customer review it.
void main() {
  final now = DateTime(2026, 9, 25, 12);

  HomeBooking booking(String id, DateTime start, HomeBookingStatus status) => HomeBooking(
        id: id,
        start: start,
        end: start.add(const Duration(minutes: 45)),
        clientName: 'ناصر عابدی',
        clientPhone: '09121112233',
        serviceName: 'اصلاح کامل',
        status: status,
      );

  Future<void> pump(WidgetTester tester, Widget child) => tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: SingleChildScrollView(child: child)),
        ),
      ));

  NowNext card(HomeBooking b) => NowNext(
        booking: b,
        inProgress: b.start!.isBefore(now),
        now: now,
        onComplete: (_) {},
        onNoShow: (_) {},
        onCall: (_) {},
      );

  group('the now/next card', () {
    testWidgets('a visit under way offers «تکمیل», not yet «عدم حضور»', (tester) async {
      await pump(tester, card(booking('b1', now.subtract(const Duration(minutes: 20)), HomeBookingStatus.confirmed)));

      expect(find.byKey(const Key('nownext-complete')), findsOneWidget);
      expect(find.byKey(const Key('nownext-noshow')), findsNothing);
    });

    testWidgets('a visit whose time is over offers both', (tester) async {
      await pump(tester, card(booking('b1', now.subtract(const Duration(hours: 2)), HomeBookingStatus.confirmed)));

      expect(find.byKey(const Key('nownext-complete')), findsOneWidget);
      expect(find.byKey(const Key('nownext-noshow')), findsOneWidget);
    });

    testWidgets('a confirmed visit still ahead says when it can be marked done', (tester) async {
      await pump(tester, card(booking('b1', now.add(const Duration(hours: 2)), HomeBookingStatus.confirmed)));

      expect(find.byKey(const Key('nownext-complete')), findsNothing);
      expect(find.text(AppStrings.homeCompleteLaterHint), findsOneWidget);
    });

    testWidgets('a pending request says it awaits confirmation', (tester) async {
      await pump(tester, card(booking('b1', now.subtract(const Duration(minutes: 5)), HomeBookingStatus.pending)));

      expect(find.byKey(const Key('nownext-complete')), findsNothing);
      expect(find.text(AppStrings.homeNextAwaitsConfirmation), findsOneWidget);
    });
  });

  group("today's agenda", () {
    Widget agenda(List<HomeBooking> bookings) => TodayAgenda(
          bookings: bookings,
          tomorrowApptCount: 0,
          onAddAppointment: () {},
          onComplete: (_) {},
          onNoShow: (_) {},
          now: now,
        );

    testWidgets('a row has a menu only when there is something the server takes', (tester) async {
      await pump(tester, agenda([
        booking('past', now.subtract(const Duration(hours: 2)), HomeBookingStatus.confirmed),
        booking('ahead', now.add(const Duration(hours: 2)), HomeBookingStatus.confirmed),
        booking('request', now.subtract(const Duration(hours: 1)), HomeBookingStatus.pending),
      ]));

      expect(find.byKey(const Key('agenda-menu-past')), findsOneWidget);
      expect(find.byKey(const Key('agenda-menu-ahead')), findsNothing);
      expect(find.byKey(const Key('agenda-menu-request')), findsNothing);
    });

    testWidgets('a visit under way offers only «تکمیل»', (tester) async {
      await pump(tester, agenda([
        booking('now', now.subtract(const Duration(minutes: 10)), HomeBookingStatus.confirmed),
      ]));

      await tester.tap(find.byKey(const Key('agenda-menu-now')));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.homeActionComplete), findsOneWidget);
      expect(find.text(AppStrings.homeActionNoShow), findsNothing);
    });
  });
}
