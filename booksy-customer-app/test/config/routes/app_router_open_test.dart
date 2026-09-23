import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/routes/app_router.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/di/injection.dart';
import 'package:booksy_customer_app/core/network/connectivity_service.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/booking/presentation/bloc/booking_bloc.dart';
import 'package:booksy_customer_app/features/booking/presentation/pages/booking_flow_page.dart';
import 'package:booksy_customer_app/features/bookings/domain/repositories/bookings_repository.dart';
import 'package:booksy_customer_app/features/bookings/presentation/bloc/appointments_bloc.dart';
import 'package:booksy_customer_app/features/bookings/presentation/pages/appointment_detail_page.dart';
import 'package:booksy_customer_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/provider_customer_cubit.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/provider_detail_cubit.dart';
import 'package:booksy_customer_app/features/search/presentation/pages/provider_detail_page.dart';

import '../../features/bookings/bookings_fakes.dart';
import '../../helpers/fake_auth_bloc.dart';

/// Opening a screen from outside the screen stack — a tapped push notification — with the app's REAL router.
///
/// Booking and checkout are drawn on the root navigator, above the tab shell. go_router 13 folds a pushed tab route
/// into the shell only when the shell is the top of the stack; above booking it adds a second copy of the shell whose
/// navigators reuse the first one's keys, and the app dies on a duplicate-GlobalKey assertion (review of the merged
/// branch, 2026-09-23).

class _Online implements ConnectivityService {
  @override
  Stream<bool> get onStatusChange => const Stream<bool>.empty();

  @override
  Future<bool> get isOnline async => true;
}

class _IdleProviderDetailCubit extends ProviderDetailCubit {
  _IdleProviderDetailCubit(super.repository);

  @override
  Future<void> load(String providerId) async {}
}

class _NoCustomerRepository implements HomeRepository {
  @override
  dynamic noSuchMethod(Invocation invocation) => throw UnimplementedError('${invocation.memberName}');
}

void main() {
  late FakeAuthBloc auth;
  late GoRouter router;

  setUp(() {
    final bookings = FakeBookings(upcoming: [fakeBooking('b9')]);
    final slots = FakeSlots();
    getIt
      ..registerSingleton<ConnectivityService>(_Online())
      ..registerSingleton<BookingsRepository>(bookings)
      ..registerSingleton<BookingRepository>(slots)
      ..registerLazySingleton<BookingBloc>(() => BookingBloc(slots))
      ..registerFactory<AppointmentsBloc>(() => AppointmentsBloc(bookings))
      ..registerFactory<ProviderDetailCubit>(() => _IdleProviderDetailCubit(slots))
      ..registerFactoryParam<ProviderCustomerCubit, String, void>(
        (providerId, _) => ProviderCustomerCubit(
          providerId: providerId,
          repository: _NoCustomerRepository(),
          customerId: () async => null,
        ),
      );
    auth = FakeAuthBloc()..signIn();
    router = AppRouter.create(auth);
  });

  tearDown(() async {
    router.dispose();
    await auth.close();
    await getIt.reset();
  });

  Future<void> settle(WidgetTester tester) async {
    for (var i = 0; i < 20; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }
  }

  /// The app as main.dart composes it, starting on a salon's profile in the home tab.
  Future<void> openSalon(WidgetTester tester) async {
    tester.view.physicalSize = const Size(360 * 3, 640 * 3);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    // Before the first frame, so the router starts there rather than on home (whose blocs this test does not need).
    router.go(Routes.providerDetail('p1'));
    await tester.pumpWidget(BlocProvider<AuthBloc>.value(
      value: auth,
      child: MaterialApp.router(theme: AppTheme.light, routerConfig: router),
    ));
    await settle(tester);
    expect(find.byType(ProviderDetailPage), findsOneWidget);
  }

  testWidgets('a notification tapped while booking opens the appointment instead of crashing', (tester) async {
    await openSalon(tester);
    unawaited(router.push(Routes.bookingFlow('p1')));
    await settle(tester);
    expect(find.byType(BookingFlowPage), findsOneWidget);

    AppRouter.open(router, Routes.appointmentDetail('b9'));
    await settle(tester);

    expect(tester.takeException(), isNull);
    expect(find.byType(AppointmentDetailPage), findsOneWidget);
    expect(router.routerDelegate.currentConfiguration.uri.path, '/appointments/b9');
  });

  testWidgets('a notification tapped on another tab opens the appointment with the way back kept', (tester) async {
    await openSalon(tester);

    AppRouter.open(router, Routes.appointmentDetail('b9'));
    await settle(tester);

    expect(tester.takeException(), isNull);
    expect(find.byType(AppointmentDetailPage), findsOneWidget);

    // Back returns to the salon the customer was looking at.
    router.pop();
    await settle(tester);
    expect(find.byType(ProviderDetailPage), findsOneWidget);
  });
}
