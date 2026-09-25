import 'package:asan_rezerve_provider_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_list_row.dart';
import 'package:asan_rezerve_provider_app/core/widgets/app_section_header.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/cubit/onboarding_cubit.dart';
import 'package:asan_rezerve_provider_app/features/onboarding/presentation/steps/services_step.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements OnboardingRepository {}

Future<OnboardingCubit> _readyCubit(_MockRepo repo, {String? category}) async {
  when(() => repo.getDraft()).thenAnswer((_) async => const Right(null));
  final cubit = OnboardingCubit(repo);
  await cubit.init(phoneNumber: '09120000000');
  if (category != null) cubit.selectCategory(category);
  return cubit;
}

Widget _harness(OnboardingCubit cubit) => MaterialApp(
  home: Directionality(
    textDirection: TextDirection.rtl,
    child: BlocProvider.value(
      value: cubit,
      child: const Scaffold(body: ServicesStep()),
    ),
  ),
);

/// Scrolls a catalogue row into view before acting on it: the list is ~60 rows,
/// taller than the test viewport.
Future<void> _tapRow(WidgetTester tester, String name) async {
  final finder = find.byKey(Key('service-preset-$name'));
  await tester.scrollUntilVisible(
    finder,
    200,
    scrollable: find.byType(Scrollable).first,
  );
  await tester.tap(finder);
  await tester.pump();
}

Future<void> _tapKey(WidgetTester tester, Key key) async {
  final finder = find.byKey(key);
  await tester.scrollUntilVisible(
    finder,
    200,
    scrollable: find.byType(Scrollable).first,
  );
  await tester.tap(finder);
  await tester.pump();
}

void main() {
  testWidgets('empty state: section header with add action, no rows', (
    tester,
  ) async {
    final cubit = await _readyCubit(_MockRepo());

    await tester.pumpWidget(_harness(cubit));

    expect(find.byType(AppSectionHeader), findsOneWidget);
    expect(find.text(AppStrings.servicesTitle), findsWidgets);
    expect(find.byKey(const Key('onboarding-add-service')), findsOneWidget);
    expect(find.byType(AppListRow), findsNothing);
    expect(find.text(AppStrings.noServicesYet), findsOneWidget);
  });

  testWidgets('renders one AppListRow per service with name and subtitle', (
    tester,
  ) async {
    final cubit = await _readyCubit(_MockRepo());
    cubit.setServices(const [
      ServiceDraft(
        name: 'کوتاهی مو',
        durationHours: 0,
        durationMinutes: 30,
        price: 150000,
      ),
      ServiceDraft(
        name: 'رنگ مو',
        durationHours: 1,
        durationMinutes: 0,
        price: 500000,
      ),
    ]);

    await tester.pumpWidget(_harness(cubit));

    expect(find.byType(AppListRow), findsNWidgets(2));
    expect(find.text('کوتاهی مو'), findsOneWidget);
    expect(find.text('رنگ مو'), findsOneWidget);
    expect(find.textContaining('30 دقیقه'), findsOneWidget);
    expect(find.text(AppStrings.noServicesYet), findsNothing);
  });

  testWidgets('deleting a row removes it from cubit state', (tester) async {
    final cubit = await _readyCubit(_MockRepo());
    cubit.setServices(const [
      ServiceDraft(
        name: 'کوتاهی مو',
        durationHours: 0,
        durationMinutes: 30,
        price: 150000,
      ),
    ]);

    await tester.pumpWidget(_harness(cubit));
    expect(find.byType(AppListRow), findsOneWidget);

    await tester.tap(find.byIcon(Icons.delete_outline));
    await tester.pump();

    expect(cubit.state.data.services, isEmpty);
    expect(find.byType(AppListRow), findsNothing);
  });

  group('suggested-services catalogue', () {
    // Typing ~60 services by hand was the longest stretch of the wizard. The
    // catalogue follows the category picked in step 2.
    testWidgets('a barbershop sees the men list, a salon the women list (C1)', (
      tester,
    ) async {
      final men = await _readyCubit(_MockRepo(), category: 'barbershop');
      await tester.pumpWidget(_harness(men));
      await tester.pump();

      expect(find.text('اصلاح ریش و سبیل'), findsOneWidget);
      expect(find.text('میکاپ عروس'), findsNothing);

      final women = await _readyCubit(_MockRepo(), category: 'hair_salon');
      await tester.pumpWidget(_harness(women));
      await tester.pump();

      await tester.enterText(
        find.byKey(const Key('onboarding-service-search')),
        'میکاپ عروس',
      );
      await tester.pump();
      // By key: find.text would also match the text typed into the search box.
      expect(
        find.byKey(const Key('service-preset-میکاپ عروس')),
        findsOneWidget,
      );
    });

    testWidgets(
      'ticking adds the service with its suggestion; unticking removes it (C2)',
      (tester) async {
        final cubit = await _readyCubit(_MockRepo(), category: 'barbershop');
        await tester.pumpWidget(_harness(cubit));
        await tester.pump();

        await _tapRow(tester, 'اصلاح سر با ماشین');

        final added = cubit.state.data.services.single;
        expect(added.name, 'اصلاح سر با ماشین');
        expect(added.price, 250000);
        expect(added.durationHours * 60 + added.durationMinutes, 30);

        await _tapRow(tester, 'اصلاح سر با ماشین');
        expect(cubit.state.data.services, isEmpty);
      },
    );

    testWidgets('editing a ticked row changes only that service (C3)', (
      tester,
    ) async {
      final cubit = await _readyCubit(_MockRepo(), category: 'barbershop');
      await tester.pumpWidget(_harness(cubit));
      await tester.pump();

      await _tapRow(tester, 'اصلاح سر با ماشین');
      await _tapRow(tester, 'خط‌گیری ریش');

      await tester.enterText(
        find.byKey(const Key('service-price-اصلاح سر با ماشین')),
        '300000',
      );
      await tester.pump();

      final byName = {for (final s in cubit.state.data.services) s.name: s};
      expect(byName['اصلاح سر با ماشین']!.price, 300000);
      expect(byName['خط‌گیری ریش']!.price, 100000, reason: 'untouched row');
    });

    testWidgets('the price field groups digits as they are typed (C6)', (
      tester,
    ) async {
      final cubit = await _readyCubit(_MockRepo(), category: 'barbershop');
      await tester.pumpWidget(_harness(cubit));
      await tester.pump();

      await _tapRow(tester, 'اصلاح سر با ماشین');
      await tester.enterText(
        find.byKey(const Key('service-price-اصلاح سر با ماشین')),
        '300000',
      );
      await tester.pump();

      expect(find.text('300,000'), findsOneWidget);
      expect(cubit.state.data.services.single.price, 300000);
    });

    testWidgets(
      'search finds a service in any group, across kaf/ye spellings (C4)',
      (tester) async {
        final cubit = await _readyCubit(_MockRepo(), category: 'hair_salon');
        await tester.pumpWidget(_harness(cubit));
        await tester.pump();

        await tester.enterText(
          find.byKey(const Key('onboarding-service-search')),
          'كراتينه',
        );
        await tester.pump();

        expect(find.text('کراتینه مو'), findsOneWidget);
        expect(find.text('مانیکور ساده'), findsNothing);
      },
    );

    testWidgets('services already chosen come back ticked (C7)', (
      tester,
    ) async {
      final cubit = await _readyCubit(_MockRepo(), category: 'barbershop');
      cubit.setServices(const [
        ServiceDraft(
          name: 'اصلاح سر با قیچی',
          durationHours: 0,
          durationMinutes: 45,
          price: 400000,
        ),
      ]);

      await tester.pumpWidget(_harness(cubit));
      await tester.pump();

      final row = tester.widget<CheckboxListTile>(
        find.byKey(const Key('service-preset-اصلاح سر با قیچی')),
      );
      expect(row.value, isTrue);
      // The saved price wins over the suggestion.
      expect(find.text('400,000'), findsOneWidget);
    });
  });

  group('custom service dialog', () {
    testWidgets(
      'names the empty required field instead of a generic message (R1-R3)',
      (tester) async {
        final cubit = await _readyCubit(_MockRepo(), category: 'barbershop');
        await tester.pumpWidget(_harness(cubit));
        await tester.pump();

        await _tapKey(tester, const Key('onboarding-add-service'));
        await tester.pumpAndSettle();

        await tester.tap(find.byKey(const Key('service-save')));
        await tester.pump();

        expect(find.text(AppStrings.fieldRequired), findsWidgets);
        expect(find.textContaining('تمام فیلد'), findsNothing);
      },
    );

    testWidgets('a custom service is added and survives catalogue picks (C5)', (
      tester,
    ) async {
      final cubit = await _readyCubit(_MockRepo(), category: 'barbershop');
      await tester.pumpWidget(_harness(cubit));
      await tester.pump();

      await _tapKey(tester, const Key('onboarding-add-service'));
      await tester.pumpAndSettle();
      await tester.enterText(
        find.byKey(const Key('service-name')),
        'ماساژ ویژه',
      );
      await tester.enterText(find.byKey(const Key('service-price')), '750000');
      await tester.tap(find.byKey(const Key('service-save')));
      await tester.pumpAndSettle();

      await _tapRow(tester, 'اصلاح سر با ماشین');

      final names = cubit.state.data.services.map((s) => s.name);
      expect(names, containsAll(['ماساژ ویژه', 'اصلاح سر با ماشین']));
    });
  });
}
