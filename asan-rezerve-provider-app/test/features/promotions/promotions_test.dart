import 'dart:convert';
import 'dart:typed_data';

import 'package:asan_rezerve_provider_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/core/errors/failures.dart';
import 'package:asan_rezerve_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:asan_rezerve_provider_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:asan_rezerve_provider_app/features/home/domain/entities/composer_models.dart';
import 'package:asan_rezerve_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:asan_rezerve_provider_app/features/promotions/data/promotions_api_service.dart';
import 'package:asan_rezerve_provider_app/features/promotions/data/promotions_repository_impl.dart';
import 'package:asan_rezerve_provider_app/features/promotions/domain/promotion.dart';
import 'package:asan_rezerve_provider_app/features/promotions/domain/promotions_repository.dart';
import 'package:asan_rezerve_provider_app/features/promotions/presentation/promotions_cubit.dart';
import 'package:asan_rezerve_provider_app/features/promotions/presentation/promotions_page.dart';
import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

/// A salon's discounts and its platform campaigns (openspec/changes/add-discounts-and-campaigns): the wire format,
/// the draft rules that mirror the server, the cubit taking the server's state, and the page at phone width with the
/// real theme.
class _MockAuth extends Mock implements AuthRepository {}

class _MockSession extends Mock implements ProviderSession {}

class _MockRepo extends Mock implements PromotionsRepository {}

class _MockHome extends Mock implements HomeRepository {}

class _Adapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  final Map<String, Object> bodies = {};
  int statusCode = 200;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    requests.add(options);
    final body = bodies['${options.method} ${options.path}'] ?? const <String, dynamic>{};
    return ResponseBody.fromString(jsonEncode(body), statusCode, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

Map<String, dynamic> _json({
  String id = 'p1',
  String state = 'Active',
  String status = 'Active',
  String activation = 'Automatic',
  String? code,
  int uses = 3,
  int? limit = 50,
  bool pausedByPlatform = false,
  String owner = 'Provider',
}) =>
    {
      'id': id,
      'owner': owner,
      'title': 'صبح‌های وسط هفته',
      'activation': activation,
      'code': code,
      'discountKind': 'Percentage',
      'discountValue': 20,
      'maxDiscountAmount': 100000,
      'minimumSubtotal': null,
      'newCustomersOnly': true,
      'serviceIds': <String>[],
      'daysOfWeek': [6, 0],
      'dailyStartTime': '10:00',
      'dailyEndTime': '13:00',
      'startsAt': '2026-09-20T00:00:00Z',
      'endsAt': null,
      'totalUsageLimit': limit,
      'perCustomerLimit': 1,
      'status': status,
      'state': state,
      'pausedByPlatform': pausedByPlatform,
      'uses': uses,
      'totalDiscount': 240000,
    };

Promotion _promotion({String id = 'p1', String status = 'Active', String state = 'Active', bool pausedByPlatform = false}) =>
    Promotion.fromJson(_json(id: id, status: status, state: state, pausedByPlatform: pausedByPlatform));

CampaignOffer _campaign({bool joined = false}) =>
    CampaignOffer(campaign: Promotion.fromJson(_json(id: 'c1', owner: 'Platform')), isJoined: joined);

Future<void> _pump(WidgetTester tester, Widget child) async {
  tester.view.physicalSize = const Size(390 * 3, 844 * 3);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(MaterialApp(
    theme: AppTheme.light,
    builder: (context, c) => Directionality(textDirection: TextDirection.rtl, child: c!),
    home: child,
  ));
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() {
    registerFallbackValue(const PromotionDraft());
    registerFallbackValue(PromotionAction.pause);
  });

  group('Promotion', () {
    test('reads the server shape', () {
      final p = Promotion.fromJson(_json(activation: 'Code', code: 'SPRING'));

      expect(p.activation, PromotionActivation.code);
      expect(p.code, 'SPRING');
      expect(p.kind, DiscountKind.percentage);
      expect(p.daysOfWeek, [6, 0]);
      expect(p.state, PromotionState.active);
      expect(p.isPlatform, isFalse);
      expect(p.benefitText, '۲۰٪ تخفیف تا سقف ۱۰۰,۰۰۰ تومان');
    });

    test('offers only the actions its status allows; a platform pause cannot be undone by the salon', () {
      expect(_promotion().actions, [PromotionAction.pause, PromotionAction.end]);
      expect(_promotion(status: 'Paused').actions, [PromotionAction.resume, PromotionAction.end]);
      expect(_promotion(status: 'Paused', pausedByPlatform: true).actions, [PromotionAction.end]);
      expect(_promotion(status: 'Ended').actions, isEmpty);
    });
  });

  group('PromotionDraft mirrors the server rules', () {
    final now = DateTime.utc(2026, 10, 1);
    const ok = PromotionDraft(title: 'تخفیف');

    test('a titled 10% promotion is valid', () => expect(ok.validate(now), isNull));

    test('title, percentage range, fixed amount, code, window, end and limits', () {
      expect(const PromotionDraft().validate(now), isNotNull);
      expect(ok.copyWith(value: () => 95).validate(now), contains('۹۰'));
      expect(ok.copyWith(value: () => 0.5).validate(now), isNotNull);
      expect(ok.copyWith(kind: DiscountKind.fixedAmount, value: () => 0).validate(now), isNotNull);
      expect(ok.copyWith(kind: DiscountKind.fixedAmount, value: () => 50000).validate(now), isNull);
      expect(ok.copyWith(activation: PromotionActivation.code, code: 'ab').validate(now), isNotNull);
      expect(ok.copyWith(activation: PromotionActivation.code, code: 'spring-1').validate(now), isNull);
      expect(ok.copyWith(dailyStartTime: () => '14:00', dailyEndTime: () => '10:00').validate(now), isNotNull);
      expect(ok.copyWith(dailyStartTime: () => '10:00').validate(now), isNotNull);
      expect(ok.copyWith(startsAt: () => now, endsAt: () => now).validate(now), isNotNull);
      expect(ok.copyWith(totalUsageLimit: () => 0).validate(now), isNotNull);
    });

    test('sends only what applies', () {
      final json = ok
          .copyWith(
            activation: PromotionActivation.code,
            code: ' spring ',
            kind: DiscountKind.fixedAmount,
            value: () => 50000,
            maxDiscountAmount: () => 999,
            daysOfWeek: [3, 6],
            serviceIds: const [],
          )
          .toJson();

      expect(json['code'], 'SPRING');
      expect(json['discountKind'], 'FixedAmount');
      expect(json['maxDiscountAmount'], isNull, reason: 'a cap is only for percentages');
      expect(json['daysOfWeek'], [3, 6]);
      expect(json['serviceIds'], isNull, reason: 'no services means every service');
      expect(ok.toJson()['code'], isNull, reason: 'an automatic promotion has no code');
    });

    test('generated codes are readable and valid', () {
      var i = 0;
      final code = generatePromotionCode((max) => (i++ * 7) % max);
      expect(RegExp(r'^OFF-[A-HJ-NP-Z2-9]{6}$').hasMatch(code), isTrue);
      expect(ok.copyWith(activation: PromotionActivation.code, code: code).validate(DateTime.utc(2026)), isNull);
    });
  });

  group('PromotionsRepositoryImpl on the wire', () {
    late _Adapter adapter;
    late PromotionsRepositoryImpl repository;

    setUp(() {
      adapter = _Adapter();
      final auth = _MockAuth();
      final session = _MockSession();
      when(() => session.providerId).thenReturn('s1');
      when(() => auth.getCurrentSession()).thenAnswer((_) async => Right(session));
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))..httpClientAdapter = adapter;
      repository = PromotionsRepositoryImpl(PromotionsApiService(dio), auth);
    });

    test('lists the salon promotions from the envelope', () async {
      adapter.bodies['GET /v1/providers/s1/promotions'] = {'data': [_json()]};

      final result = await repository.list();

      expect(result.getOrElse(() => const []).single.id, 'p1');
    });

    test('creates with the draft as the body', () async {
      adapter.bodies['POST /v1/providers/s1/promotions'] = {'data': _json(id: 'new')};

      await repository.create(const PromotionDraft(title: 'تخفیف', value: 15));

      final sent = adapter.requests.single;
      expect(sent.method, 'POST');
      expect((sent.data as Map)['discountValue'], 15);
      expect((sent.data as Map)['activation'], 'Automatic');
    });

    test('lifecycle actions post to their own path', () async {
      adapter.bodies['POST /v1/providers/s1/promotions/p1/pause'] = {'data': _json(status: 'Paused', state: 'Paused')};

      final result = await repository.change('p1', PromotionAction.pause);

      expect(adapter.requests.single.path, '/v1/providers/s1/promotions/p1/pause');
      expect(result.getOrElse(() => throw 'x').state, PromotionState.paused);
    });

    test('joining posts and leaving deletes the enrollment', () async {
      adapter.bodies['POST /v1/providers/s1/campaigns/c1/enrollment'] = {'data': {'campaign': _json(id: 'c1'), 'isJoined': true}};
      adapter.bodies['DELETE /v1/providers/s1/campaigns/c1/enrollment'] = {'data': {'campaign': _json(id: 'c1'), 'isJoined': false}};

      expect((await repository.setJoined('c1', join: true)).getOrElse(() => throw 'x').isJoined, isTrue);
      expect((await repository.setJoined('c1', join: false)).getOrElse(() => throw 'x').isJoined, isFalse);
    });

    test("a refusal carries the field's own Persian sentence", () async {
      adapter.statusCode = 400;
      adapter.bodies['POST /v1/providers/s1/promotions'] = {
        'success': false,
        'message': "Validation failed for property 'DiscountValue': درصد تخفیف باید بین ۱ تا ۹۰ باشد.",
        'error': {
          'errors': {
            'DiscountValue': ['درصد تخفیف باید بین ۱ تا ۹۰ باشد.']
          }
        },
      };

      final result = await repository.create(const PromotionDraft(title: 'x', value: 95));

      expect(result.fold((f) => f, (_) => null), const ValidationFailure('درصد تخفیف باید بین ۱ تا ۹۰ باشد.'));
    });
  });

  group('PromotionsCubit', () {
    late _MockRepo repo;
    late _MockHome home;

    setUp(() {
      repo = _MockRepo();
      home = _MockHome();
      when(() => home.fetchServices()).thenAnswer((_) async => const Right([
            ComposerService(id: 'svc1', name: 'کوتاهی مو', durationMinutes: 30, price: 200000),
          ]));
    });

    test('a failed load is a failure, never "no discounts"', () async {
      when(() => repo.list()).thenAnswer((_) async => const Left(ServerFailure('قطع شد')));
      when(() => repo.campaigns()).thenAnswer((_) async => const Right([]));
      final cubit = PromotionsCubit(repo, home);

      await cubit.load();

      expect(cubit.state.loaded, isFalse);
      expect(cubit.state.error, 'قطع شد');
    });

    test('services that cannot be read do not fail the page', () async {
      when(() => repo.list()).thenAnswer((_) async => Right([_promotion()]));
      when(() => repo.campaigns()).thenAnswer((_) async => const Right([]));
      when(() => home.fetchServices()).thenAnswer((_) async => const Left(ServerFailure('x')));
      final cubit = PromotionsCubit(repo, home);

      await cubit.load();

      expect(cubit.state.loaded, isTrue);
      expect(cubit.state.services, isEmpty);
    });

    test('an invalid draft never reaches the server', () async {
      final cubit = PromotionsCubit(repo, home);

      final error = await cubit.save(const PromotionDraft(title: 'x', value: 99));

      expect(error, isNotNull);
      verifyNever(() => repo.create(any()));
    });

    test('a created promotion goes to the top; an edited one is replaced in place', () async {
      when(() => repo.list()).thenAnswer((_) async => Right([_promotion(id: 'a')]));
      when(() => repo.campaigns()).thenAnswer((_) async => const Right([]));
      when(() => repo.create(any())).thenAnswer((_) async => Right(_promotion(id: 'b')));
      when(() => repo.update('a', any())).thenAnswer((_) async => Right(_promotion(id: 'a', status: 'Paused', state: 'Paused')));
      final cubit = PromotionsCubit(repo, home);
      await cubit.load();

      expect(await cubit.save(const PromotionDraft(title: 'جدید')), isNull);
      expect(cubit.state.promotions.map((p) => p.id), ['b', 'a']);

      expect(await cubit.save(const PromotionDraft(title: 'ویرایش'), editingId: 'a'), isNull);
      expect(cubit.state.promotions.last.state, PromotionState.paused);
    });

    test('a refused change leaves the promotion as it was and says why', () async {
      when(() => repo.list()).thenAnswer((_) async => Right([_promotion()]));
      when(() => repo.campaigns()).thenAnswer((_) async => const Right([]));
      when(() => repo.change('p1', PromotionAction.resume))
          .thenAnswer((_) async => const Left(ServerFailure('توسط پشتیبانی متوقف شده')));
      final cubit = PromotionsCubit(repo, home);
      await cubit.load();

      expect(await cubit.change('p1', PromotionAction.resume), 'توسط پشتیبانی متوقف شده');
      expect(cubit.state.promotions.single.state, PromotionState.active);
      expect(cubit.state.busyId, isNull);
    });

    test('joining marks the campaign joined from the server answer', () async {
      when(() => repo.list()).thenAnswer((_) async => const Right([]));
      when(() => repo.campaigns()).thenAnswer((_) async => Right([_campaign()]));
      when(() => repo.setJoined('c1', join: true)).thenAnswer((_) async => Right(_campaign(joined: true)));
      final cubit = PromotionsCubit(repo, home);
      await cubit.load();

      expect(await cubit.setJoined('c1', join: true), isNull);
      expect(cubit.state.campaigns.single.isJoined, isTrue);
      expect(cubit.state.joinedCount, 1);
    });
  });

  group('the discounts page', () {
    late _MockRepo repo;
    late _MockHome home;

    setUp(() {
      repo = _MockRepo();
      home = _MockHome();
      when(() => home.fetchServices()).thenAnswer((_) async => const Right([]));
    });

    Future<PromotionsCubit> open(WidgetTester tester, {List<Promotion> promotions = const [], List<CampaignOffer> campaigns = const []}) async {
      when(() => repo.list()).thenAnswer((_) async => Right(promotions));
      when(() => repo.campaigns()).thenAnswer((_) async => Right(campaigns));
      final cubit = PromotionsCubit(repo, home);
      await _pump(tester, BlocProvider.value(value: cubit..load(), child: const PromotionsPage()));
      return cubit;
    }

    testWidgets('no discounts yet: an invitation to create one, not a blank page', (tester) async {
      await open(tester);

      expect(find.text(AppStrings.promotionsEmpty), findsOneWidget);
      expect(find.text(AppStrings.promotionsEmptyHint), findsOneWidget);
    });

    testWidgets('a promotion shows its benefit, state, conditions and usage', (tester) async {
      await open(tester, promotions: [_promotion()]);

      expect(find.text('صبح‌های وسط هفته'), findsOneWidget);
      expect(find.text('۲۰٪ تخفیف تا سقف ۱۰۰,۰۰۰ تومان'), findsOneWidget);
      expect(find.text(AppStrings.promotionsStateActive), findsOneWidget);
      expect(find.text(AppStrings.promotionsNewCustomers), findsOneWidget);
      expect(find.text(AppStrings.promotionsHours('10:00', '13:00')), findsOneWidget);
      expect(find.textContaining('۳ از ۵۰ استفاده'), findsOneWidget);
      expect(find.byKey(const Key('promotion-pause-p1')), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('pausing goes through the cubit', (tester) async {
      when(() => repo.change('p1', PromotionAction.pause))
          .thenAnswer((_) async => Right(_promotion(status: 'Paused', state: 'Paused')));
      await open(tester, promotions: [_promotion()]);

      await tester.tap(find.byKey(const Key('promotion-pause-p1')));
      await tester.pumpAndSettle();

      verify(() => repo.change('p1', PromotionAction.pause)).called(1);
      expect(find.text(AppStrings.promotionsStatePaused), findsOneWidget);
    });

    testWidgets('the campaigns tab explains who pays and joins on tap', (tester) async {
      when(() => repo.setJoined('c1', join: true)).thenAnswer((_) async => Right(_campaign(joined: true)));
      await open(tester, campaigns: [_campaign()]);

      await tester.tap(find.text(AppStrings.promotionsCampaigns));
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.promotionsCampaignsHint), findsOneWidget);

      await tester.tap(find.byKey(const Key('campaign-join-c1')));
      await tester.pumpAndSettle();

      verify(() => repo.setJoined('c1', join: true)).called(1);
      expect(find.text(AppStrings.promotionsJoined), findsOneWidget);
    });

    testWidgets('the form opens, previews the benefit and refuses a 95% discount in Persian', (tester) async {
      await open(tester);

      await tester.tap(find.byKey(const Key('promotions-new')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const Key('promotion-title')).last, 'تخفیف پاییزه');
      await tester.enterText(find.byKey(const Key('promotion-value')).last, '95');
      await tester.pumpAndSettle();
      expect(find.text('۹۵٪ تخفیف'), findsOneWidget);

      await tester.ensureVisible(find.byKey(const Key('promotion-save')));
      await tester.tap(find.byKey(const Key('promotion-save')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('promotion-error')), findsOneWidget);
      verifyNever(() => repo.create(any()));
      expect(tester.takeException(), isNull);
    });

    testWidgets('a valid form is saved and the sheet closes', (tester) async {
      when(() => repo.create(any())).thenAnswer((_) async => Right(_promotion(id: 'new')));
      await open(tester);

      await tester.tap(find.byKey(const Key('promotions-new')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const Key('promotion-title')).last, 'تخفیف پاییزه');
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('promotion-save')));
      await tester.tap(find.byKey(const Key('promotion-save')));
      await tester.pumpAndSettle();

      final sent = verify(() => repo.create(captureAny())).captured.single as PromotionDraft;
      expect(sent.title, 'تخفیف پاییزه');
      expect(sent.value, 10);
      expect(find.byKey(const Key('promotion-save')), findsNothing);
    });
  });
}
