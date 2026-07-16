import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/home/domain/entities/provider_client.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/clients_cubit.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/clients_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

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

  setUp(() {
    repository = MockHomeRepository();
    when(() => repository.fetchClients())
        .thenAnswer((_) async => const Right([kaveh, mina]));
  });

  group('ClientsCubit', () {
    test('load populates the book; refresh failure keeps it', () async {
      final cubit = ClientsCubit(repository);
      await cubit.load();
      expect(cubit.state.status, ClientsStatus.ready);
      expect(cubit.state.all, hasLength(2));

      when(() => repository.fetchClients())
          .thenAnswer((_) async => const Left(ServerFailure('خطا')));
      await cubit.refresh();
      expect(cubit.state.status, ClientsStatus.ready); // book retained
      expect(cubit.state.all, hasLength(2));
      await cubit.close();
    });

    test('load failure with empty book → failed', () async {
      when(() => repository.fetchClients())
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

  group('ClientsView', () {
    Future<void> pump(WidgetTester tester) async {
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
            child: const ClientsView(),
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
      await pump(tester);

      when(() => repository.fetchClients())
          .thenAnswer((_) async => const Right([mina]));
      await tester.tap(find.byKey(const Key('app-error-retry')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('client-row-c2')), findsOneWidget);
    });
  });
}
