import 'dart:async';

import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/di/injection.dart';
import 'package:booksy_provider_app/core/widgets/app_error_state.dart';
import 'package:booksy_provider_app/core/widgets/app_loading.dart';
import 'package:booksy_provider_app/features/onboarding/data/datasources/geocoding_service.dart';
import 'package:booksy_provider_app/features/onboarding/data/datasources/location_api_service.dart';
import 'package:booksy_provider_app/features/onboarding/data/models/location_models.dart';
import 'package:booksy_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:booksy_provider_app/features/onboarding/presentation/steps/location_step.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

class _MockLocationApi extends Mock implements LocationApiService {}

class _MockGeocoding extends Mock implements GeocodingService {}

void main() {
  late _MockRepo repo;
  late _MockLocationApi locationApi;
  late _MockGeocoding geocoding;

  const cities = [
    CityOption(id: 12, name: 'تهران', provinceName: 'تهران'),
    CityOption(id: 21, name: 'کاشان', provinceName: 'اصفهان'),
  ];

  setUp(() async {
    repo = _MockRepo();
    locationApi = _MockLocationApi();
    geocoding = _MockGeocoding();
    when(() => locationApi.getAllCities()).thenAnswer((_) async => cities);
    // Selecting a city forward-geocodes to recenter the map; keep it inert here.
    when(() => geocoding.geocode(any())).thenAnswer((_) async => null);
    when(() => geocoding.reverseGeocode(any(), any()))
        .thenAnswer((_) async => null);

    // reset() is async — await it, or its continuation wipes the fresh
    // registrations a few microtasks into the test body.
    await getIt.reset();
    getIt.registerSingleton<LocationApiService>(locationApi);
    getIt.registerSingleton<GeocodingService>(geocoding);
  });

  tearDown(() async => getIt.reset());

  Future<OnboardingCubit> pumpStep(WidgetTester tester) async {
    when(() => repo.getDraft()).thenAnswer((_) async => const Right(null));
    final cubit = OnboardingCubit(repo);
    await cubit.init(phoneNumber: '09120000000');

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

  testWidgets('shows a city dropdown and a map, but no province field',
      (tester) async {
    await pumpStep(tester);

    expect(find.byKey(const Key('onboarding-city')), findsOneWidget);
    expect(find.byKey(const Key('onboarding-map')), findsOneWidget);
    // The province input field was removed (parity with the Vue app).
    expect(find.byKey(const Key('onboarding-province')), findsNothing);
  });

  testWidgets('selecting a city commits the city AND its derived province',
      (tester) async {
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

  testWidgets('city search matches across kaf variants (mobile-keyboard bug)',
      (tester) async {
    await pumpStep(tester);

    // Data stores Persian kaf (کاشان); type Arabic kaf (كاشان) as a phone
    // keyboard might. The normalized search must still surface the city.
    await tester.enterText(find.byKey(const Key('onboarding-city')), 'كاشان');
    await tester.pump();

    expect(find.text('کاشان (اصفهان)'), findsWidgets);
  });

  group('city list screen states (spec: feedback-states)', () {
    testWidgets('shows AppLoading while the city list is fetching',
        (tester) async {
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

    testWidgets('failure shows AppErrorState and retry reloads the list',
        (tester) async {
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

    testWidgets('loaded state renders the searchable city field',
        (tester) async {
      await pumpStep(tester);

      expect(find.byType(AppLoading), findsNothing);
      expect(find.byType(AppErrorState), findsNothing);
      expect(find.byKey(const Key('onboarding-city')), findsOneWidget);
    });
  });
}
