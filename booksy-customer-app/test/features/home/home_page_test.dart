import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/core/location/location_service.dart';
import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';
import 'package:booksy_customer_app/core/widgets/widgets.dart';
import 'package:booksy_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:booksy_customer_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_customer_app/features/home/domain/usecases/get_home_data.dart';
import 'package:booksy_customer_app/features/home/presentation/bloc/home_bloc.dart';
import 'package:booksy_customer_app/features/home/presentation/bloc/home_state.dart';
import 'package:booksy_customer_app/features/home/presentation/pages/home_page.dart';
import 'package:booksy_customer_app/features/home/presentation/widgets/featured_provider_card.dart';
import 'package:booksy_customer_app/features/home/presentation/widgets/home_category_row.dart';
import 'package:booksy_customer_app/features/home/presentation/widgets/nearby_provider_card.dart';
import 'package:booksy_customer_app/features/search/domain/repositories/search_repository.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/nearby_providers_cubit.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/service_categories.dart';

/// Widget tests for the redesigned home surface.
///
/// Home has to hold up on the data that actually exists today: providers with
/// no image, a zero rating, no review count, no starting price and no distance.
/// The rating and price-band elements must therefore be absent, not zeroed.

// ---------------------------------------------------------------- fakes

class _FakeGetHomeData implements GetHomeData {
  Either<Failure, HomeData> callResult = Right(_homeData());

  @override
  Future<Either<Failure, HomeData>> call() async => callResult;

  @override
  Future<Either<Failure, HomeData>> retrySection(
    HomeSection section,
    HomeData current,
  ) async =>
      callResult;

  @override
  HomeRepository get repository => throw UnimplementedError();

  @override
  SecureStorageService get storageService => throw UnimplementedError();
}

/// Emits a fixed state so the page never depends on async loading order.
class _StubHomeBloc extends HomeBloc {
  _StubHomeBloc(HomeState initial) : super(_FakeGetHomeData()) {
    emit(initial);
  }
}

class _UnusedLocationService implements LocationService {
  @override
  Future<LocationResult> currentPosition() async =>
      throw UnimplementedError('location is not resolved in tests');
}

/// `noSuchMethod` forwarding keeps this fake compiling across repository
/// signature changes.
class _UnusedSearchRepository implements SearchRepository {
  @override
  dynamic noSuchMethod(Invocation invocation) =>
      throw UnimplementedError('${invocation.memberName} is not used here');
}

class _StubNearbyCubit extends NearbyProvidersCubit {
  int loadCalls = 0;

  _StubNearbyCubit(NearbyState initial)
      : super(
          locationService: _UnusedLocationService(),
          repository: _UnusedSearchRepository(),
        ) {
    emit(initial);
  }

  @override
  Future<void> load() async {
    loadCalls++;
  }
}

// ---------------------------------------------------------------- fixtures

ProviderSummary _provider({
  String id = 'p1',
  String name = 'سالن زیبایی رز',
  String? imageUrl,
  double rating = 0,
  int reviewCount = 0,
  double? distance,
  int startingPrice = 0,
}) =>
    ProviderSummary(
      id: id,
      name: name,
      imageUrl: imageUrl,
      rating: rating,
      reviewCount: reviewCount,
      distance: distance,
      startingPrice: startingPrice,
      isOpen: true,
    );

HomeData _homeData({List<ProviderSummary> topProviders = const []}) => HomeData(
      categories: const [],
      upcomingBookings: const [],
      topProviders: topProviders,
      promotions: const [],
      recentlyVisitedProviders: const [],
      favoriteProviders: const [],
    );

// ---------------------------------------------------------------- harness

/// Records every location the router was asked to go to, so navigation can be
/// asserted without standing up the real app shell.
class _Nav {
  final List<String> visited = [];
}

Widget _app({
  required HomeBloc bloc,
  required NearbyProvidersCubit nearby,
  required _Nav nav,
  double textScale = 1.0,
}) {
  Widget stub(String label) => Scaffold(body: Text(label));

  final router = GoRouter(
    initialLocation: '/home',
    observers: [],
    redirect: (context, state) {
      nav.visited.add(state.uri.toString());
      return null;
    },
    routes: [
      GoRoute(
        path: '/home',
        builder: (context, state) => BlocProvider<HomeBloc>.value(
          value: bloc,
          child: HomePage(nearbyCubit: nearby),
        ),
      ),
      GoRoute(
        path: '/explore',
        builder: (context, state) => stub('explore'),
        routes: [
          // 'area' and 'nearby' were retired: both are now the single map destination.
          GoRoute(path: 'map', builder: (context, state) => stub('map')),
        ],
      ),
      GoRoute(
        path: '/providers/:id',
        builder: (context, state) => stub('provider-detail'),
        routes: [
          GoRoute(path: 'book', builder: (context, state) => stub('book')),
        ],
      ),
      GoRoute(
        path: '/appointments',
        builder: (context, state) => stub('appointments'),
      ),
      GoRoute(path: '/profile', builder: (context, state) => stub('profile')),
    ],
  );

  return MaterialApp.router(
    theme: AppTheme.light,
    routerConfig: router,
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(context)
          .copyWith(textScaler: TextScaler.linear(textScale)),
      child: Directionality(textDirection: TextDirection.rtl, child: child!),
    ),
  );
}

void main() {
  late _Nav nav;

  setUp(() => nav = _Nav());

  HomeBloc loadedBloc({List<ProviderSummary> topProviders = const []}) =>
      _StubHomeBloc(HomeLoaded.fromData(_homeData(topProviders: topProviders)));

  group('chrome', () {
    testWidgets('renders the app bar, search pill and category tiles',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(topProviders: [_provider()]),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.text(AppStrings.homeTitle), findsOneWidget);
      expect(find.byKey(const Key('home-menu-button')), findsOneWidget);
      expect(find.byKey(const Key('home-map-search-button')), findsOneWidget);
      expect(find.byKey(const Key('home-search-pill')), findsOneWidget);
      expect(find.text(AppStrings.homeSearchHint), findsOneWidget);

      // Five category tiles plus the "more" tile.
      expect(find.byType(HomeCategoryRow), findsOneWidget);
      for (final category
          in kServiceCategories.take(kHomeCategoryTileCount)) {
        expect(
          find.byKey(Key('home-category-${category.apiValue}')),
          findsOneWidget,
        );
      }
      expect(find.byKey(const Key('home-category-more')), findsOneWidget);
    });

    testWidgets('lays out right-to-left', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(topProviders: [_provider()]),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      final direction = Directionality.of(
        tester.element(find.byKey(const Key('home-search-pill'))),
      );
      expect(direction, TextDirection.rtl);
    });

    testWidgets('the map-search button opens the map', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      await tester.tap(find.byKey(const Key('home-map-search-button')));
      await tester.pumpAndSettle();

      expect(find.text('map'), findsOneWidget);
    });

    testWidgets('the search pill opens explore', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      await tester.tap(find.byKey(const Key('home-search-pill')));
      await tester.pumpAndSettle();

      expect(find.text('explore'), findsOneWidget);
    });

    testWidgets('a category tile deep-links to explore with its API value',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      await tester.tap(find.byKey(const Key('home-category-Barbershop')));
      await tester.pumpAndSettle();

      expect(find.text('explore'), findsOneWidget);
      // The Persian label must never reach the wire — only the enum name does.
      expect(nav.visited.last, '/explore?category=Barbershop');
    });

    testWidgets('the hamburger opens a menu of real destinations',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      await tester.tap(find.byKey(const Key('home-menu-button')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('home-menu-nearby')), findsOneWidget);
      await tester.tap(find.byKey(const Key('home-menu-nearby')));
      await tester.pumpAndSettle();

      expect(find.text('map'), findsOneWidget);
    });
  });

  group('featured rail', () {
    testWidgets('shows a card per provider with a book action', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(topProviders: [
          _provider(),
          _provider(id: 'p2', name: 'آرایشگاه آرش'),
        ]),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.text(AppStrings.topProvidersTitle), findsOneWidget);
      expect(find.byType(FeaturedProviderCard), findsNWidgets(2));
      expect(find.text('سالن زیبایی رز'), findsOneWidget);
      expect(find.text(AppStrings.bookNowShort), findsNWidgets(2));
    });

    testWidgets('hides rating and price band while the data is all zeros',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(topProviders: [_provider()]),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.byType(ProviderRating), findsNothing);
      expect(find.byType(PriceBandLabel), findsNothing);
      expect(find.textContaining('کیلومتر'), findsNothing);
      // No image either — the placeholder stands in.
      expect(find.byIcon(Icons.storefront_outlined), findsWidgets);
    });

    testWidgets('shows rating and distance once the data provides them',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(topProviders: [
          _provider(rating: 4.5, reviewCount: 8, distance: 1.2),
        ]),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.byType(ProviderRating), findsOneWidget);
      expect(find.text('۴.۵'), findsOneWidget);
      expect(find.text(AppStrings.distanceKmLabel('۱.۲')), findsOneWidget);
    });

    testWidgets('the book button starts the booking flow', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(topProviders: [_provider()]),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      await tester.tap(find.byKey(const Key('home-featured-book-p1')));
      await tester.pumpAndSettle();

      expect(find.text('book'), findsOneWidget);
    });

    testWidgets('the section disappears when there are no providers',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.text(AppStrings.topProvidersTitle), findsNothing);
      expect(find.byType(FeaturedProviderCard), findsNothing);
    });
  });

  group('nearest list', () {
    testWidgets('stacks wide cards with a view-profile action', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby: _StubNearbyCubit(NearbyState(
          status: NearbyStatus.loaded,
          providers: [_provider(), _provider(id: 'p2', name: 'اسپا آرام')],
        )),
        nav: nav,
      ));
      await tester.pump();

      expect(find.text(AppStrings.nearestTitle), findsOneWidget);
      expect(find.byType(NearbyProviderCard), findsNWidgets(2));
      expect(find.text(AppStrings.viewProfile), findsNWidgets(2));

      await tester.tap(find.byKey(const Key('home-nearby-profile-p1')));
      await tester.pumpAndSettle();
      expect(find.text('provider-detail'), findsOneWidget);
    });

    testWidgets('offers the map when location permission is denied',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby: _StubNearbyCubit(
          const NearbyState(status: NearbyStatus.permissionDenied),
        ),
        nav: nav,
      ));
      await tester.pump();

      expect(find.byKey(const Key('home-nearest-permission')), findsOneWidget);
      expect(find.text(AppStrings.locationPermissionNeeded), findsOneWidget);

      await tester.tap(find.byKey(const Key('home-nearest-area-cta')));
      await tester.pumpAndSettle();
      expect(find.text('map'), findsOneWidget);
    });

    testWidgets('shows a skeleton while resolving location', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(),
        nearby:
            _StubNearbyCubit(const NearbyState(status: NearbyStatus.loading)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.text(AppStrings.nearestTitle), findsOneWidget);
      expect(find.byType(SkeletonLoader), findsWidgets);
      expect(find.byType(NearbyProviderCard), findsNothing);
    });

    testWidgets('offers a retry when the nearby search failed', (tester) async {
      final nearby = _StubNearbyCubit(const NearbyState(
        status: NearbyStatus.error,
        errorMessage: 'boom',
      ));
      await tester.pumpWidget(
        _app(bloc: loadedBloc(), nearby: nearby, nav: nav),
      );
      await tester.pump();

      expect(find.text(AppStrings.sectionLoadFailed), findsOneWidget);
      await tester.tap(find.text(AppStrings.retry));
      await tester.pump();

      expect(nearby.loadCalls, 1);
    });
  });

  group('page-level states', () {
    testWidgets('renders a content-shaped skeleton on first load',
        (tester) async {
      await tester.pumpWidget(_app(
        bloc: _StubHomeBloc(const HomeLoading()),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.byType(SkeletonLoader), findsWidgets);
      // The chrome stays usable while the sections load.
      expect(find.byKey(const Key('home-search-pill')), findsOneWidget);
    });

    testWidgets('renders the error state with a retry', (tester) async {
      await tester.pumpWidget(_app(
        bloc: _StubHomeBloc(const HomeError('خطای شبکه')),
        nearby: _StubNearbyCubit(const NearbyState(status: NearbyStatus.empty)),
        nav: nav,
      ));
      await tester.pump();

      expect(find.text('خطای شبکه'), findsOneWidget);
      expect(find.text(AppStrings.retry), findsOneWidget);
    });

    testWidgets('survives 1.3x text scale without overflow', (tester) async {
      await tester.pumpWidget(_app(
        bloc: loadedBloc(topProviders: [_provider(rating: 4.5, distance: 2)]),
        nearby: _StubNearbyCubit(NearbyState(
          status: NearbyStatus.loaded,
          providers: [_provider(id: 'p9', name: 'سالن آرایش و زیبایی نگین')],
        )),
        nav: nav,
        textScale: 1.3,
      ));
      await tester.pump();

      expect(tester.takeException(), isNull);
    });
  });
}
