import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/config/theme/app_tokens.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/widgets/app_status_badge.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_booking.dart';
import 'package:booksy_provider_app/features/home/presentation/widgets/booking_card.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Rich booking card (ColiRide ride-card anatomy): avatar + bold name +
/// price + status pill, with the muted time-range/service row beneath.
void main() {
  Future<void> pump(WidgetTester tester, Widget child) {
    return tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(body: Padding(padding: const EdgeInsets.all(16), child: child)),
        ),
      ),
    );
  }

  HomeBooking booking({
    HomeBookingStatus status = HomeBookingStatus.confirmed,
    double? price = 250000,
    DateTime? end,
  }) {
    return HomeBooking(
      id: 'b1',
      start: DateTime(2026, 7, 21, 10),
      end: end ?? DateTime(2026, 7, 21, 10, 45),
      clientName: 'سارا محمدی',
      clientPhone: '0912',
      serviceName: 'اصلاح مو',
      price: price,
      currency: 'IRR',
      status: status,
    );
  }

  testWidgets('renders avatar initial, bold name, grouped price, time range '
      'and service', (tester) async {
    await pump(tester, BookingCard(booking: booking()));

    expect(find.text('س'), findsOneWidget); // avatar initial
    expect(find.text('سارا محمدی'), findsOneWidget);
    expect(find.text('250,000'), findsOneWidget); // grouped thousands
    expect(find.text('10:00 تا 10:45'), findsOneWidget);
    expect(find.textContaining('اصلاح مو'), findsOneWidget);
  });

  testWidgets('status pill maps lifecycle to semantic colors',
      (tester) async {
    await pump(tester, BookingCard(booking: booking()));
    expect(find.text(AppStrings.homeStatusConfirmed), findsOneWidget);

    await pump(
        tester, BookingCard(booking: booking(status: HomeBookingStatus.pending)));
    final pendingBadge =
        tester.widget<AppStatusBadge>(find.byType(AppStatusBadge));
    expect(pendingBadge.status, AppBadgeStatus.warning);
    expect(find.text(AppStrings.homeStatusPending), findsOneWidget);

    await pump(tester,
        BookingCard(booking: booking(status: HomeBookingStatus.cancelled)));
    expect(find.text(AppStrings.homeStatusCancelled), findsOneWidget);
  });

  testWidgets('without a price the amount column is absent', (tester) async {
    await pump(tester, BookingCard(booking: booking(price: null)));

    expect(find.text('250,000'), findsNothing);
    expect(find.text('سارا محمدی'), findsOneWidget);
  });

  testWidgets('highlighted card uses the soft primary fill', (tester) async {
    await pump(tester, BookingCard(booking: booking(), highlighted: true));

    final material = tester.widget<Material>(
      find
          .descendant(
              of: find.byType(BookingCard), matching: find.byType(Material))
          .first,
    );
    expect(material.color, AppColors.primarySoft);
  });

  testWidgets('tap fires onTap', (tester) async {
    var tapped = false;
    await pump(
        tester, BookingCard(booking: booking(), onTap: () => tapped = true));

    await tester.tap(find.byType(BookingCard));
    expect(tapped, isTrue);
  });
}
