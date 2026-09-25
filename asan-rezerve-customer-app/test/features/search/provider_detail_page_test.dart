import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/core/utils/jalali_formatter.dart';
import 'package:asan_rezerve_customer_app/core/utils/price_formatter.dart';
import 'package:asan_rezerve_customer_app/core/widgets/widgets.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:asan_rezerve_customer_app/features/home/domain/repositories/home_repository.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/bloc/provider_customer_cubit.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/bloc/provider_detail_cubit.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/pages/provider_detail_page.dart';
import 'package:asan_rezerve_customer_app/features/reviews/domain/entities/review.dart';

import '../../helpers/fake_auth_bloc.dart';

/// Widget tests for the redesigned provider profile.
///
/// The two cases that matter are the *full* provider and the *bare* one: seeded
/// providers have no image, a zero rating, no reviews and often no services, so
/// the page has to look right with almost nothing to show and must never
/// display a fabricated rating or price band.

// ---------------------------------------------------------------- fakes

/// Signature-agnostic stand-in: `noSuchMethod` forwarding means this fake keeps
/// compiling when [BookingRepository]'s methods change.
class _UnusedRepository implements BookingRepository {
  @override
  dynamic noSuchMethod(Invocation invocation) =>
      throw UnimplementedError('${invocation.memberName} is not used here');
}

class _StubProviderDetailCubit extends ProviderDetailCubit {
  int loadCalls = 0;

  _StubProviderDetailCubit(ProviderDetailState initial)
      : super(_UnusedRepository()) {
    emit(initial);
  }

  @override
  Future<void> load(String providerId) async {
    loadCalls++;
  }

  /// A later state of the same page (e.g. its reviews arriving).
  void show(ProviderDetailState next) => emit(next);
}

/// The customer's side of the salon (visit + favourite) on the wire.
class _FakeCustomerRepository implements HomeRepository {
  final visits = <String>[];
  final added = <String>[];
  final removed = <String>[];
  Set<String> favorites = {};
  Either<Failure, Unit> changeResult = const Right(unit);

  @override
  Future<Either<Failure, void>> recordProviderVisit(
      String customerId, String providerId,
      {String? viewSource}) async {
    visits.add(providerId);
    return const Right(null);
  }

  @override
  Future<Either<Failure, Set<String>>> getFavoriteProviderIds(
          String customerId) async =>
      Right(favorites);

  @override
  Future<Either<Failure, Unit>> addFavoriteProvider(
      String customerId, String providerId) async {
    added.add(providerId);
    return changeResult;
  }

  @override
  Future<Either<Failure, Unit>> removeFavoriteProvider(
      String customerId, String providerId) async {
    removed.add(providerId);
    return changeResult;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) =>
      throw UnimplementedError('${invocation.memberName} is not used here');
}

/// A customer cubit whose id follows the fake session: null for a guest.
ProviderCustomerCubit _customerCubit(
  _FakeCustomerRepository repo,
  FakeAuthBloc auth,
) =>
    ProviderCustomerCubit(
      providerId: 'p1',
      repository: repo,
      customerId: () async => auth.state is Authenticated ? 'c1' : null,
    );

// ---------------------------------------------------------------- fixtures

/// 2026-08-11 is a Tuesday.
final _tuesdayNoon = DateTime(2026, 8, 11, 12);

BusinessHour _tuesday({bool closed = false}) => BusinessHour(
      dayOfWeek: 'سه‌شنبه',
      openTime: closed ? null : '09:00',
      closeTime: closed ? null : '18:30',
      isClosed: closed,
    );

ProviderDetail _fullProvider() => ProviderDetail(
      id: 'p1',
      businessName: 'سالن زیبایی رز',
      description: 'سالنی با ۱۰ سال سابقه در پارس‌آباد.',
      city: 'پارس‌آباد',
      addressLine: 'خیابان امام، پلاک ۱۲',
      profileImageUrl: null,
      averageRating: 4.6,
      totalReviews: 12,
      businessHours: [
        _tuesday(),
        const BusinessHour(dayOfWeek: 'جمعه', isClosed: true),
      ],
      services: const [
        ServiceItem(
          id: 's1',
          name: 'کوتاهی مو',
          price: 250000,
          currency: 'تومان',
          durationMinutes: 45,
        ),
        ServiceItem(
          id: 's2',
          name: 'رنگ مو',
          price: 850000,
          currency: 'تومان',
          durationMinutes: 120,
        ),
      ],
      staff: const [],
    );

/// The realistic freshly-seeded provider: nothing but a name.
ProviderDetail _bareProvider() => const ProviderDetail(
      id: 'p2',
      businessName: 'آرایشگاه نمونه',
      averageRating: 0,
      totalReviews: 0,
      businessHours: [],
      services: [],
      staff: [],
    );

/// A salon the catalogue knows only by its pin: no city, no street address,
/// but coordinates and reviews (#12 of the 2026-09-23 review).
ProviderDetail _addresslessProvider() => const ProviderDetail(
      id: 'p3',
      businessName: 'سالن بی‌نشانی',
      averageRating: 4,
      totalReviews: 1,
      latitude: 39.643089,
      longitude: 47.897802,
      businessHours: [],
      services: [],
      staff: [],
    );

/// A salon with both a street address and a pin: the usual case.
ProviderDetail _locatedProvider() => const ProviderDetail(
      id: 'p5',
      businessName: 'سالن نهال',
      city: 'پارس‌آباد',
      addressLine: 'شهرک پناهی، کوچه بلور ۳',
      averageRating: 0,
      totalReviews: 0,
      latitude: 39.643089,
      longitude: 47.897802,
      businessHours: [],
      services: [],
      staff: [],
    );

/// An address the catalogue has not pinned yet.
ProviderDetail _unpinnedProvider() => const ProviderDetail(
      id: 'p6',
      businessName: 'سالن بی‌نقشه',
      city: 'پارس‌آباد',
      addressLine: 'خیابان امام، پلاک ۱۲',
      averageRating: 0,
      totalReviews: 0,
      businessHours: [],
      services: [],
      staff: [],
    );

/// The heading the map used to carry as a section of its own, under the same address again.
const _removedMapHeading = 'موقعیت روی نقشه';

const _oneReview = ProviderReviews(
  averageRating: 4,
  totalReviews: 1,
  items: [
    Review(id: 'r1', customerName: 'مریم', rating: 4, comment: 'خوب بود')
  ],
);

/// A price long enough to be cut off in a half-width cell.
ProviderDetail _expensiveProvider() => const ProviderDetail(
      id: 'p4',
      businessName: 'سالن گران',
      averageRating: 0,
      totalReviews: 0,
      businessHours: [],
      services: [
        ServiceItem(
          id: 's9',
          name: 'کراتین',
          price: 2500000,
          currency: 'تومان',
          durationMinutes: 180,
        ),
        ServiceItem(
          id: 's10',
          name: 'رنگ و مش',
          price: 12500000,
          currency: 'تومان',
          durationMinutes: 240,
        ),
      ],
      staff: [],
    );

// ---------------------------------------------------------------- harness

Widget _app(
  _StubProviderDetailCubit cubit, {
  DateTime? now,
  double textScale = 1.0,
  FakeAuthBloc? auth,
  _FakeCustomerRepository? customerRepository,
}) {
  final session = auth ?? FakeAuthBloc();
  final customers = customerRepository ?? _FakeCustomerRepository();
  final router = GoRouter(
    initialLocation: '/providers/p1',
    routes: [
      GoRoute(
        path: '/login',
        builder: (context, state) => Scaffold(
          body: Text('login redirect=${state.uri.queryParameters['redirect']}'),
        ),
      ),
      GoRoute(
        path: '/providers/:id',
        builder: (context, state) => ProviderDetailPage(
          providerId: state.pathParameters['id']!,
          cubit: cubit,
          customerCubit: _customerCubit(customers, session),
          now: now,
        ),
        routes: [
          GoRoute(
            path: 'book',
            builder: (context, state) => Scaffold(
              body: Column(
                children: [
                  const Text('booking-flow'),
                  Text('service=${state.uri.queryParameters['service'] ?? ''}'),
                ],
              ),
            ),
          ),
        ],
      ),
    ],
  );

  return BlocProvider<AuthBloc>.value(
    value: session,
    child: MaterialApp.router(
      theme: AppTheme.light,
      routerConfig: router,
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context)
            .copyWith(textScaler: TextScaler.linear(textScale)),
        // The app is Persian-first: every state must lay out right-to-left.
        child: Directionality(textDirection: TextDirection.rtl, child: child!),
      ),
    ),
  );
}

_StubProviderDetailCubit _loaded(ProviderDetail provider,
        {ProviderReviews? reviews}) =>
    _StubProviderDetailCubit(ProviderDetailState(
      status: ProviderDetailStatus.loaded,
      provider: provider,
      reviews: reviews,
    ));

/// [key] inside the «تماس و موقعیت» section.
Finder _inContact(Key key) => find.descendant(
      of: find.byKey(const Key('provider-contact-location')),
      matching: find.byKey(key),
    );

/// A phone-sized surface, so layout findings match what a customer sees.
void _phone(WidgetTester tester, {double width = 360, double height = 640}) {
  tester.view.physicalSize = Size(width, height);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
}

void main() {
  group('full provider', () {
    testWidgets('renders every section of the design', (tester) async {
      await tester
          .pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      // Hero + heading.
      expect(find.byKey(const Key('provider-hero-image')), findsOneWidget);
      expect(find.text('سالن زیبایی رز'), findsWidgets);

      // Meta line: city (standing in for category), rating, starting price.
      expect(find.text('پارس‌آباد'), findsOneWidget);
      expect(find.text('۴.۶'), findsOneWidget);
      expect(find.text(AppStrings.reviewCountLabel('۱۲')), findsOneWidget);
      expect(find.byKey(const Key('provider-starting-price')), findsOneWidget);

      // Working hours with the open-now pill and a closed day.
      expect(find.text(AppStrings.workingHoursTitle), findsOneWidget);
      expect(find.byKey(const Key('provider-open-now-badge')), findsOneWidget);
      expect(find.text(AppStrings.closedDay), findsOneWidget);

      // Services, about, contact & location.
      expect(find.text(AppStrings.servicesTitle), findsOneWidget);
      expect(find.text('کوتاهی مو'), findsOneWidget);
      expect(find.text('رنگ مو'), findsOneWidget);
      expect(find.text(AppStrings.aboutTitle), findsOneWidget);
      expect(find.text(AppStrings.contactAndLocationTitle), findsOneWidget);
      expect(find.byKey(const Key('provider-address-row')), findsOneWidget);

      // Pinned CTA.
      expect(find.byKey(const Key('provider-book-cta')), findsOneWidget);
    });

    // customer-app-ux-review-fixes F.1: the `$$` band read as dollars in a
    // Toman app; the header now says what the cheapest service costs.
    testWidgets('shows its cheapest priced service as «از … تومان»',
        (tester) async {
      await tester
          .pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      expect(find.text(PriceFormatter.formatFrom(250000)), findsOneWidget);
      expect(find.textContaining(r'$'), findsNothing);
    });

    testWidgets('lays out right-to-left', (tester) async {
      await tester
          .pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      final direction = Directionality.of(
        tester.element(find.byKey(const Key('provider-book-cta'))),
      );
      expect(direction, TextDirection.rtl);
    });

    testWidgets('survives 1.3x text scale without overflow', (tester) async {
      await tester.pumpWidget(
        _app(_loaded(_fullProvider()), now: _tuesdayNoon, textScale: 1.3),
      );
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
    });

    testWidgets('the book CTA navigates into the booking flow', (tester) async {
      await tester
          .pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('provider-book-cta')));
      await tester.pumpAndSettle();

      expect(find.text('booking-flow'), findsOneWidget);
    });
  });

  group('provider with no data yet', () {
    testWidgets('shows no image, no rating and no price band', (tester) async {
      await tester.pumpWidget(_app(_loaded(_bareProvider())));
      await tester.pumpAndSettle();

      // Placeholder instead of a broken image.
      expect(find.byKey(const Key('provider-hero-image')), findsOneWidget);
      expect(find.byIcon(Icons.storefront_outlined), findsOneWidget);

      // A zero rating reads as a bad salon, so no star is shown — and since
      // provider-reviews-and-ratings the header says "no reviews yet" in words
      // instead of vanishing (the count is real now, so zero means zero).
      expect(find.byType(ProviderRating), findsNothing);
      expect(find.textContaining('۰.۰'), findsNothing);
      expect(find.byKey(const Key('provider-starting-price')), findsNothing);
      expect(find.byKey(const Key('provider-no-reviews')), findsOneWidget);
    });

    testWidgets('hides the hours, about and contact sections entirely',
        (tester) async {
      await tester.pumpWidget(_app(_loaded(_bareProvider())));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.workingHoursTitle), findsNothing);
      expect(find.text(AppStrings.aboutTitle), findsNothing);
      expect(find.text(AppStrings.contactAndLocationTitle), findsNothing);
      expect(find.byKey(const Key('provider-address-row')), findsNothing);
      // The phone row has no data source yet and must never be faked.
      expect(find.byKey(const Key('provider-phone-row')), findsNothing);
    });

    testWidgets('says so when there are no services, and still offers booking',
        (tester) async {
      await tester.pumpWidget(_app(_loaded(_bareProvider())));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.servicesTitle), findsOneWidget);
      expect(
        find.byKey(const Key('provider-services-empty')),
        findsOneWidget,
      );

      final cta = find.byKey(const Key('provider-book-cta'));
      expect(cta, findsOneWidget);
      await tester.tap(cta);
      await tester.pumpAndSettle();
      expect(find.text('booking-flow'), findsOneWidget);
    });
  });

  group('services open the booking flow (C.2)', () {
    testWidgets('tapping a service starts booking with that service chosen',
        (tester) async {
      await tester
          .pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      final service = find.byKey(const Key('provider-service-s2'));
      await tester.ensureVisible(service);
      await tester.pumpAndSettle();
      await tester.tap(service);
      await tester.pumpAndSettle();

      expect(find.text('booking-flow'), findsOneWidget);
      expect(find.text('service=s2'), findsOneWidget);
    });

    testWidgets('each service is a named button of at least 48 dp',
        (tester) async {
      final semantics = tester.ensureSemantics();
      await tester
          .pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      final cell = find.byKey(const Key('provider-service-s1'));
      expect(tester.getSize(cell).height, greaterThanOrEqualTo(48));
      expect(tester.getSize(cell).width, greaterThanOrEqualTo(48));

      final node = tester.getSemantics(cell);
      expect(node, isSemantics(isButton: true, hasTapAction: true));
      expect(node.label, contains('کوتاهی مو'));
      expect(node.label, contains(PriceFormatter.format(250000)));
      expect(node.label, contains(JalaliFormatter.toPersianDigits('45')));
      expect(node.label, contains(AppStrings.serviceBookAction));
      // The affordance is visible too, not only announced.
      expect(
        find.descendant(
            of: cell, matching: find.text(AppStrings.serviceBookAction)),
        findsOneWidget,
      );
      semantics.dispose();
    });
  });

  group('salon without an address (C.1)', () {
    testWidgets('still shows its reviews and its map', (tester) async {
      await tester.pumpWidget(
        _app(_loaded(_addresslessProvider(), reviews: _oneReview)),
      );
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('provider-reviews-section')), findsOneWidget);
      expect(find.byKey(const Key('review-r1')), findsOneWidget);
      // QA 2026-09-23 #7 changed where the map lives: it is part of «تماس و موقعیت» now, so a salon known only by
      // its pin gets that section (heading + map), just without an address row. It used to stay hidden here
      // because the map was a section of its own.
      expect(find.text(AppStrings.contactAndLocationTitle), findsOneWidget);
      expect(_inContact(const Key('provider-location-card')), findsOneWidget);
      expect(find.byKey(const Key('provider-address-row')), findsNothing);
    });
  });

  // QA recording 2026-09-23 #10: guest or signed in, the profile says where a review is written.
  group('where to leave a review', () {
    testWidgets('a guest is told', (tester) async {
      await tester.pumpWidget(_app(_loaded(_fullProvider(), reviews: _oneReview), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.reviewsHowToWrite), findsOneWidget);
    });

    testWidgets('a signed-in customer is told', (tester) async {
      await tester.pumpWidget(_app(_loaded(_fullProvider(), reviews: _oneReview),
          now: _tuesdayNoon, auth: FakeAuthBloc()..signIn()));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.reviewsHowToWrite), findsOneWidget);
    });
  });

  // QA recording 2026-09-23 #7: «تماس و موقعیت» showed the address, and «موقعیت روی نقشه» further down repeated
  // it above the map. One section now: address row, map, directions.
  group('one «تماس و موقعیت» section', () {
    testWidgets('holds the address once, then the map and directions',
        (tester) async {
      await tester.pumpWidget(_app(_loaded(_locatedProvider())));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.contactAndLocationTitle), findsOneWidget);
      expect(find.text(_removedMapHeading), findsNothing);
      expect(find.textContaining('شهرک پناهی، کوچه بلور ۳'), findsOneWidget,
          reason: 'the address is said once');

      final addressRow = _inContact(const Key('provider-address-row'));
      final map = _inContact(const Key('provider-location-card'));
      final directions = _inContact(const Key('provider-directions'));
      expect(addressRow, findsOneWidget);
      expect(map, findsOneWidget);
      expect(directions, findsOneWidget);
      // Address, then map, then directions, down the page.
      expect(tester.getTopLeft(addressRow).dy,
          lessThan(tester.getTopLeft(map).dy));
      expect(tester.getTopLeft(map).dy,
          lessThan(tester.getTopLeft(directions).dy));
    });

    testWidgets('an address without a pin is the section without a map',
        (tester) async {
      await tester.pumpWidget(_app(_loaded(_unpinnedProvider())));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.contactAndLocationTitle), findsOneWidget);
      expect(_inContact(const Key('provider-address-row')), findsOneWidget);
      expect(find.byKey(const Key('provider-location-card')), findsNothing);
      expect(find.byKey(const Key('provider-directions')), findsNothing);
    });

    testWidgets('neither an address nor a pin: no section at all',
        (tester) async {
      await tester.pumpWidget(_app(_loaded(_bareProvider())));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('provider-contact-location')), findsNothing);
      expect(find.byKey(const Key('provider-location-card')), findsNothing);
    });

    for (final scale in [1.0, 1.3]) {
      testWidgets('fits a 360x640 phone at ${scale}x text', (tester) async {
        _phone(tester);
        await tester.pumpWidget(
          _app(_loaded(_locatedProvider()), textScale: scale),
        );
        await tester.pumpAndSettle();
        await tester.ensureVisible(find.byKey(const Key('provider-directions')));
        await tester.pumpAndSettle();

        expect(tester.takeException(), isNull);
        expect(
          tester.getSize(find.byKey(const Key('provider-directions'))).height,
          greaterThanOrEqualTo(48),
        );
      });
    }
  });

  group('service prices (C.3)', () {
    for (final scale in [1.0, 1.3]) {
      testWidgets('are never cut off at 360 px and ${scale}x text',
          (tester) async {
        _phone(tester);
        await tester.pumpWidget(
          _app(_loaded(_expensiveProvider()), textScale: scale),
        );
        await tester.pumpAndSettle();

        expect(tester.takeException(), isNull);
        for (final amount in [2500000, 12500000]) {
          final price = find.text(PriceFormatter.format(amount));
          expect(price, findsOneWidget);
          final paragraph = tester.renderObject<RenderParagraph>(price);
          expect(paragraph.didExceedMaxLines, isFalse,
              reason: 'the currency must never be lost to an ellipsis');
        }
      });
    }
  });

  group('the signed-in customer (C.4, C.5)', () {
    Finder heart() => find.byKey(const Key('provider-favorite-toggle'));

    testWidgets(
        'a guest visit is not recorded; the heart sends them to '
        'sign in and back here', (tester) async {
      final repo = _FakeCustomerRepository();
      await tester
          .pumpWidget(_app(_loaded(_fullProvider()), customerRepository: repo));
      await tester.pumpAndSettle();

      expect(repo.visits, isEmpty);
      await tester.tap(heart());
      await tester.pumpAndSettle();

      expect(find.text('login redirect=/providers/p1'), findsOneWidget);
      expect(repo.added, isEmpty);
    });

    testWidgets('a signed-in customer visit is recorded once', (tester) async {
      final auth = FakeAuthBloc()..signIn();
      final repo = _FakeCustomerRepository();
      final detail = _loaded(_fullProvider());
      await tester
          .pumpWidget(_app(detail, auth: auth, customerRepository: repo));
      await tester.pumpAndSettle();
      // The page rebuilding (its reviews arriving) is not a second opening.
      detail.show(ProviderDetailState(
        status: ProviderDetailStatus.loaded,
        provider: _fullProvider(),
        reviews: _oneReview,
      ));
      await tester.pumpAndSettle();

      expect(repo.visits, ['p1']);
    });

    testWidgets(
        'signing in while the page is open records the visit and '
        'loads the heart', (tester) async {
      final auth = FakeAuthBloc();
      final repo = _FakeCustomerRepository()..favorites = {'p1'};
      await tester.pumpWidget(
          _app(_loaded(_fullProvider()), auth: auth, customerRepository: repo));
      await tester.pumpAndSettle();
      expect(repo.visits, isEmpty);

      auth.signIn();
      await tester.pumpAndSettle();

      expect(repo.visits, ['p1']);
      expect(find.byIcon(Icons.favorite), findsOneWidget);
    });

    testWidgets('the heart is a white, labelled, 48 dp button on the bar',
        (tester) async {
      final semantics = tester.ensureSemantics();
      await tester.pumpWidget(
          _app(_loaded(_fullProvider()), auth: FakeAuthBloc()..signIn()));
      await tester.pumpAndSettle();

      final size = tester.getSize(heart());
      expect(size.width, greaterThanOrEqualTo(48));
      expect(size.height, greaterThanOrEqualTo(48));
      expect(
        tester.getSemantics(heart()),
        isSemantics(
            label: AppStrings.favoriteAdd, isButton: true, hasTapAction: true),
      );
      final icon = tester.widget<Icon>(
          find.descendant(of: heart(), matching: find.byType(Icon)));
      expect(icon.color, Colors.white);
      semantics.dispose();
    });

    testWidgets('a favourite starts filled, and a tap removes it',
        (tester) async {
      final repo = _FakeCustomerRepository()..favorites = {'p1'};
      await tester.pumpWidget(_app(_loaded(_fullProvider()),
          auth: FakeAuthBloc()..signIn(), customerRepository: repo));
      await tester.pumpAndSettle();

      expect(find.byIcon(Icons.favorite), findsOneWidget);
      await tester.tap(heart());
      await tester.pumpAndSettle();

      expect(repo.removed, ['p1']);
      expect(find.byIcon(Icons.favorite_border), findsOneWidget);
    });

    testWidgets('a refused add goes back to empty and says so', (tester) async {
      final repo = _FakeCustomerRepository()
        ..changeResult = const Left(ServerFailure('boom'));
      await tester.pumpWidget(_app(_loaded(_fullProvider()),
          auth: FakeAuthBloc()..signIn(), customerRepository: repo));
      await tester.pumpAndSettle();

      await tester.tap(heart());
      await tester.pumpAndSettle();

      expect(repo.added, ['p1']);
      expect(find.byIcon(Icons.favorite_border), findsOneWidget);
      expect(find.text(AppStrings.favoriteAddFailed), findsOneWidget);
    });

    testWidgets(
        'logging out empties the heart, and the next customer to sign in '
        'here has a visit of their own', (tester) async {
      // /providers/:id stays mounted in the home tab's stack across a logout,
      // which ends in LoggedOut (not Unauthenticated).
      final auth = FakeAuthBloc()..signIn();
      final repo = _FakeCustomerRepository()..favorites = {'p1'};
      await tester.pumpWidget(
          _app(_loaded(_fullProvider()), auth: auth, customerRepository: repo));
      await tester.pumpAndSettle();
      expect(find.byIcon(Icons.favorite), findsOneWidget);
      expect(repo.visits, ['p1']);

      auth.add(const LogoutEvent());
      await tester.pumpAndSettle();

      expect(auth.state, isA<LoggedOut>());
      expect(find.byIcon(Icons.favorite), findsNothing);
      expect(find.byIcon(Icons.favorite_border), findsOneWidget);

      repo.favorites = {};
      auth.signIn();
      await tester.pumpAndSettle();

      expect(repo.visits, ['p1', 'p1']);
      expect(find.byIcon(Icons.favorite_border), findsOneWidget);
    });
  });

  group('async states', () {
    testWidgets('loading shows the content-shaped skeleton and no CTA',
        (tester) async {
      final cubit = _StubProviderDetailCubit(const ProviderDetailState());
      await tester.pumpWidget(_app(cubit));
      await tester.pump();

      expect(find.byType(SkeletonLoader), findsWidgets);
      expect(find.byKey(const Key('provider-book-cta')), findsNothing);
    });

    testWidgets('error offers a retry that reloads the provider',
        (tester) async {
      final cubit = _StubProviderDetailCubit(const ProviderDetailState(
        status: ProviderDetailStatus.error,
        errorMessage: AppStrings.providerNotFound,
      ));
      await tester.pumpWidget(_app(cubit));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.providerNotFound), findsOneWidget);
      await tester.tap(find.text(AppStrings.retry));
      await tester.pump();

      expect(cubit.loadCalls, 1);
    });
  });
}
