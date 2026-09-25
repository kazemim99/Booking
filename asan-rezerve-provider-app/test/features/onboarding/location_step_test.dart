import 'dart:async';

import 'package:asan_rezerve_provider_app/config/theme/app_tokens.dart';
import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/core/di/injection.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_error_state.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_loading.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/data/datasources/device_location_service.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/data/datasources/geocoding_service.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/data/datasources/location_api_service.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/data/models/location_models.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/steps/location_step.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

class _MockLocationApi extends Mock implements LocationApiService {}

class _MockGeocoding extends Mock implements GeocodingService {}

class _MockDeviceLocation extends Mock implements DeviceLocationService {}

void main() {
  late _MockRepo repo;
  late _MockLocationApi locationApi;
  late _MockGeocoding geocoding;
  late _MockDeviceLocation deviceLocation;

  const cities = [
    CityOption(id: 12, name: 'تهران', provinceName: 'تهران'),
    CityOption(id: 21, name: 'کاشان', provinceName: 'اصفهان'),
  ];

  setUp(() async {
    repo = _MockRepo();
    locationApi = _MockLocationApi();
    geocoding = _MockGeocoding();
    deviceLocation = _MockDeviceLocation();
    // Default: no device fix (permission denied / unavailable).
    when(() => deviceLocation.current()).thenAnswer((_) async => null);
    when(() => locationApi.getAllCities()).thenAnswer((_) async => cities);
    // Selecting a city forward-geocodes to recenter the map; keep it inert here.
    when(() => geocoding.geocode(any())).thenAnswer((_) async => null);
    when(
      () => geocoding.reverseGeocode(any(), any()),
    ).thenAnswer((_) async => null);

    // reset() is async — await it, or its continuation wipes the fresh
    // registrations a few microtasks into the test body.
    await getIt.reset();
    getIt.registerSingleton<LocationApiService>(locationApi);
    getIt.registerSingleton<GeocodingService>(geocoding);
    getIt.registerSingleton<DeviceLocationService>(deviceLocation);
  });

  tearDown(() async => getIt.reset());

  Future<OnboardingCubit> pumpStep(
    WidgetTester tester, {
    OnboardingAddress? address,
  }) async {
    when(() => repo.getDraft()).thenAnswer((_) async => const Right(null));
    final cubit = OnboardingCubit(repo);
    await cubit.init(phoneNumber: '09120000000');
    if (address != null) cubit.updateAddress(address);

    await tester.pumpWidget(
      MaterialApp(
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: BlocProvider.value(
            value: cubit,
            child: const Scaffold(body: LocationStep()),
          ),
        ),
      ),
    );
    // Let the city-load future resolve (do NOT pumpAndSettle — the map and
    // progress indicators never settle).
    await tester.pump();
    await tester.pump();
    return cubit;
  }

  group('device location default (S12)', () {
    const here = LatLng(39.6482, 47.9174); // Parsabad

    testWidgets(
      'no saved pin: pins the device location and fills the address',
      (tester) async {
        when(() => deviceLocation.current()).thenAnswer((_) async => here);
        when(
          () => geocoding.reverseGeocode(here.latitude, here.longitude),
        ).thenAnswer(
          (_) async => const ReverseGeocodeResult(
            formattedAddress: 'پارس‌آباد، خیابان امام',
          ),
        );

        final cubit = await pumpStep(tester);
        await tester.pump();

        final address = cubit.state.data.address;
        expect(address.latitude, here.latitude);
        expect(address.longitude, here.longitude);
        expect(address.addressLine1, 'پارس‌آباد، خیابان امام');
      },
    );

    testWidgets('no device fix: no pin, nothing committed (Iran-center view)', (
      tester,
    ) async {
      final cubit = await pumpStep(tester);
      await tester.pump();

      verify(() => deviceLocation.current()).called(1);
      expect(cubit.state.data.address.latitude, isNull);
      verifyNever(() => geocoding.reverseGeocode(any(), any()));
    });

    testWidgets('a saved pin wins: the device location is never requested', (
      tester,
    ) async {
      final cubit = await pumpStep(
        tester,
        address: const OnboardingAddress(
          addressLine1: 'نشانی ذخیره‌شده',
          latitude: 35.7,
          longitude: 51.4,
        ),
      );
      await tester.pump();

      verifyNever(() => deviceLocation.current());
      expect(cubit.state.data.address.latitude, 35.7);
      expect(cubit.state.data.address.addressLine1, 'نشانی ذخیره‌شده');
    });

    testWidgets('a typed address is never overwritten by the device fix', (
      tester,
    ) async {
      final fix = Completer<LatLng?>();
      when(() => deviceLocation.current()).thenAnswer((_) => fix.future);
      when(() => geocoding.reverseGeocode(any(), any())).thenAnswer(
        (_) async =>
            const ReverseGeocodeResult(formattedAddress: 'نشانی از نقشه'),
      );

      final cubit = await pumpStep(tester);
      await tester.enterText(
        find.byKey(const Key('onboarding-address-line1')),
        'نشانی دستی',
      );
      fix.complete(here);
      await tester.pump();
      await tester.pump();

      expect(cubit.state.data.address.latitude, here.latitude);
      expect(cubit.state.data.address.addressLine1, 'نشانی دستی');
    });
  });

  testWidgets('shows a city dropdown and a map, but no province field', (
    tester,
  ) async {
    await pumpStep(tester);

    expect(find.byKey(const Key('onboarding-city')), findsOneWidget);
    expect(find.byKey(const Key('onboarding-map')), findsOneWidget);
    // The province input field was removed (parity with the Vue app).
    expect(find.byKey(const Key('onboarding-province')), findsNothing);
  });

  testWidgets('selecting a city commits the city AND its derived province', (
    tester,
  ) async {
    final cubit = await pumpStep(tester);

    // Type to reveal the inline results list.
    await tester.enterText(find.byKey(const Key('onboarding-city')), 'کاشان');
    await tester.pump();
    // The results list renders directly under the field (not an overlay).
    expect(find.byKey(const Key('onboarding-city-results')), findsOneWidget);

    // Pick the matching city.
    await tester.tap(find.text('کاشان (اصفهان)').last);
    await tester.pump();

    final address = cubit.state.data.address;
    expect(address.city, 'کاشان');
    // Province is derived from the city — never typed by the user.
    expect(address.province, 'اصفهان');
  });

  testWidgets(
    'the city chevron rotates 180° when the inline list opens and closes',
    (tester) async {
      await pumpStep(tester);

      AnimatedRotation chevron() => tester.widget<AnimatedRotation>(
        find.byKey(const Key('onboarding-city-chevron')),
      );

      expect(chevron().turns, 0);

      await tester.enterText(find.byKey(const Key('onboarding-city')), 'کاشان');
      await tester.pump();
      expect(find.byKey(const Key('onboarding-city-results')), findsOneWidget);
      expect(chevron().turns, 0.5);
      expect(chevron().duration, AppMotion.fast);

      await tester.tap(find.text('کاشان (اصفهان)').last);
      await tester.pump();
      expect(find.byKey(const Key('onboarding-city-results')), findsNothing);
      expect(chevron().turns, 0);
    },
  );

  testWidgets('city search matches across kaf variants (mobile-keyboard bug)', (
    tester,
  ) async {
    await pumpStep(tester);

    // Data stores Persian kaf (کاشان); type Arabic kaf (كاشان) as a phone
    // keyboard might. The normalized search must still surface the city.
    await tester.enterText(find.byKey(const Key('onboarding-city')), 'كاشان');
    await tester.pump();

    expect(find.text('کاشان (اصفهان)'), findsWidgets);
  });

  group('city list screen states (spec: feedback-states)', () {
    testWidgets('shows AppLoading while the city list is fetching', (
      tester,
    ) async {
      // Hold the fetch open so the loading state stays visible.
      final gate = Completer<List<CityOption>>();
      when(() => locationApi.getAllCities()).thenAnswer((_) => gate.future);

      await pumpStep(tester);

      expect(find.byType(AppLoading), findsOneWidget);
      expect(find.text(AppStrings.citiesLoading), findsOneWidget);
      expect(find.byKey(const Key('onboarding-city')), findsNothing);

      gate.complete(cities);
      await tester.pump();
    });

    testWidgets('failure shows AppErrorState and retry reloads the list', (
      tester,
    ) async {
      var calls = 0;
      when(() => locationApi.getAllCities()).thenAnswer((_) async {
        calls++;
        if (calls == 1) throw Exception('network down');
        return cities;
      });

      await pumpStep(tester);

      // Failed state: error view with retry, no city field.
      expect(find.byType(AppErrorState), findsOneWidget);
      expect(find.text(AppStrings.cityLoadError), findsOneWidget);
      expect(find.byKey(const Key('onboarding-city')), findsNothing);

      // Retry re-triggers the fetch and lands in the loaded state.
      await tester.tap(find.byKey(const Key('app-error-retry')));
      await tester.pump();
      await tester.pump();

      expect(calls, 2);
      expect(find.byType(AppErrorState), findsNothing);
      expect(find.byKey(const Key('onboarding-city')), findsOneWidget);
    });

    testWidgets('loaded state renders the searchable city field', (
      tester,
    ) async {
      await pumpStep(tester);

      expect(find.byType(AppLoading), findsNothing);
      expect(find.byType(AppErrorState), findsNothing);
      expect(find.byKey(const Key('onboarding-city')), findsOneWidget);
    });
  });

  group('map zoom', () {
    testWidgets('opens one step closer than it used to', (tester) async {
      await pumpStep(tester);

      final map = tester.widget<FlutterMap>(
        find.byKey(const Key('onboarding-map')),
      );
      expect(map.options.initialZoom, MapZoom.country);
      expect(
        MapZoom.country,
        6,
        reason: 'was 5; the user asked for one step in',
      );
    });

    testWidgets('a ctrl+wheel zoom gesture reaches the map', (tester) async {
      // On web the browser turns ctrl+wheel into a scale (pinch) event, which
      // flutter_map does not act on — so ctrl+scroll did nothing on the live
      // site. The step handles that event itself.
      await pumpStep(tester);

      // Specifically the listener wrapping the map: a scroll view has one too.
      final listener = find.byWidgetPredicate(
        (w) =>
            w is Listener && w.onPointerSignal != null && w.child is FlutterMap,
      );
      expect(listener, findsOneWidget);
    });
  });

  group('MapZoom.afterScale', () {
    test('pinching out zooms in, pinching in zooms out', () {
      expect(MapZoom.afterScale(10, 2.0), 11, reason: 'doubling is one level');
      expect(MapZoom.afterScale(10, 0.5), 9);
      expect(MapZoom.afterScale(10, 1.0), 10, reason: 'no gesture, no change');
    });

    test('never leaves the range the map allows', () {
      expect(MapZoom.afterScale(MapZoom.max, 8), MapZoom.max);
      expect(MapZoom.afterScale(MapZoom.min, 0.01), MapZoom.min);
    });
  });
}
