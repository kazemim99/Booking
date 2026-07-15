import 'dart:async';

import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/core/network/connectivity_service.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/home/domain/composition/home_widget_registry.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_enums.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_inputs.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_snapshot.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/home_cubit.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements HomeRepository {}

class _MockConnectivity extends Mock implements ConnectivityService {}

const _operationalSnapshot = HomeSnapshot(
  providerStatus: ProviderStatus.active,
  bookingMode: HomeBookingMode.request,
  availability: HomeAvailability.open,
  signals: MaturitySignals(
    profileComplete: true,
    totalBookingsAllTime: 50,
    bookingsTrailing30d: 12,
  ),
  todayApptCount: 4,
  hasUpcomingToday: true,
  pendingRequestCount: 2,
);

void main() {
  late _MockRepo repo;
  late _MockConnectivity connectivity;
  late StreamController<bool> connStream;

  setUp(() {
    repo = _MockRepo();
    connectivity = _MockConnectivity();
    connStream = StreamController<bool>.broadcast();
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => connectivity.onStatusChange)
        .thenAnswer((_) => connStream.stream);
  });

  tearDown(() => connStream.close());

  HomeCubit build() => HomeCubit(repo, connectivity);

  test('initial state is the LOADING context (skeleton screen)', () {
    final cubit = build();
    expect(cubit.state.system, SystemState.loading);
    expect(const HomeWidgetRegistry().compose(cubit.state), isEmpty);
  });

  test('load success resolves the operational context', () async {
    when(() => repo.fetchSnapshot())
        .thenAnswer((_) async => const Right(_operationalSnapshot));

    final cubit = build();
    await cubit.load();

    expect(cubit.state.system, SystemState.ok);
    expect(cubit.state.maturity, HomeMaturity.operational);
    expect(cubit.state.day, HomeDayContext.active);
    expect(cubit.state.pendingRequestCount, 2);
    expect(cubit.state.isStale, isFalse);
  });

  test('load failure with no cache resolves the ERROR context', () async {
    when(() => repo.fetchSnapshot())
        .thenAnswer((_) async => const Left(ServerFailure('down')));

    final cubit = build();
    await cubit.load();

    expect(cubit.state.system, SystemState.error);
    expect(const HomeWidgetRegistry().compose(cubit.state), isEmpty);
  });

  test('refresh failure after a success falls back to stale cache', () async {
    when(() => repo.fetchSnapshot())
        .thenAnswer((_) async => const Right(_operationalSnapshot));
    final cubit = build();
    await cubit.load();

    when(() => repo.fetchSnapshot())
        .thenAnswer((_) async => const Left(ServerFailure('down')));
    await cubit.refresh();

    // Body survives from cache; only staleness is signalled.
    expect(cubit.state.system, SystemState.ok);
    expect(cubit.state.day, HomeDayContext.active);
    expect(cubit.state.isStale, isTrue);
  });

  test('going offline flips to a stale offline context with the banner',
      () async {
    when(() => repo.fetchSnapshot())
        .thenAnswer((_) async => const Right(_operationalSnapshot));
    final cubit = build();
    await cubit.load();

    connStream.add(false);
    await Future<void>.delayed(Duration.zero);

    expect(cubit.state.system, SystemState.offline);
    expect(cubit.state.isStale, isTrue);
    expect(cubit.state.banners, contains(HomeBannerKind.offline));
    // Cached body still composes (spec §7 row 13/14).
    expect(cubit.state.day, HomeDayContext.active);
  });

  test('reconnecting refreshes and returns to a fresh OK context', () async {
    when(() => repo.fetchSnapshot())
        .thenAnswer((_) async => const Right(_operationalSnapshot));
    final cubit = build();
    await cubit.load();
    connStream.add(false);
    await Future<void>.delayed(Duration.zero);
    expect(cubit.state.system, SystemState.offline);

    connStream.add(true);
    await Future<void>.delayed(Duration.zero);

    expect(cubit.state.system, SystemState.ok);
    expect(cubit.state.isStale, isFalse);
    verify(() => repo.fetchSnapshot()).called(2); // load + reconnect sync
  });

  test('stale-result guard: a superseded refresh never overwrites the newer '
      'one', () async {
    final first = Completer<Either<Failure, HomeSnapshot>>();
    final answers = <Future<Either<Failure, HomeSnapshot>>>[
      first.future,
      Future.value(const Right(_operationalSnapshot)),
    ];
    when(() => repo.fetchSnapshot()).thenAnswer((_) => answers.removeAt(0));

    final cubit = build();
    final r1 = cubit.refresh(); // will complete LAST, with a failure
    final r2 = cubit.refresh(); // completes first, with fresh data
    await r2;
    first.complete(const Left(ServerFailure('slow, stale failure')));
    await r1;

    // The late failure from the superseded request must not mark things
    // failed/stale — the newer success wins.
    expect(cubit.state.system, SystemState.ok);
    expect(cubit.state.isStale, isFalse);
  });
}
