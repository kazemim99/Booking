import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/core/widgets/widgets.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/booking/presentation/bloc/booking_bloc.dart';
import 'package:booksy_customer_app/features/booking/presentation/widgets/service_selection_step.dart';

/// Widget tests for the multi-select service step.
///
/// The behaviours that matter: several services can be on at once, the running
/// totals follow the selection, and an empty selection cannot advance the flow —
/// a booking without a service is meaningless.

const _cut = ServiceItem(
  id: 's1',
  name: 'کوتاهی مو',
  price: 250000,
  currency: 'تومان',
  durationMinutes: 45,
);

const _colour = ServiceItem(
  id: 's2',
  name: 'رنگ مو',
  price: 500000,
  currency: 'تومان',
  durationMinutes: 90,
);

const _staff = StaffMember(id: 'st1', name: 'مریم احمدی', isActive: true);

class _FakeRepo implements BookingRepository {
  final List<ServiceItem> services;

  _FakeRepo({this.services = const [_cut, _colour]});

  @override
  Future<Either<Failure, ProviderDetail>> getProviderDetail(String id) async =>
      Right(ProviderDetail(
        id: 'p1',
        businessName: 'سالن نمونه',
        averageRating: 4.8,
        totalReviews: 12,
        businessHours: const [],
        services: services,
        staff: const [_staff],
      ));

  @override
  Future<Either<Failure, List<TimeSlot>>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
    List<String>? serviceIds,
  }) async =>
      const Right([]);

  @override
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
    List<String>? serviceIds,
  }) async =>
      const Right('b1');
}

Finder _tile(ServiceItem service) =>
    find.byKey(Key('booking-service-${service.id}'));

const _continueKey = Key('booking-services-continue');
const _durationKey = Key('booking-services-total-duration');
const _priceKey = Key('booking-services-total-price');
const _countKey = Key('booking-services-count');

String _textOf(WidgetTester tester, Key key) =>
    tester.widget<Text>(find.byKey(key)).data!;

/// True when the tile renders a ticked checkbox.
bool _isChecked(WidgetTester tester, ServiceItem service) {
  final icons = find.descendant(
    of: _tile(service),
    matching: find.byWidgetPredicate((w) => w is Icon && w.icon == Icons.check_box),
  );
  return icons.evaluate().isNotEmpty;
}

Future<void> _settle(WidgetTester tester) async {
  for (var i = 0; i < 6; i++) {
    await tester.pump(const Duration(milliseconds: 20));
  }
}

void main() {
  Future<BookingBloc> pump(
    WidgetTester tester, {
    List<ServiceItem> services = const [_cut, _colour],
  }) async {
    final bloc = BookingBloc(_FakeRepo(services: services));
    // Closed in a tear-down, never inside the test body: `await bloc.close()`
    // under `testWidgets` waits on the fake clock and hangs the test.
    addTearDown(bloc.close);
    bloc.add(const BookingStarted('p1'));

    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      // The app is Persian-first, so the step must lay out right-to-left.
      locale: const Locale('fa'),
      builder: (context, child) =>
          Directionality(textDirection: TextDirection.rtl, child: child!),
      home: Scaffold(
        body: BlocProvider.value(
          value: bloc,
          child: BlocBuilder<BookingBloc, BookingState>(
            bloc: bloc,
            builder: (context, state) => ServiceSelectionStep(state: state),
          ),
        ),
      ),
    ));
    await _settle(tester);
    return bloc;
  }

  group('ServiceSelectionStep', () {
    testWidgets('lists the provider services with a hint that several may be '
        'picked', (tester) async {
      await pump(tester);

      expect(_tile(_cut), findsOneWidget);
      expect(_tile(_colour), findsOneWidget);
      expect(find.text(AppStrings.bookingSelectServicesHint), findsOneWidget);
    });

    testWidgets('renders right-to-left', (tester) async {
      await pump(tester);

      expect(
        Directionality.of(tester.element(find.byKey(_continueKey))),
        TextDirection.rtl,
      );
    });

    testWidgets('starts with nothing selected and continue disabled',
        (tester) async {
      final bloc = await pump(tester);

      expect(_isChecked(tester, _cut), isFalse);
      expect(_isChecked(tester, _colour), isFalse);
      expect(tester.widget<AppButton>(find.byKey(_continueKey)).onPressed,
          isNull);
      expect(_textOf(tester, _countKey),
          AppStrings.bookingSelectAtLeastOneService);
      expect(bloc.state.step, BookingStep.service);
    });

    testWidgets('tapping a service selects it and enables continue',
        (tester) async {
      final bloc = await pump(tester);

      await tester.tap(_tile(_cut));
      await _settle(tester);

      expect(_isChecked(tester, _cut), isTrue);
      expect(bloc.state.services, [_cut]);
      expect(tester.widget<AppButton>(find.byKey(_continueKey)).onPressed,
          isNotNull);
    });

    testWidgets('two services can be selected at once', (tester) async {
      final bloc = await pump(tester);

      await tester.tap(_tile(_cut));
      await _settle(tester);
      await tester.tap(_tile(_colour));
      await _settle(tester);

      expect(_isChecked(tester, _cut), isTrue);
      expect(_isChecked(tester, _colour), isTrue,
          reason: 'selecting a second service must not clear the first');
      expect(bloc.state.services, [_cut, _colour]);
    });

    testWidgets('tapping a selected service deselects it', (tester) async {
      final bloc = await pump(tester);

      await tester.tap(_tile(_cut));
      await _settle(tester);
      await tester.tap(_tile(_colour));
      await _settle(tester);
      await tester.tap(_tile(_cut));
      await _settle(tester);

      expect(_isChecked(tester, _cut), isFalse);
      expect(_isChecked(tester, _colour), isTrue);
      expect(bloc.state.services, [_colour]);
    });

    testWidgets('the summary totals grow and shrink with the selection',
        (tester) async {
      await pump(tester);

      await tester.tap(_tile(_cut));
      await _settle(tester);
      // ۴۵ دقیقه / ۲۵۰۰۰۰ تومان, rendered with Persian digits.
      expect(_textOf(tester, _durationKey), contains('۴۵'));
      expect(_textOf(tester, _priceKey), contains('۲۵۰۰۰۰'));

      await tester.tap(_tile(_colour));
      await _settle(tester);
      expect(_textOf(tester, _durationKey), contains('۱۳۵'),
          reason: '45 + 90 minutes');
      expect(_textOf(tester, _priceKey), contains('۷۵۰۰۰۰'),
          reason: '250000 + 500000');
      expect(_textOf(tester, _countKey), contains('۲'));

      // Removing the colour puts the totals back.
      await tester.tap(_tile(_colour));
      await _settle(tester);
      expect(_textOf(tester, _durationKey), contains('۴۵'));
      expect(_textOf(tester, _priceKey), contains('۲۵۰۰۰۰'));
    });

    testWidgets('deselecting everything disables continue again',
        (tester) async {
      final bloc = await pump(tester);

      await tester.tap(_tile(_cut));
      await _settle(tester);
      await tester.tap(_tile(_cut));
      await _settle(tester);

      expect(bloc.state.services, isEmpty);
      expect(tester.widget<AppButton>(find.byKey(_continueKey)).onPressed,
          isNull);
      expect(bloc.state.step, BookingStep.service);
    });

    testWidgets('tapping continue with a selection advances the flow',
        (tester) async {
      final bloc = await pump(tester);

      await tester.tap(_tile(_cut));
      await _settle(tester);
      await tester.tap(find.byKey(_continueKey));
      await _settle(tester);

      // Single-staff provider, so the staff step is skipped.
      expect(bloc.state.step, BookingStep.time);
    });

    testWidgets('tapping continue with no selection cannot advance the flow',
        (tester) async {
      final bloc = await pump(tester);

      await tester.tap(find.byKey(_continueKey), warnIfMissed: false);
      await _settle(tester);

      expect(bloc.state.step, BookingStep.service);
    });

    testWidgets('the summary survives a 1.3× font scale without overflowing',
        (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 1.3;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

      await pump(tester);
      await tester.tap(_tile(_colour));
      await _settle(tester);

      // A RenderFlex overflow surfaces as a framework exception.
      expect(tester.takeException(), isNull);
      expect(find.byKey(_priceKey), findsOneWidget);
    });

    testWidgets('a selected tile announces its state to a screen reader',
        (tester) async {
      final handle = tester.ensureSemantics();
      await pump(tester);

      await tester.tap(_tile(_cut));
      await _settle(tester);

      expect(
        find.bySemanticsLabel(
          RegExp('${_cut.name}.*${AppStrings.bookingServiceSelectedA11y}'),
        ),
        findsOneWidget,
      );
      handle.dispose();
    });

    testWidgets('a provider with no services shows the empty state',
        (tester) async {
      await pump(tester, services: const []);

      expect(find.byType(EmptyState), findsOneWidget);
      expect(find.byKey(_continueKey), findsNothing);
    });
  });
}
