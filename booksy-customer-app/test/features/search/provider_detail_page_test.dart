import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/widgets/widgets.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/provider_detail_cubit.dart';
import 'package:booksy_customer_app/features/search/presentation/pages/provider_detail_page.dart';

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
}

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

// ---------------------------------------------------------------- harness

Widget _app(
  _StubProviderDetailCubit cubit, {
  DateTime? now,
  double textScale = 1.0,
}) {
  final router = GoRouter(
    initialLocation: '/providers/p1',
    routes: [
      GoRoute(
        path: '/providers/:id',
        builder: (context, state) => ProviderDetailPage(
          providerId: state.pathParameters['id']!,
          cubit: cubit,
          now: now,
        ),
        routes: [
          GoRoute(
            path: 'book',
            builder: (context, state) => const Scaffold(
              body: Text('booking-flow'),
            ),
          ),
        ],
      ),
    ],
  );

  return MaterialApp.router(
    theme: AppTheme.light,
    routerConfig: router,
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(context)
          .copyWith(textScaler: TextScaler.linear(textScale)),
      // The app is Persian-first: every state must lay out right-to-left.
      child: Directionality(textDirection: TextDirection.rtl, child: child!),
    ),
  );
}

_StubProviderDetailCubit _loaded(ProviderDetail provider) =>
    _StubProviderDetailCubit(ProviderDetailState(
      status: ProviderDetailStatus.loaded,
      provider: provider,
    ));

void main() {
  group('full provider', () {
    testWidgets('renders every section of the design', (tester) async {
      await tester.pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      // Hero + heading.
      expect(find.byKey(const Key('provider-hero-image')), findsOneWidget);
      expect(find.text('سالن زیبایی رز'), findsWidgets);

      // Meta line: city (standing in for category), rating, derived price band.
      expect(find.text('پارس‌آباد'), findsOneWidget);
      expect(find.text('۴.۶'), findsOneWidget);
      expect(find.text(AppStrings.reviewCountLabel('۱۲')), findsOneWidget);
      expect(find.byType(PriceBandLabel), findsOneWidget);

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

    testWidgets('derives the price band from this provider\'s own services',
        (tester) async {
      await tester.pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
      await tester.pumpAndSettle();

      // Median of 250k / 850k lands in the middle band — never invented.
      final label = tester.widget<PriceBandLabel>(find.byType(PriceBandLabel));
      expect(label.band, PriceBand.mid);
    });

    testWidgets('lays out right-to-left', (tester) async {
      await tester.pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
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
      await tester.pumpWidget(_app(_loaded(_fullProvider()), now: _tuesdayNoon));
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

      // A zero rating reads as a bad salon, so nothing is shown at all.
      expect(find.byType(ProviderRating), findsNothing);
      expect(find.textContaining('۰.۰'), findsNothing);
      expect(find.byType(PriceBandLabel), findsNothing);
      expect(find.byType(ProviderMetaLine), findsNothing);
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
