import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/home/domain/entities/provider_client.dart';
import 'package:booksy_provider_app/features/home/domain/entities/saved_customer.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/clients_cubit.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/clients_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fake_contact_picker.dart';

class MockHomeRepository extends Mock implements HomeRepository {}

void main() {
  late MockHomeRepository repository;

  const kaveh = ProviderClient(
    customerId: 'c1',
    name: 'کاوه احمدی', // Persian kaf
    phone: '+989121112233',
    totalBookings: 3,
    completedBookings: 2,
    upcomingBookings: 1,
  );
  const mina = ProviderClient(
    customerId: 'c2',
    name: 'مینا رستمی',
    phone: '+989157330950',
    totalBookings: 1,
    upcomingBookings: 1,
  );
  const morteza = SavedCustomer(
    id: 'k1',
    firstName: 'مرتضی',
    lastName: 'کاظمی',
    phone: '+989123135143',
    totalBookings: 2,
    upcomingBookings: 1,
  );

  setUpAll(() {
    registerFallbackValue(const CustomerDraft(firstName: '', phone: ''));
    registerFallbackValue(<CustomerDraft>[]);
  });

  setUp(() {
    repository = MockHomeRepository();
    when(() => repository.fetchClients())
        .thenAnswer((_) async => const Right([kaveh, mina]));
    when(() => repository.fetchSavedCustomers())
        .thenAnswer((_) async => const Right([]));
  });

  group('ClientsCubit', () {
    test('load populates the book; refresh failure keeps it', () async {
      final cubit = ClientsCubit(repository);
      await cubit.load();
      expect(cubit.state.status, ClientsStatus.ready);
      expect(cubit.state.all, hasLength(2));

      when(() => repository.fetchClients())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      await cubit.refresh();
      expect(cubit.state.status, ClientsStatus.ready); // book retained
      expect(cubit.state.all, hasLength(2));
      await cubit.close();
    });

    test('load failure with empty book → failed', () async {
      when(() => repository.fetchClients())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      final cubit = ClientsCubit(repository);
      await cubit.load();
      expect(cubit.state.status, ClientsStatus.failed);
      expect(cubit.state.error, 'خطا');
      await cubit.close();
    });

    test('search normalizes Persian variants (spec: كاوه matches کاوه)',
        () async {
      final cubit = ClientsCubit(repository);
      await cubit.load();

      cubit.search('كاوه'); // Arabic kaf
      expect(cubit.state.filtered.single.customerId, 'c1');

      cubit.search('915'); // phone fragment (+98 strips the leading zero)
      expect(cubit.state.filtered.single.customerId, 'c2');

      cubit.search('یافت‌نشدنی');
      expect(cubit.state.filtered, isEmpty);
      await cubit.close();
    });
  });

  group('customer book (spec: provider-customer-book)', () {
    test('a saved customer and an online client with the same number are one row',
        () {
      const online = ProviderClient(
        customerId: 'u9',
        name: 'Morteza K',
        phone: '09123135143',
        totalBookings: 3,
      );
      final rows = ClientsCubit.merge([morteza], [online, mina]);

      expect(rows, hasLength(2));
      expect(rows.first.saved, morteza);
      expect(rows.first.name, 'مرتضی کاظمی');
      expect(rows.first.totalBookings, 5); // 2 booked for them + 3 online
      expect(rows.first.upcomingBookings, 1);
      expect(rows.last.customerId, 'c2'); // the other online client kept
    });

    test('the book still loads when only the saved customers fail', () async {
      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      final cubit = ClientsCubit(repository);
      await cubit.load();
      expect(cubit.state.status, ClientsStatus.ready);
      expect(cubit.state.all, hasLength(2));
      await cubit.close();
    });

    test('a saved customer is searchable by its number as typed locally',
        () async {
      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Right([morteza]));
      final cubit = ClientsCubit(repository);
      await cubit.load();
      cubit.search('0912313');
      expect(cubit.state.filtered.single.customerId, 'k1');
      await cubit.close();
    });
  });

  group('ClientsView', () {
    Future<void> pump(WidgetTester tester, {FakeContactPicker? picker}) async {
      final cubit = ClientsCubit(repository);
      addTearDown(cubit.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light, // real theme (button footgun)
          builder: (context, child) => Directionality(
            textDirection: TextDirection.rtl,
            child: child ?? const SizedBox.shrink(),
          ),
          home: BlocProvider<ClientsCubit>.value(
            value: cubit..load(),
            child: ClientsView(
              contactPicker: picker ?? FakeContactPicker(isSupported: false),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
    }

    testWidgets('renders client rows with names and counts', (tester) async {
      await pump(tester);

      expect(find.byKey(const Key('client-row-c1')), findsOneWidget);
      expect(find.text('کاوه احمدی'), findsOneWidget);
      expect(find.textContaining(AppStrings.clientBookings(3, 1)),
          findsOneWidget);
    });

    testWidgets('search field filters the list', (tester) async {
      await pump(tester);

      await tester.enterText(
          find.byKey(const Key('clients-search')), 'مینا');
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('client-row-c2')), findsOneWidget);
      expect(find.byKey(const Key('client-row-c1')), findsNothing);
    });

    testWidgets('client sheet offers book-again and call (real theme rows)',
        (tester) async {
      await pump(tester);

      await tester.tap(find.byKey(const Key('client-row-c1')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('client-book-again')), findsOneWidget);
      expect(find.byKey(const Key('client-call')), findsOneWidget);
    });

    testWidgets('empty book shows the inviting empty state', (tester) async {
      when(() => repository.fetchClients())
          .thenAnswer((_) async => const Right([]));
      await pump(tester);

      expect(find.text(AppStrings.clientsEmptyTitle), findsOneWidget);
      expect(find.text(AppStrings.clientsEmptyBody), findsOneWidget);
    });

    testWidgets('failure shows retry that reloads', (tester) async {
      when(() => repository.fetchClients())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      await pump(tester);

      when(() => repository.fetchClients())
          .thenAnswer((_) async => const Right([mina]));
      await tester.tap(find.byKey(const Key('app-error-retry')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('client-row-c2')), findsOneWidget);
    });

    testWidgets(
        'K1 add: required fields are marked inline, then the customer is saved',
        (tester) async {
      when(() => repository.addCustomer(any()))
          .thenAnswer((_) async => const Right(morteza));
      await pump(tester);

      await tester.tap(find.byKey(const Key('clients-add')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('customer-save')));
      await tester.pumpAndSettle();
      // First name and mobile: the inline "required" line, never a generic banner.
      expect(find.text(AppStrings.fieldRequired), findsNWidgets(2));
      verifyNever(() => repository.addCustomer(any()));

      await tester.enterText(
          find.byKey(const Key('customer-first-name')), 'مرتضی');
      await tester.enterText(
          find.byKey(const Key('customer-last-name')), 'کاظمی');
      await tester.enterText(find.byKey(const Key('customer-phone')), '0912');
      await tester.tap(find.byKey(const Key('customer-save')));
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.customerPhoneInvalid), findsOneWidget);

      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Right([morteza]));
      await tester.enterText(
          find.byKey(const Key('customer-phone')), '09123135143');
      await tester.tap(find.byKey(const Key('customer-save')));
      await tester.pumpAndSettle();

      verify(() => repository.addCustomer(const CustomerDraft(
          firstName: 'مرتضی',
          lastName: 'کاظمی',
          phone: '09123135143'))).called(1);
      expect(find.byKey(const Key('client-row-k1')), findsOneWidget);
    });

    testWidgets('import is offered only where the phone has a contact picker',
        (tester) async {
      await pump(tester);
      expect(find.byKey(const Key('clients-import')), findsNothing);
    });

    testWidgets(
        'K3 import sends exactly the ticked contacts and reports the result',
        (tester) async {
      const ticked = [
        CustomerDraft(
            firstName: 'سارا', lastName: 'احمدی', phone: '09351112233'),
      ];
      when(() => repository.importCustomers(any())).thenAnswer(
          (_) async => const Right(CustomerImportSummary(added: 1)));
      final picker = FakeContactPicker(picked: ticked);
      await pump(tester, picker: picker);

      await tester.tap(find.byKey(const Key('clients-import')));
      await tester.pumpAndSettle();

      expect(picker.calls, 1);
      verify(() => repository.importCustomers(ticked)).called(1);
      expect(find.text(AppStrings.customerImportResult(1, 0, 0)),
          findsOneWidget);
    });

    testWidgets('K4 a saved customer can be edited and removed from its sheet',
        (tester) async {
      when(() => repository.fetchSavedCustomers())
          .thenAnswer((_) async => const Right([morteza]));
      when(() => repository.removeCustomer(any()))
          .thenAnswer((_) async => const Right(null));
      await pump(tester);

      await tester.tap(find.byKey(const Key('client-row-k1')));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('client-edit')), findsOneWidget);
      expect(find.byKey(const Key('client-save')), findsNothing);

      await tester.tap(find.byKey(const Key('client-remove')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('client-remove-confirm')));
      await tester.pumpAndSettle();

      verify(() => repository.removeCustomer('k1')).called(1);
    });

    testWidgets('an online client can be saved into the book', (tester) async {
      await pump(tester);

      await tester.tap(find.byKey(const Key('client-row-c1')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('client-save')), findsOneWidget);
      expect(find.byKey(const Key('client-edit')), findsNothing);
    });
  });
}
