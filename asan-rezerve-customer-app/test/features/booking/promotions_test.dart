import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/utils/jalali_formatter.dart';
import 'package:asan_rezerve_customer_app/core/utils/price_formatter.dart';
import 'package:asan_rezerve_customer_app/features/booking/data/datasources/booking_remote_datasource.dart';
import 'package:asan_rezerve_customer_app/features/booking/data/repositories/booking_repository_impl.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/entities/promotion_entities.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:asan_rezerve_customer_app/features/booking/presentation/bloc/price_quote_cubit.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/widgets/services_grid.dart';

/// Discounts in the customer app (openspec/changes/add-discounts-and-campaigns): what an offer promises on the salon
/// page, the quote cubit, and the wire — the quote request, the booking's code and a lost discount told apart from
/// a taken slot.

class _Adapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  Map<String, dynamic> body = const {};
  int statusCode = 200;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    requests.add(options);
    return ResponseBody.fromString(jsonEncode(body), statusCode, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

PublicOffer _offer({
  bool percent = true,
  double value = 20,
  double? cap,
  double? minimum,
  bool newCustomers = false,
  List<String> services = const [],
  List<int> days = const [],
  String? from,
  String? to,
}) =>
    PublicOffer(
      id: 'o1',
      title: 'تخفیف پاییزه',
      isPercentage: percent,
      value: value,
      maxDiscountAmount: cap,
      minimumSubtotal: minimum,
      newCustomersOnly: newCustomers,
      serviceIds: services,
      daysOfWeek: days,
      dailyStartTime: from,
      dailyEndTime: to,
    );

void main() {
  group('PublicOffer mirrors the server arithmetic', () {
    test('a capped percentage, a 90% ceiling and whole Toman', () {
      expect(_offer().discountFor(200000), 40000);
      expect(_offer(value: 30, cap: 100000).discountFor(1000000), 100000);
      expect(_offer(percent: false, value: 500000).discountFor(300000), 270000);
      expect(_offer(value: 15).discountFor(33333), 4999);
    });

    test('reads the wire', () {
      final offer = PublicOffer.fromJson(const {
        'id': 'o9',
        'title': 'یلدا',
        'discountKind': 'FixedAmount',
        'discountValue': 50000,
        'newCustomersOnly': true,
        'serviceIds': ['s1'],
        'daysOfWeek': [6, 0],
        'dailyStartTime': '10:00',
        'dailyEndTime': '13:00',
      });

      expect(offer.isPercentage, isFalse);
      expect(offer.serviceIds, ['s1']);
      expect(offer.isUnconditional, isFalse);
      expect(offer.condition, 'برای اولین نوبت · شنبه، یکشنبه · ساعت ۱۰:۰۰ تا ۱۳:۰۰');
    });
  });

  group('offerForService', () {
    test('an unconditional offer gives a price that is a fact', () {
      final result = offerForService('s1', 200000, [_offer()]);

      expect(result!.discount, 40000);
      expect(result.discountedPrice, 160000);
    });

    test('a price that depends on the day or the customer is never promised', () {
      expect(offerForService('s1', 200000, [_offer(days: [6])])!.discountedPrice, isNull);
      expect(offerForService('s1', 200000, [_offer(newCustomers: true)])!.discountedPrice, isNull);
    });

    test('other services, unmet minimums and nothing to take off are skipped', () {
      expect(offerForService('s1', 200000, [_offer(services: ['s2'])]), isNull);
      expect(offerForService('s1', 200000, [_offer(minimum: 300000)]), isNull);
      expect(offerForService('s1', 40, [_offer(value: 1)]), isNull);
    });
  });

  group('PriceQuote', () {
    test('reads the server quote', () {
      final quote = PriceQuote.fromJson(const {
        'subtotal': 250000,
        'discount': 50000,
        'total': 200000,
        'appliedDiscount': {'title': 'تخفیف پاییزه', 'code': 'AUTUMN'},
        'codeOutcome': 'BetterOfferApplied',
        'codeMessage': 'تخفیف بهتری روی این نوبت اعمال شده است.',
      });

      expect(quote.total, 200000);
      expect(quote.discountTitle, 'تخفیف پاییزه');
      expect(quote.codeOutcome, CodeOutcome.betterOfferApplied);
      expect(quote.hasDiscount, isTrue);
    });
  });

  group('the wire', () {
    late _Adapter adapter;
    late BookingRemoteDataSource source;
    late BookingRepositoryImpl repository;

    setUp(() {
      adapter = _Adapter();
      final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;
      source = BookingRemoteDataSource(serviceCatalogDio: dio);
      repository = BookingRemoteDataSourceBacked.of(source);
    });

    test('the quote asks for the visit on the salon clock with a trimmed code', () async {
      adapter.body = {
        'data': {'subtotal': 100, 'discount': 0, 'total': 100, 'codeOutcome': 'None'}
      };

      await repository.quote(
        providerId: 'p1',
        serviceIds: const ['s1', 's2'],
        startTime: DateTime(2026, 10, 3, 11),
        promotionCode: '  loyal ',
      );

      final sent = adapter.requests.single;
      expect(sent.path, '/v1/Bookings/quote');
      expect((sent.data as Map)['serviceIds'], ['s1', 's2']);
      expect((sent.data as Map)['promotionCode'], 'loyal');
      expect((sent.data as Map)['startTime'], startsWith('2026-10-03T11:00:00'));
    });

    test('a booking carries the accepted code, and none when there is none', () async {
      adapter
        ..statusCode = 201
        ..body = {'id': 'b1'};

      await source.createBooking(
          providerId: 'p1', serviceId: 's1', staffProviderId: 'st1', startTime: DateTime(2026, 10, 3, 11), promotionCode: 'LOYAL');
      await source.createBooking(providerId: 'p1', serviceId: 's1', staffProviderId: 'st1', startTime: DateTime(2026, 10, 3, 11));

      expect((adapter.requests.first.data as Map)['promotionCode'], 'LOYAL');
      expect((adapter.requests.last.data as Map).containsKey('promotionCode'), isFalse);
    });

    test('a discount that went is not a taken slot', () async {
      adapter
        ..statusCode = 409
        ..body = {
          'success': false,
          'message': 'ظرفیت این تخفیف همین حالا تکمیل شد',
          'error': {'code': 'PROMOTION_UNAVAILABLE', 'message': 'ظرفیت این تخفیف همین حالا تکمیل شد'},
        };

      final result = await repository.createBooking(
          providerId: 'p1', serviceId: 's1', staffProviderId: 'st1', startTime: DateTime(2026, 10, 3, 11));

      final failure = result.fold((f) => f, (_) => null);
      expect(failure, isA<PromotionUnavailableFailure>());
      expect(failure!.message, 'ظرفیت این تخفیف همین حالا تکمیل شد');
    });

    test('a taken slot is still a taken slot', () async {
      adapter
        ..statusCode = 409
        ..body = {'success': false, 'message': 'x', 'error': {'code': 'RESOURCE_CONFLICT'}};

      final result = await repository.createBooking(
          providerId: 'p1', serviceId: 's1', staffProviderId: 'st1', startTime: DateTime(2026, 10, 3, 11));

      expect(result.fold((f) => f, (_) => null), isA<SlotTakenFailure>());
    });

    test('offers that cannot be read are none, never an error', () async {
      adapter.statusCode = 500;

      expect(await repository.getOffers('p1'), isEmpty);
    });
  });

  group('PriceQuoteCubit', () {
    late _Adapter adapter;
    late PriceQuoteCubit cubit;

    setUp(() {
      adapter = _Adapter();
      final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;
      cubit = PriceQuoteCubit(
        BookingRemoteDataSourceBacked.of(BookingRemoteDataSource(serviceCatalogDio: dio)),
        providerId: 'p1',
        serviceIds: const ['s1'],
        startTime: DateTime(2026, 10, 3, 11),
      );
    });

    tearDown(() => cubit.close());

    test('a failed quote is a failure the page shows as the list price', () async {
      adapter.statusCode = 500;

      await cubit.load();

      expect(cubit.state.status, QuoteStatus.failed);
      expect(cubit.state.quote, isNull);
    });

    test('only an applied code is kept for the booking', () async {
      adapter.body = {
        'data': {'subtotal': 100, 'discount': 30, 'total': 70, 'codeOutcome': 'Applied', 'codeMessage': 'کد تخفیف اعمال شد.'}
      };
      await cubit.applyCode(' LOYAL ', failedMessage: 'x');
      expect(cubit.state.appliedCode, 'LOYAL');
      expect(cubit.state.messageOk, isTrue);

      adapter.body = {
        'data': {'subtotal': 100, 'discount': 30, 'total': 70, 'codeOutcome': 'BetterOfferApplied', 'codeMessage': 'بهتر'}
      };
      await cubit.applyCode('SMALL', failedMessage: 'x');
      expect(cubit.state.appliedCode, isNull);
      expect(cubit.state.message, 'بهتر');

      adapter.body = {
        'data': {'subtotal': 100, 'discount': 0, 'total': 100, 'codeOutcome': 'NotFound', 'codeMessage': 'کد تخفیف معتبر نیست.'}
      };
      await cubit.applyCode('NOPE', failedMessage: 'x');
      expect(cubit.state.appliedCode, isNull);
      expect(cubit.state.messageOk, isFalse);
    });

    test('a check that could not be made says so and keeps the price', () async {
      adapter.body = {
        'data': {'subtotal': 100, 'discount': 0, 'total': 100, 'codeOutcome': 'None'}
      };
      await cubit.load();
      adapter.statusCode = 500;

      await cubit.applyCode('LOYAL', failedMessage: 'بررسی کد انجام نشد');

      expect(cubit.state.message, 'بررسی کد انجام نشد');
      expect(cubit.state.quote!.total, 100);
    });
  });

  group('the salon services grid', () {
    Future<void> pump(WidgetTester tester, List<PublicOffer> offers) async {
      tester.view.physicalSize = const Size(390 * 3, 844 * 3);
      tester.view.devicePixelRatio = 3;
      addTearDown(tester.view.reset);
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Scaffold(
            body: ServicesGrid(
              offers: offers,
              services: const [
                ServiceItem(id: 's1', name: 'کوتاهی مو', price: 200000, currency: 'تومان', durationMinutes: 30),
                ServiceItem(id: 's2', name: 'رنگ مو', price: 500000, currency: 'تومان', durationMinutes: 90),
              ],
            ),
          ),
        ),
      ));
    }

    String toman(int v) => JalaliFormatter.toPersianDigits(PriceFormatter.format(v));

    testWidgets('an offer on one service strikes its price and shows the discounted one', (tester) async {
      await pump(tester, [_offer(services: ['s1'])]);

      expect(find.byKey(const Key('service-offer-s1')), findsOneWidget);
      expect(find.byKey(const Key('service-price-was-s1')), findsOneWidget);
      expect(find.text(toman(160000)), findsOneWidget);
      expect(find.byKey(const Key('service-offer-s2')), findsNothing);
      expect(find.text(toman(500000)), findsOneWidget);
    });

    testWidgets('a conditional offer shows its badge and condition, not a price', (tester) async {
      await pump(tester, [_offer(from: '10:00', to: '13:00')]);

      expect(find.textContaining('ساعت ۱۰:۰۰ تا ۱۳:۰۰'), findsNWidgets(2));
      expect(find.byKey(const Key('service-price-was-s1')), findsNothing);
      expect(find.text(toman(200000)), findsOneWidget);
    });

    testWidgets('no offers, no change', (tester) async {
      await pump(tester, const []);

      expect(find.byKey(const Key('service-offer-s1')), findsNothing);
      expect(find.text(toman(200000)), findsOneWidget);
    });
  });
}

/// The repository over a real data source, as the app wires it.
extension BookingRemoteDataSourceBacked on BookingRemoteDataSource {
  static BookingRepositoryImpl of(BookingRemoteDataSource source) => BookingRepositoryImpl(remoteDataSource: source);
}
