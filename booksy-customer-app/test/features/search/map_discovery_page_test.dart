import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/location/geocoding_service.dart';
import 'package:booksy_customer_app/core/location/location_service.dart';
import 'package:booksy_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:booksy_customer_app/features/search/domain/repositories/search_repository.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/map_discovery_cubit.dart';
import 'package:booksy_customer_app/features/search/presentation/pages/map_discovery_page.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/map_provider_card.dart';

/// Widget-level tests for the map + carousel discovery page.
///
/// The cubit's own business logic (geocoding, fallback centring, category enum
/// wiring, stale-result guards) is already covered exhaustively by
/// `map_discovery_cubit_test.dart`. This suite covers what only a widget test
/// can: that the page actually renders the map/carousel/chrome for each cubit
/// state, that user gestures reach the cubit with the right arguments, that
/// pin and card selection stay in sync, and that the layout survives RTL and a
/// 1.3x font scale.
///
/// The cubit is replaced by a stub subclass rather than a mock: every method
/// except [MapDiscoveryCubit.selectProvider] just records its arguments, so
/// wiring is asserted without ever touching the network. [selectProvider] is
/// left to the real implementation because the pin <-> card sync test needs
/// the state to genuinely change.

class _UnusedSearchRepository implements SearchRepository {
  @override
  dynamic noSuchMethod(Invocation invocation) =>
      throw UnimplementedError('${invocation.memberName} is not used in this test');
}

class _UnusedLocationService implements LocationService {
  @override
  Future<LocationResult> currentPosition() async =>
      throw UnimplementedError('location is not resolved in this test');
}

class _UnusedGeocodingService implements GeocodingService {
  @override
  Future<GeoCoordinates?> geocode(String term) async =>
      throw UnimplementedError('geocoding is not resolved in this test');
}

/// Every tile resolves to a 1x1 transparent PNG straight from memory, so the
/// widget tree never reaches the tile network.
class _FakeTileProvider extends TileProvider {
  @override
  ImageProvider getImage(TileCoordinates coordinates, TileLayer options) =>
      MemoryImage(TileProvider.transparentImage);
}

class _StubMapDiscoveryCubit extends MapDiscoveryCubit {
  int retryCalls = 0;
  int useMyLocationCalls = 0;
  int dismissNoticeCalls = 0;
  final List<String?> categoryCalls = [];
  String? lastSearchAreaTerm;
  double? lastVisibleLat;
  double? lastVisibleLng;
  double? lastVisibleRadius;

  _StubMapDiscoveryCubit(MapDiscoveryState initial)
      : super(
          repository: _UnusedSearchRepository(),
          locationService: _UnusedLocationService(),
          geocodingService: _UnusedGeocodingService(),
        ) {
    emit(initial);
  }

  @override
  Future<void> retry() async => retryCalls++;

  @override
  Future<void> selectCategory(String? category) async {
    categoryCalls.add(category);
  }

  @override
  Future<void> searchVisibleArea({
    required double latitude,
    required double longitude,
    required double radiusKm,
  }) async {
    lastVisibleLat = latitude;
    lastVisibleLng = longitude;
    lastVisibleRadius = radiusKm;
  }

  @override
  Future<void> searchArea(String term) async {
    lastSearchAreaTerm = term;
  }

  @override
  Future<void> useMyLocation() async => useMyLocationCalls++;

  @override
  void dismissNotice() => dismissNoticeCalls++;
}

ProviderSummary _provider(
  String id,
  String name, {
  double lat = MapDiscoveryCubit.fallbackLatitude,
  double lon = MapDiscoveryCubit.fallbackLongitude,
  double? distance,
  double rating = 0,
  int reviewCount = 0,
}) =>
    ProviderSummary(
      id: id,
      name: name,
      rating: rating,
      reviewCount: reviewCount,
      distance: distance,
      startingPrice: 0,
      isOpen: true,
      latitude: lat,
      longitude: lon,
    );

Widget _app({required MapDiscoveryCubit cubit, double textScale = 1.0}) {
  final router = GoRouter(
    initialLocation: '/explore/map',
    routes: [
      GoRoute(
        path: '/explore/map',
        builder: (context, state) => MapDiscoveryPage(
          cubit: cubit,
          tileProvider: _FakeTileProvider(),
        ),
      ),
      GoRoute(
        path: '/providers/:id',
        builder: (context, state) =>
            Scaffold(body: Text('provider-detail-${state.pathParameters['id']}')),
      ),
    ],
  );

  return MaterialApp.router(
    theme: AppTheme.light,
    routerConfig: router,
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(context).copyWith(textScaler: TextScaler.linear(textScale)),
      child: Directionality(textDirection: TextDirection.rtl, child: child!),
    ),
  );
}

void main() {
  // A generous, deterministic viewport: big enough that two providers a
  // couple of kilometres apart both stay inside the visible bounds (flutter_map
  // culls markers outside them) and far enough apart on screen at the default
  // zoom that they never fall into the same clustering cell.
  setUp(() {
    final binding = TestWidgetsFlutterBinding.ensureInitialized();
    binding.platformDispatcher.views.first.physicalSize = const Size(1080, 1920);
    binding.platformDispatcher.views.first.devicePixelRatio = 1.0;
    addTearDown(binding.platformDispatcher.views.first.resetPhysicalSize);
    addTearDown(binding.platformDispatcher.views.first.resetDevicePixelRatio);
  });

  const providerA = 'a';
  const providerB = 'b';

  MapDiscoveryState loadedState({
    List<ProviderSummary> providers = const [],
    String? selectedProviderId,
    MapNotice notice = MapNotice.none,
    String areaLabel = AppStrings.mapDefaultAreaLabel,
  }) =>
      MapDiscoveryState(
        status: providers.isEmpty
            ? MapDiscoveryStatus.empty
            : MapDiscoveryStatus.loaded,
        providers: providers,
        areaLabel: areaLabel,
        selectedProviderId: selectedProviderId,
        notice: notice,
      );

  group('rendering', () {
    testWidgets('renders the map, chrome and a carousel card per provider',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [
          _provider(providerA, 'سالن الف', distance: 0.4),
          _provider(providerB, 'سالن ب', lat: 39.665, lon: 47.935, distance: 1.9),
        ],
        selectedProviderId: providerA,
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));

      expect(find.text(AppStrings.mapTitle), findsOneWidget);
      expect(find.byKey(const Key('map-menu-button')), findsOneWidget);
      expect(find.byKey(const Key('map-canvas')), findsOneWidget);
      expect(find.byKey(const Key('map-area-search-field')), findsOneWidget);
      expect(find.text(AppStrings.mapDefaultAreaLabel), findsOneWidget);
      expect(find.byKey(const Key('map-provider-carousel')), findsOneWidget);
      expect(find.byType(MapProviderCard), findsNWidgets(2));
      expect(find.text('سالن الف'), findsOneWidget);
      expect(find.text('سالن ب'), findsOneWidget);
      expect(find.byKey(const Key('map-pin-$providerA')), findsOneWidget);
      expect(find.byKey(const Key('map-pin-$providerB')), findsOneWidget);
      expect(find.byKey(const Key('map-my-location-fab')), findsOneWidget);

      await tester.pump(const Duration(milliseconds: 400));
    });

    testWidgets('shows the empty panel when nothing is nearby', (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState());

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();

      expect(find.byKey(const Key('map-empty-panel')), findsOneWidget);
      expect(find.text(AppStrings.mapEmptyTitle), findsOneWidget);
      expect(find.text(AppStrings.mapEmptySubtitle), findsOneWidget);
      expect(find.byType(MapProviderCard), findsNothing);
    });

    testWidgets('shows the error panel and retries on tap', (tester) async {
      final cubit = _StubMapDiscoveryCubit(const MapDiscoveryState(
        status: MapDiscoveryStatus.error,
        errorMessage: 'قطع ارتباط',
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();

      expect(find.byKey(const Key('map-error-panel')), findsOneWidget);
      expect(find.text('قطع ارتباط'), findsOneWidget);

      await tester.tap(find.byKey(const Key('map-retry')));
      await tester.pump();

      expect(cubit.retryCalls, 1);
    });

    testWidgets(
        'falls back to the Parsabad centre and surfaces the permission notice',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [_provider(providerA, 'سالن الف')],
        notice: MapNotice.permissionDenied,
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();

      expect(cubit.state.latitude, MapDiscoveryCubit.fallbackLatitude);
      expect(cubit.state.longitude, MapDiscoveryCubit.fallbackLongitude);
      expect(find.byKey(const Key('map-notice')), findsOneWidget);
      expect(find.text(AppStrings.locationPermissionNeeded), findsOneWidget);
      // Never a blank screen: providers still show up behind the notice.
      expect(find.byType(MapProviderCard), findsOneWidget);

      await tester.tap(find.byKey(const Key('map-notice-dismiss')));
      await tester.pump();

      expect(cubit.dismissNoticeCalls, 1);
    });
  });

  group('category filter', () {
    testWidgets('passes the ServiceCategory ENUM NAME, never a Persian label',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [_provider(providerA, 'سالن الف')],
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();

      await tester.tap(find.byKey(const Key('map-category-Barbershop')));
      await tester.pump();

      expect(cubit.categoryCalls, contains('Barbershop'));
      expect(cubit.categoryCalls.last, isNot(contains('آرایش')));

      await tester.tap(find.byKey(const Key('map-category-all')));
      await tester.pump();

      expect(cubit.categoryCalls.last, isNull);
    });
  });

  group('city search', () {
    testWidgets('submitting the search field geocodes the typed area',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [_provider(providerA, 'سالن الف')],
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();

      await tester.enterText(
        find.byKey(const Key('map-area-search-field')),
        'اردبیل',
      );
      await tester.testTextInput.receiveAction(TextInputAction.search);
      await tester.pump();

      expect(cubit.lastSearchAreaTerm, 'اردبیل');
    });
  });

  group('search this area', () {
    testWidgets(
        'panning the map offers a re-search, and tapping it re-queries the new centre',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [_provider(providerA, 'سالن الف')],
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));

      expect(find.byKey(const Key('map-search-this-area')), findsNothing);

      // A real drag gesture on the map canvas — flutter_map reports it with
      // hasGesture=true, which is what makes the page offer a re-search.
      await tester.drag(
        find.byKey(const Key('map-canvas')),
        const Offset(-250, -180),
      );
      await tester.pump();
      // The offer only appears once the camera has been still for the debounce
      // window — never mid-gesture.
      expect(find.byKey(const Key('map-search-this-area')), findsNothing);

      await tester.pump(const Duration(milliseconds: 600));

      expect(find.byKey(const Key('map-search-this-area')), findsOneWidget);

      await tester.tap(find.byKey(const Key('map-search-this-area')));
      await tester.pump();

      expect(cubit.lastVisibleLat, isNotNull);
      expect(cubit.lastVisibleLng, isNotNull);
      expect(cubit.lastVisibleRadius, isNotNull);
      // The button is dismissed once the customer acts on it.
      expect(find.byKey(const Key('map-search-this-area')), findsNothing);
    });
  });

  group('pin <-> card selection sync', () {
    testWidgets('tapping a pin selects it and highlights the matching card',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [
          _provider(providerA, 'سالن الف'),
          _provider(providerB, 'سالن ب', lat: 39.665, lon: 47.935),
        ],
        selectedProviderId: providerA,
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));

      expect(
        tester.widget<MapProviderCard>(find.byKey(const Key('map-card-$providerB'))).selected,
        isFalse,
      );

      await tester.tap(find.byKey(const Key('map-pin-$providerB')));
      await tester.pumpAndSettle();

      expect(cubit.state.selectedProviderId, providerB);
      expect(
        tester.widget<MapProviderCard>(find.byKey(const Key('map-card-$providerB'))).selected,
        isTrue,
      );
      expect(
        tester.widget<MapProviderCard>(find.byKey(const Key('map-card-$providerA'))).selected,
        isFalse,
      );
    });

    testWidgets('tapping a card selects it (the same path a carousel scroll uses)',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [
          _provider(providerA, 'سالن الف'),
          _provider(providerB, 'سالن ب', lat: 39.665, lon: 47.935),
        ],
        selectedProviderId: providerA,
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));

      // Card b starts only partially on screen (RTL reverses the carousel's
      // paging order, and viewportFraction < 1 means neighbours peek in from
      // the edge) — bring it fully into view before tapping it, exactly as a
      // real swipe would.
      await tester.ensureVisible(find.byKey(const Key('map-card-$providerB')));
      await tester.pumpAndSettle();

      // Tap the card body (its title), not the "مشاهده پروفایل" CTA nested
      // inside it — the card's own onTap is what PageView.onPageChanged also
      // calls when a scroll lands on this page, so this exercises the same
      // selection path a carousel swipe would.
      await tester.tap(find.text('سالن ب'));
      await tester.pumpAndSettle();

      expect(cubit.state.selectedProviderId, providerB);
    });

    testWidgets('the "مشاهده پروفایل" button opens the provider profile',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [_provider(providerA, 'سالن الف')],
        selectedProviderId: providerA,
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));

      await tester.tap(find.byKey(const Key('map-card-profile-$providerA')));
      await tester.pumpAndSettle();

      expect(find.text('provider-detail-$providerA'), findsOneWidget);
    });
  });

  group('layout', () {
    testWidgets('the chrome renders right-to-left', (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [_provider(providerA, 'سالن الف')],
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();

      final direction = Directionality.of(
        tester.element(find.byKey(const Key('map-area-search-field'))),
      );
      expect(direction, TextDirection.rtl);

      // The map canvas itself is a geographic plane, not RTL chrome.
      final mapDirection = Directionality.of(
        tester.element(find.byKey(const Key('map-canvas'))),
      );
      expect(mapDirection, TextDirection.ltr);
    });

    testWidgets(
        'the locate-me button stays bottom-*right* regardless of RTL — a map-control convention, not chrome text',
        (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [_provider(providerA, 'سالن الف')],
      ));

      await tester.pumpWidget(_app(cubit: cubit));
      await tester.pump();

      // A plain RTL Row would put the last child (the FAB) on the physical
      // left — this pins the regression: the FAB's left edge must sit to the
      // right of the attribution text it shares a row with.
      final fabLeft = tester.getTopLeft(find.byKey(const Key('map-my-location-fab'))).dx;
      final attributionLeft =
          tester.getTopLeft(find.text('© ${AppStrings.mapAttribution}')).dx;
      expect(fabLeft, greaterThan(attributionLeft));
    });

    testWidgets('survives 1.3x text scale without overflow', (tester) async {
      final cubit = _StubMapDiscoveryCubit(loadedState(
        providers: [
          _provider(providerA, 'سالن آرایش و زیبایی مغان', distance: 12.7, rating: 4.8, reviewCount: 120),
          _provider(providerB, 'سالن ب', lat: 39.665, lon: 47.935),
        ],
      ));

      await tester.pumpWidget(_app(cubit: cubit, textScale: 1.3));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));

      expect(tester.takeException(), isNull);
    });
  });
}
