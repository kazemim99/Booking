import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/home/domain/entities/composer_models.dart';
import 'package:booksy_provider_app/features/home/domain/entities/saved_customer.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/composer_cubit.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/booking_composer_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fake_contact_picker.dart';

class MockHomeRepository extends Mock implements HomeRepository {}

void main() {
  late MockHomeRepository repository;

  const service = ComposerService(id: 's1', name: 'اصلاح مو', durationMinutes: 45);
  const staff = ComposerStaff(id: 'st1', name: 'سارا');
  final day = DateTime(2026, 7, 15);
  final slot = DateTime(2026, 7, 15, 10);

  setUp(() {
    repository = MockHomeRepository();
    when(() => repository.fetchComposerCatalog()).thenAnswer(
      (_) async => const Right(
          ComposerCatalog(services: [service], staff: [staff])),
    );
    when(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
          serviceIds: any(named: 'serviceIds'),
        )).thenAnswer((_) async => Right(SlotAvailability(slots: [slot])));
    when(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
          serviceIds: any(named: 'serviceIds'),
        )).thenAnswer((_) async => const Right(null));
  });

  /// Pumps a stub home that pushes the composer as a real route so the
  /// pop-result (`true` on creation) is observable. Real theme (button
  /// footgun) + RTL.
  Future<bool?Function()> pumpComposer(WidgetTester tester) async {
    bool? result;
    final cubit = ComposerCubit(repository, now: () => day);
    addTearDown(cubit.close);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                key: const Key('open-composer'),
                onPressed: () async {
                  result = await Navigator.of(context).push<bool>(
                    MaterialPageRoute(
                      builder: (_) => BlocProvider.value(
                        value: cubit..load(),
                        child: const ComposerView(),
                      ),
                    ),
                  );
                },
                child: const Text('open'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.byKey(const Key('open-composer')));
    await tester.pumpAndSettle();
    return () => result;
  }

  testWidgets('single service+staff pre-select and slots render (RTL, real theme)',
      (tester) async {
    await pumpComposer(tester);

    // Pre-selected single options show their names.
    expect(find.text('اصلاح مو'), findsOneWidget);
    expect(find.text('سارا'), findsOneWidget);
    // Slot chip rendered from the live fetch.
    expect(find.byKey(const Key('slot-1000')), findsOneWidget);
  });

  testWidgets('submit is gated until a slot is chosen, then creates and pops true',
      (tester) async {
    // The client fields sit low in a lazy list (below the pick-customer
    // buttons); a taller surface builds them.
    await tester.binding.setSurfaceSize(const Size(1080, 2400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final result = await pumpComposer(tester);

    // Gated: no slot selected yet.
    FilledButton button() => tester.widget<FilledButton>(find.descendant(
        of: find.byKey(const Key('composer-submit')),
        matching: find.byType(FilledButton)));
    expect(button().onPressed, isNull);

    // Choose the slot → submit enables.
    await tester.tap(find.byKey(const Key('slot-1000')));
    await tester.pumpAndSettle();
    expect(button().onPressed, isNotNull);

    // Fill the walk-in client and submit.
    await tester.enterText(
        find.byKey(const Key('composer-client-name')), 'رضا کریمی');
    await tester.tap(find.byKey(const Key('composer-submit')));
    await tester.pumpAndSettle();

    verify(() => repository.createBooking(
          serviceId: 's1',
          staffId: 'st1',
          startTime: slot,
          clientName: 'رضا کریمی',
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
          serviceIds: any(named: 'serviceIds'),
        )).called(1);
    expect(result(), isTrue); // popped with true → Home refreshes
  });

  testWidgets('book-again prefill seeds the walk-in fields', (tester) async {
    // The client fields sit low in a lazy list; a taller surface builds both in
    // the default test viewport (they are otherwise never constructed).
    await tester.binding.setSurfaceSize(const Size(1080, 2400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final cubit = ComposerCubit(repository, now: () => day);
    addTearDown(cubit.close);
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: BlocProvider.value(
            value: cubit..load(),
            child: const ComposerView(
              initialClientName: 'مینا رستمی',
              initialClientPhone: '+989157330950',
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('مینا رستمی'), findsOneWidget);
    expect(find.text('+989157330950'), findsOneWidget);
  });

  testWidgets('no slots for the day shows the plain empty message',
      (tester) async {
    when(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
          serviceIds: any(named: 'serviceIds'),
        )).thenAnswer((_) async => const Right(SlotAvailability.empty()));

    await pumpComposer(tester);

    expect(find.byKey(const Key('composer-no-slots')), findsOneWidget);
    expect(find.text(AppStrings.composerNoSlots), findsOneWidget);
  });

  testWidgets(
      'an explained empty day shows the server reason, not the generic text',
      (tester) async {
    const reason = 'این ارائه‌دهنده هنوز کارمندی اضافه نکرده است.';
    when(() => repository.fetchAvailableSlots(
          serviceId: any(named: 'serviceId'),
          date: any(named: 'date'),
          staffId: any(named: 'staffId'),
          serviceIds: any(named: 'serviceIds'),
        )).thenAnswer(
        (_) async => const Right(SlotAvailability.empty(
              unavailableReason: reason,
            )));

    await pumpComposer(tester);

    expect(find.byKey(const Key('composer-no-slots-reason')), findsOneWidget);
    expect(find.text(reason), findsOneWidget);
    // The generic message must not double up with the specific one.
    expect(find.byKey(const Key('composer-no-slots')), findsNothing);
    expect(find.text(AppStrings.composerNoSlots), findsNothing);
  });

  testWidgets('creation failure keeps the composer open with selections',
      (tester) async {
    when(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
          serviceIds: any(named: 'serviceIds'),
        )).thenAnswer(
            (_) async => const Left(ServerFailure('ثبت نوبت ناموفق بود')));

    final result = await pumpComposer(tester);
    await tester.tap(find.byKey(const Key('slot-1000')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('composer-submit')));
    await tester.pumpAndSettle();

    // Still on the composer, selection intact, error surfaced.
    expect(result(), isNull);
    expect(find.byKey(const Key('composer-submit')), findsOneWidget);
    expect(find.text('ثبت نوبت ناموفق بود'), findsOneWidget);
  });

  group('no staff (spec: provider-booking-composer)', () {
    void withoutStaff() {
      when(() => repository.fetchComposerCatalog()).thenAnswer(
        (_) async => const Right(
            ComposerCatalog(services: [service], staff: [])),
      );
      when(() => repository.fetchAvailableSlots(
            serviceId: any(named: 'serviceId'),
            date: any(named: 'date'),
            staffId: any(named: 'staffId'),
            serviceIds: any(named: 'serviceIds'),
          )).thenAnswer((_) async => const Right(SlotAvailability.empty(
            unavailableReason: 'این ارائه‌دهنده هنوز کارمندی اضافه نکرده است.',
          )));
    }

    testWidgets('shows the up-front notice with an add-staff action',
        (tester) async {
      withoutStaff();
      await pumpComposer(tester);

      expect(find.byKey(const Key('composer-no-staff')), findsOneWidget);
      expect(find.text(AppStrings.composerNoStaffTitle), findsOneWidget);
      expect(find.byKey(const Key('composer-add-staff')), findsOneWidget);
    });

    testWidgets('is absent when the business has staff', (tester) async {
      await pumpComposer(tester);

      expect(find.byKey(const Key('composer-no-staff')), findsNothing);
    });
  });

  group('customer from the book or the contacts (spec: provider-customer-book K5)',
      () {
    const morteza = SavedCustomer(
      id: 'k1',
      firstName: 'مرتضی',
      lastName: 'کاظمی',
      phone: '+989123135143',
    );

    setUpAll(() {
      registerFallbackValue(<CustomerDraft>[]);
    });

    setUp(() {
      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Right([morteza]));
      when(() => repository.createBooking(
            serviceId: any(named: 'serviceId'),
            staffId: any(named: 'staffId'),
            startTime: any(named: 'startTime'),
            clientName: any(named: 'clientName'),
            clientPhone: any(named: 'clientPhone'),
            notes: any(named: 'notes'),
            serviceIds: any(named: 'serviceIds'),
            providerCustomerId: any(named: 'providerCustomerId'),
          )).thenAnswer((_) async => const Right(null));
    });

    Future<void> pump(WidgetTester tester, {FakeContactPicker? picker}) async {
      // The customer fields sit low in a lazy list; a taller surface builds them.
      await tester.binding.setSurfaceSize(const Size(1080, 2400));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      final cubit = ComposerCubit(repository, now: () => day);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light, // real theme: the pick buttons sit in a Row
          home: Directionality(
            textDirection: TextDirection.rtl,
            child: BlocProvider.value(
              value: cubit..load(),
              child: ComposerView(
                contactPicker: picker ?? FakeContactPicker(isSupported: false),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
    }

    Future<void> submit(WidgetTester tester) async {
      await tester.tap(find.byKey(const Key('slot-1000')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('composer-submit')));
      await tester.pumpAndSettle();
    }

    String? bookedFor() => verify(() => repository.createBooking(
          serviceId: any(named: 'serviceId'),
          staffId: any(named: 'staffId'),
          startTime: any(named: 'startTime'),
          clientName: any(named: 'clientName'),
          clientPhone: any(named: 'clientPhone'),
          notes: any(named: 'notes'),
          serviceIds: any(named: 'serviceIds'),
          providerCustomerId: captureAny(named: 'providerCustomerId'),
        )).captured.single as String?;

    testWidgets('picking a saved customer fills both fields and books for them',
        (tester) async {
      await pump(tester);

      await tester.tap(find.byKey(const Key('composer-pick-customer')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('customer-pick-k1')));
      await tester.pumpAndSettle();

      expect(find.text('مرتضی کاظمی'), findsOneWidget);
      expect(find.text('09123135143'), findsOneWidget);

      await submit(tester);
      expect(bookedFor(), 'k1');
    });

    testWidgets('typing a different number books someone else: the link drops',
        (tester) async {
      await pump(tester);
      await tester.tap(find.byKey(const Key('composer-pick-customer')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('customer-pick-k1')));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('composer-client-phone')), '09351112233');
      await tester.pumpAndSettle();

      await submit(tester);
      expect(bookedFor(), isNull);
    });

    testWidgets('the contact button shows only where the phone has a picker',
        (tester) async {
      await pump(tester);
      expect(find.byKey(const Key('composer-pick-contact')), findsNothing);
      expect(find.byKey(const Key('composer-pick-customer')), findsOneWidget);
    });

    testWidgets('a picked contact is saved to the book and the booking is theirs',
        (tester) async {
      const contact = CustomerDraft(
          firstName: 'مرتضی', lastName: 'کاظمی', phone: '09123135143');
      when(() => repository.importCustomers(any())).thenAnswer(
          (_) async => const Right(CustomerImportSummary(added: 1)));
      await pump(tester, picker: FakeContactPicker(picked: const [contact]));

      await tester.tap(find.byKey(const Key('composer-pick-contact')));
      await tester.pumpAndSettle();

      verify(() => repository.importCustomers(const [contact])).called(1);
      expect(find.text('مرتضی کاظمی'), findsOneWidget);

      await submit(tester);
      expect(bookedFor(), 'k1');
    });
  });
}
