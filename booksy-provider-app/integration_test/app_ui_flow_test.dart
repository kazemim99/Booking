import 'package:booksy_provider_app/app.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/di/injection.dart';
import 'package:booksy_provider_app/core/storage/secure_storage_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:pinput/pinput.dart';

/// REAL-UI end-to-end test. Drives the actual ProviderApp (real DI, real
/// router, real HTTP) on a real device against a running backend, covering
/// every screen, validation, interaction, navigation and state transition:
///
///   login (+2 validations) → OTP → onboarding steps 1..8 (+4 validations,
///   service dialog, hours toggle, gallery skip, preview) → dashboard
///
/// HOW TO RUN (verified on an Android emulator):
///   1. backend on :5000 started with OTP_SANDBOX_CODE=123456
///   2. an emulator booted (e.g. `emulator -avd Pixel_7`)
///   3. flutter test integration_test/app_ui_flow_test.dart -d emulator-5554
///
/// The emulator reaches the host backend at 10.0.2.2:5000 (see ApiConstants);
/// cleartext HTTP is enabled in the DEBUG-only AndroidManifest.
///
/// NOT RUNNABLE ON WEB/CHROME: flutter_secure_storage's web implementation
/// throws `OperationError` (DomException) on read, so every authenticated
/// request fails before it leaves the app. Mobile is the target platform; the
/// web bug is tracked as a deferred item.
const String _otp = '123456';

/// The OTP screen runs a 1s resend countdown, so the tree never goes idle —
/// pumpAndSettle would hang. Pump until the expected widget appears instead.
Future<void> _pumpUntil(
  WidgetTester tester,
  Finder finder, {
  Duration timeout = const Duration(seconds: 30),
  String? reason,
}) async {
  final deadline = DateTime.now().add(timeout);
  final snackbars = <String>{};
  while (DateTime.now().isBefore(deadline)) {
    await tester.pump(const Duration(milliseconds: 150));
    if (finder.evaluate().isNotEmpty) return;
    // Error snackbars auto-dismiss, so capture them as they appear.
    for (final sb in find.byType(SnackBar).evaluate()) {
      final texts = find
          .descendant(of: find.byWidget(sb.widget), matching: find.byType(Text))
          .evaluate()
          .map((e) => (e.widget as Text).data)
          .whereType<String>();
      snackbars.addAll(texts);
    }
  }
  final visible = tester
      .widgetList<Text>(find.byType(Text))
      .map((t) => t.data)
      .whereType<String>()
      .toList();
  throw TestFailure(
    'Timed out waiting for: ${reason ?? finder.toString()}\n'
    'Snackbars seen while waiting: $snackbars\n'
    'On-screen text was: $visible',
  );
}

/// Validation snackbars render at the BOTTOM, directly over the wizard's
/// next/back buttons — tapping while one is up hits the snackbar instead. Wait
/// for it to auto-dismiss before driving the action row again.
Future<void> _waitForSnackbarToClear(WidgetTester tester) async {
  final deadline = DateTime.now().add(const Duration(seconds: 10));
  while (DateTime.now().isBefore(deadline) &&
      find.byType(SnackBar).evaluate().isNotEmpty) {
    await tester.pump(const Duration(milliseconds: 250));
  }
  await tester.pump(const Duration(milliseconds: 300));
}

Future<void> _tap(WidgetTester tester, Finder finder) async {
  await tester.ensureVisible(finder);
  await tester.pump();
  await tester.tap(finder);
  await tester.pump();
}

/// Focus the field before typing: after a button tap the field has lost focus,
/// and enterText on an unfocused field silently no-ops on web.
Future<void> _fill(WidgetTester tester, Key key, String value) async {
  final finder = find.byKey(key);
  await tester.ensureVisible(finder);
  await tester.tap(finder);
  await tester.pump();
  await tester.enterText(finder, value);
  await tester.pump();
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'provider journey through the real UI: login → OTP → onboarding → dashboard',
    (tester) async {
      // Fresh provider each run so the journey starts at onboarding.
      final phone = '0912${DateTime.now().millisecondsSinceEpoch % 10000000}'
          .padRight(11, '0')
          .substring(0, 11);

      await configureDependencies();
      // Web secure storage persists in localStorage — start signed out.
      await getIt<SecureStorageService>().clearSession();

      await tester.pumpWidget(const ProviderApp());
      await tester.pump();

      // ---------- SCREEN 1: LOGIN ----------
      await _pumpUntil(tester, find.text(AppStrings.loginTitle),
          reason: 'login screen');
      expect(find.text(AppStrings.loginSubtitle), findsOneWidget);

      // Validation: empty phone.
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.sendOtp));
      await tester.pump();
      expect(find.text(AppStrings.phoneRequired), findsOneWidget,
          reason: 'empty phone must show the required error');

      // Validation: malformed phone.
      await _fill(tester, const Key('login-phone'), '0812');
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.sendOtp));
      await tester.pump();
      expect(find.text(AppStrings.phoneInvalid), findsOneWidget,
          reason: 'bad phone must show the invalid error');

      // Happy path: send the OTP.
      await _fill(tester, const Key('login-phone'), phone);
      // Guard: prove the canonical number actually landed in the field.
      expect(
        tester.widget<TextField>(find.byType(TextField)).controller?.text,
        phone,
        reason: 'the phone must be in the field before submitting',
      );
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.sendOtp));

      // ---------- SCREEN 2: OTP ----------
      await _pumpUntil(tester, find.byType(Pinput), reason: 'OTP screen');
      // NB: otpTitle and the verify button share the same label ("تایید کد").
      expect(find.text(AppStrings.otpTitle), findsWidgets);
      expect(find.byType(Pinput), findsOneWidget);

      // Entering all 6 digits auto-submits.
      await tester.enterText(find.byType(Pinput), _otp);
      await tester.pump();

      // ---------- SCREEN 3: ONBOARDING · step 1 (business info) ----------
      await _pumpUntil(tester, find.text(AppStrings.businessInfoTitle),
          timeout: const Duration(seconds: 40),
          reason: 'onboarding step 1 (new provider must land here)');

      // Validation: required fields.
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));
      await _pumpUntil(tester, find.byType(SnackBar),
          reason: 'step 1 validation error');
      await _waitForSnackbarToClear(tester);

      await _fill(tester, const Key('onboarding-business-name'), 'سالن رابط کاربری');
      await _fill(tester, const Key('onboarding-owner-first-name'), 'رضا');
      await _fill(tester, const Key('onboarding-owner-last-name'), 'محمدی');
      await _fill(tester, const Key('onboarding-phone'), phone);
      await _fill(tester, const Key('onboarding-email'), 'ui@booksy.test');
      // Required by the backend validator (BusinessDescription .NotEmpty).
      await _fill(tester, const Key('onboarding-description'), 'سالن اصلاح مردانه');
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));

      // ---------- step 2 (category) ----------
      await _pumpUntil(tester, find.text(AppStrings.categoryTitle),
          reason: 'onboarding step 2');

      // Validation: nothing selected.
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));
      await _pumpUntil(tester, find.byType(SnackBar),
          reason: 'step 2 validation error');
      await _waitForSnackbarToClear(tester);

      await _tap(tester, find.byKey(const Key('category-barbershop')));
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));

      // ---------- step 3 (location → creates the draft) ----------
      await _pumpUntil(tester, find.text(AppStrings.locationTitle),
          reason: 'onboarding step 3');

      // Validation: address + city required.
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));
      await _pumpUntil(tester, find.byType(SnackBar),
          reason: 'step 3 validation error');
      await _waitForSnackbarToClear(tester);

      await _fill(tester, const Key('onboarding-address-line1'), 'خیابان ولیعصر');
      await _fill(tester, const Key('onboarding-city'), 'تهران');
      await _fill(tester, const Key('onboarding-province'), 'تهران');
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));

      // ---------- step 4 (services) ----------
      await _pumpUntil(tester, find.text(AppStrings.servicesTitle),
          timeout: const Duration(seconds: 40),
          reason: 'onboarding step 4 (draft must be created on the server)');
      expect(find.text(AppStrings.noServicesYet), findsOneWidget);

      // Validation: at least one service.
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));
      await _pumpUntil(tester, find.byType(SnackBar),
          reason: 'step 4 validation error');
      await _waitForSnackbarToClear(tester);

      // Add a service through the dialog.
      await _tap(tester, find.byKey(const Key('onboarding-add-service')));
      await _pumpUntil(tester, find.byKey(const Key('service-name')),
          reason: 'service dialog');
      await _fill(tester, const Key('service-name'), 'اصلاح مو');
      await _fill(tester, const Key('service-duration'), '45');
      await _fill(tester, const Key('service-price'), '250000');
      await _tap(tester, find.byKey(const Key('service-save')));

      await _pumpUntil(tester, find.text('اصلاح مو'),
          reason: 'service added to the list');
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));

      // ---------- step 5 (working hours) ----------
      await _pumpUntil(tester, find.text(AppStrings.hoursTitle),
          timeout: const Duration(seconds: 40),
          reason: 'onboarding step 5 (services must be saved)');
      // Toggle Friday closed, exercising the day switch.
      await _tap(tester, find.byKey(const Key('day-toggle-5')));
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.next));

      // ---------- step 6 (gallery, skippable) ----------
      await _pumpUntil(tester, find.text(AppStrings.galleryTitle),
          timeout: const Duration(seconds: 40),
          reason: 'onboarding step 6 (hours must be saved)');
      await _tap(tester, find.widgetWithText(FilledButton, AppStrings.skip));

      // ---------- step 7 (preview) ----------
      await _pumpUntil(tester, find.text(AppStrings.previewSubtitle),
          reason: 'onboarding step 7');
      // The review must reflect what we entered.
      expect(find.text('سالن رابط کاربری'), findsOneWidget);
      expect(find.text('آرایشگاه مردانه'), findsOneWidget);
      expect(find.text('تهران'), findsWidgets);

      await _tap(
        tester,
        find.widgetWithText(FilledButton, AppStrings.confirmAndSubmit),
      );

      // ---------- step 8 (completion) ----------
      await _pumpUntil(tester, find.text(AppStrings.completionTitle),
          timeout: const Duration(seconds: 40),
          reason: 'registration completion');

      await _tap(tester, find.byKey(const Key('onboarding-go-to-dashboard')));

      // ---------- SCREEN 4: DASHBOARD ----------
      // Provider status is re-fetched (PendingVerification) → router lets us in.
      await _pumpUntil(tester, find.text(AppStrings.dashboardWelcome),
          timeout: const Duration(seconds: 40),
          reason: 'dashboard after onboarding');
      expect(find.text(AppStrings.dashboardTitle), findsOneWidget);
    },
    timeout: const Timeout(Duration(minutes: 5)),
  );
}
