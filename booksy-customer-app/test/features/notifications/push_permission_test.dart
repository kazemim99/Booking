import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/push/push_registration.dart';
import 'package:booksy_customer_app/features/notifications/presentation/push_enable_tile.dart';
import 'package:booksy_customer_app/features/notifications/presentation/push_permission_cubit.dart';
import 'package:booksy_customer_app/features/notifications/presentation/push_soft_prompt.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Turning notifications on, from a tap. In a browser that is the only moment the permission prompt may appear, so
/// the app offers it twice: a row in the profile, always there, and a one-time card right after a booking is made —
/// the moment the customer has something to be told about ("the salon confirmed").
class _FakeSettings implements PushSettings {
  PushStatus current;
  PushStatus answer;
  int enables = 0;

  _FakeSettings(this.current, {this.answer = PushStatus.enabled});

  @override
  Future<PushStatus> status() async => current;

  @override
  Future<PushStatus> enable() async {
    enables++;
    return current = answer;
  }
}

class _Memory implements PushPromptMemory {
  bool dismissed;
  _Memory({this.dismissed = false});

  @override
  Future<bool> wasDismissed() async => dismissed;

  @override
  Future<void> dismiss() async => dismissed = true;
}

void main() {
  group('PushPermissionCubit', () {
    test('starts unavailable, so nothing is offered before the status is known', () {
      final cubit = PushPermissionCubit(_FakeSettings(PushStatus.notAsked), _Memory());

      expect(cubit.state.status, PushStatus.unavailable);
      expect(cubit.state.showsSoftPrompt, isFalse);
    });

    test('loads the status and whether the card was dismissed', () async {
      final cubit = PushPermissionCubit(_FakeSettings(PushStatus.notAsked), _Memory());

      await cubit.load();

      expect(cubit.state.status, PushStatus.notAsked);
      expect(cubit.state.showsSoftPrompt, isTrue);
    });

    test('the card is offered once: dismissed, it stays dismissed', () async {
      final memory = _Memory();
      final cubit = PushPermissionCubit(_FakeSettings(PushStatus.notAsked), memory);
      await cubit.load();

      await cubit.dismissPrompt();

      expect(cubit.state.showsSoftPrompt, isFalse);
      expect(memory.dismissed, isTrue);

      final later = PushPermissionCubit(_FakeSettings(PushStatus.notAsked), memory);
      await later.load();
      expect(later.state.showsSoftPrompt, isFalse);
    });

    test('the card is not offered when the question is already answered', () async {
      for (final answered in [PushStatus.enabled, PushStatus.blocked, PushStatus.unavailable]) {
        final cubit = PushPermissionCubit(_FakeSettings(answered), _Memory());
        await cubit.load();
        expect(cubit.state.showsSoftPrompt, isFalse, reason: '$answered');
      }
    });

    test('turning them on reports the answer', () async {
      final cubit = PushPermissionCubit(_FakeSettings(PushStatus.notAsked, answer: PushStatus.blocked), _Memory());
      await cubit.load();

      final result = await cubit.enable();

      expect(result, PushStatus.blocked);
      expect(cubit.state.status, PushStatus.blocked);
      expect(cubit.state.busy, isFalse);
    });

    test('a second tap while the prompt is open does not ask twice', () async {
      final settings = _FakeSettings(PushStatus.notAsked);
      final cubit = PushPermissionCubit(settings, _Memory());
      await cubit.load();

      await Future.wait([cubit.enable(), cubit.enable()]);

      expect(settings.enables, 1);
    });
  });

  Future<PushPermissionCubit> pump(WidgetTester tester, Widget Function(PushPermissionCubit) build,
      _FakeSettings settings, {_Memory? memory}) async {
    final cubit = PushPermissionCubit(settings, memory ?? _Memory());
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      home: Scaffold(body: ListView(children: [build(cubit)])),
    ));
    await tester.pumpAndSettle();
    return cubit;
  }

  group('profile row', () {
    testWidgets('a build without push shows no row at all', (tester) async {
      await pump(tester, (c) => PushEnableTile(cubit: c), _FakeSettings(PushStatus.unavailable));

      expect(find.byKey(const Key('push-enable-tile')), findsNothing);
    });

    testWidgets('never asked: a tap asks, and says it worked', (tester) async {
      final settings = _FakeSettings(PushStatus.notAsked);
      await pump(tester, (c) => PushEnableTile(cubit: c), settings);

      expect(find.text(AppStrings.pushEnableAction), findsOneWidget);
      await tester.tap(find.byKey(const Key('push-enable-tile')));
      await tester.pumpAndSettle();

      expect(settings.enables, 1);
      expect(find.text(AppStrings.pushEnabledTitle), findsWidgets);
      expect(find.text(AppStrings.pushEnabledSnack), findsOneWidget);
    });

    testWidgets('declined in the prompt: the row says how to undo it', (tester) async {
      final settings = _FakeSettings(PushStatus.notAsked, answer: PushStatus.blocked);
      await pump(tester, (c) => PushEnableTile(cubit: c), settings);

      await tester.tap(find.byKey(const Key('push-enable-tile')));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.pushBlockedTitle), findsOneWidget);
      expect(find.text(AppStrings.pushBlockedHint), findsWidgets);
    });

    testWidgets('already on: says so, and a tap asks nothing', (tester) async {
      final settings = _FakeSettings(PushStatus.enabled);
      await pump(tester, (c) => PushEnableTile(cubit: c), settings);

      expect(find.text(AppStrings.pushEnabledTitle), findsOneWidget);
      await tester.tap(find.byKey(const Key('push-enable-tile')));
      await tester.pumpAndSettle();

      expect(settings.enables, 0);
    });

    testWidgets('allowed but not connected: the row says so and a tap tries again', (tester) async {
      final settings = _FakeSettings(PushStatus.notAsked, answer: PushStatus.unreachable);
      await pump(tester, (c) => PushEnableTile(cubit: c), settings);

      await tester.tap(find.byKey(const Key('push-enable-tile')));
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.pushUnreachableTitle), findsOneWidget);
      expect(find.text(AppStrings.pushEnabledTitle), findsNothing, reason: 'nothing can arrive yet');

      settings.answer = PushStatus.enabled;
      await tester.tap(find.byKey(const Key('push-enable-tile')));
      await tester.pumpAndSettle();

      expect(settings.enables, 2);
      expect(find.text(AppStrings.pushEnabledTitle), findsWidgets);
    });

    testWidgets('blocked: the browser will not ask again, so the row explains instead of offering a dead tap',
        (tester) async {
      final settings = _FakeSettings(PushStatus.blocked);
      await pump(tester, (c) => PushEnableTile(cubit: c), settings);

      expect(find.text(AppStrings.pushBlockedHint), findsOneWidget);
      await tester.tap(find.byKey(const Key('push-enable-tile')));
      await tester.pumpAndSettle();

      expect(settings.enables, 0);
    });
  });

  group('one-time card after a booking', () {
    testWidgets('never asked: offered, and turning on from it hides it', (tester) async {
      final settings = _FakeSettings(PushStatus.notAsked);
      await pump(tester, (c) => PushSoftPrompt(cubit: c), settings);

      expect(find.text(AppStrings.pushPromptTitle), findsOneWidget);
      await tester.tap(find.byKey(const Key('push-prompt-enable')));
      await tester.pumpAndSettle();

      expect(settings.enables, 1);
      expect(find.text(AppStrings.pushPromptTitle), findsNothing);
    });

    testWidgets('dismissed: gone, and remembered', (tester) async {
      final memory = _Memory();
      await pump(tester, (c) => PushSoftPrompt(cubit: c), _FakeSettings(PushStatus.notAsked), memory: memory);

      await tester.tap(find.byKey(const Key('push-prompt-dismiss')));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.pushPromptTitle), findsNothing);
      expect(memory.dismissed, isTrue);
    });

    testWidgets('dismissed before: not offered again', (tester) async {
      await pump(tester, (c) => PushSoftPrompt(cubit: c), _FakeSettings(PushStatus.notAsked),
          memory: _Memory(dismissed: true));

      expect(find.text(AppStrings.pushPromptTitle), findsNothing);
    });

    testWidgets('already answered or unavailable: not offered', (tester) async {
      for (final status in [PushStatus.enabled, PushStatus.blocked, PushStatus.unavailable]) {
        await pump(tester, (c) => PushSoftPrompt(cubit: c), _FakeSettings(status));
        expect(find.text(AppStrings.pushPromptTitle), findsNothing, reason: '$status');
      }
    });

    testWidgets('fits a small phone at a large font without overflowing', (tester) async {
      tester.view.physicalSize = const Size(320 * 3, 640 * 3);
      tester.view.devicePixelRatio = 3;
      addTearDown(tester.view.reset);
      final cubit = PushPermissionCubit(_FakeSettings(PushStatus.notAsked), _Memory());
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.light,
        home: MediaQuery(
          data: const MediaQueryData(textScaler: TextScaler.linear(1.3)),
          child: Scaffold(body: ListView(children: [PushSoftPrompt(cubit: cubit)])),
        ),
      ));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.text(AppStrings.pushPromptTitle), findsOneWidget);
    });
  });
}
