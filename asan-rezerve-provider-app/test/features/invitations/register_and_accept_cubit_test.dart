import 'package:asan_rezerve_provider_app/core/errors/failures.dart';
import 'package:asan_rezerve_provider_app/features/invitations/domain/invitation_repository.dart';
import 'package:asan_rezerve_provider_app/features/invitations/domain/invitation_summary.dart';
import 'package:asan_rezerve_provider_app/features/invitations/presentation/register_and_accept_cubit.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepo extends Mock implements InvitationRepository {}

void main() {
  late _MockRepo repo;

  const summary = InvitationSummary(
    invitationId: 'inv-1',
    organizationId: 'org-1',
    organizationName: 'سالن رُز',
    maskedPhone: '••••••1234',
    status: 'Pending',
    isValid: true,
  );

  setUp(() {
    repo = _MockRepo();
    registerFallbackValue('');
  });

  RegisterAndAcceptCubit build() => RegisterAndAcceptCubit(repo, 'inv-1');

  test('load: enteringInfo when the summary resolves', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    final cubit = build();
    await cubit.load();
    expect(cubit.state.phase, RegisterAcceptPhase.enteringInfo);
    expect(cubit.state.summary, summary);
    await cubit.close();
  });

  test('load: notFound when the invitation does not exist', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(null));
    final cubit = build();
    await cubit.load();
    expect(cubit.state.phase, RegisterAcceptPhase.notFound);
    await cubit.close();
  });

  test('load: loadError surfaces a network/server failure', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Left(ServerFailure('خطا')));
    final cubit = build();
    await cubit.load();
    expect(cubit.state.phase, RegisterAcceptPhase.loadError);
    expect(cubit.state.error, 'خطا');
    await cubit.close();
  });

  test('sendOtp: verifyingOtp with the masked phone on success', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.sendOtp('inv-1'))
        .thenAnswer((_) async => const Right('••••••1234'));
    final cubit = build();
    await cubit.load();
    await cubit.sendOtp();
    expect(cubit.state.phase, RegisterAcceptPhase.verifyingOtp);
    expect(cubit.state.maskedPhone, '••••••1234');
    await cubit.close();
  });

  test('sendOtp: a failure returns to enteringInfo with the error surfaced',
      () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.sendOtp('inv-1'))
        .thenAnswer((_) async => const Left(ValidationFailure('دعوت منقضی شده')));
    final cubit = build();
    await cubit.load();
    await cubit.sendOtp();
    expect(cubit.state.phase, RegisterAcceptPhase.enteringInfo);
    expect(cubit.state.error, 'دعوت منقضی شده');
    await cubit.close();
  });

  test('register: registered on success', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.sendOtp('inv-1'))
        .thenAnswer((_) async => const Right('••••••1234'));
    when(() => repo.registerAndAccept(
          'inv-1',
          firstName: any(named: 'firstName'),
          lastName: any(named: 'lastName'),
          email: any(named: 'email'),
          otpCode: any(named: 'otpCode'),
        )).thenAnswer((_) async => const Right(null));
    final cubit = build();
    await cubit.load();
    await cubit.sendOtp();
    await cubit.register(
        firstName: 'Ali', lastName: 'Rezai', otpCode: '135790');
    expect(cubit.state.phase, RegisterAcceptPhase.registered);
    await cubit.close();
  });

  test('register: an invalid OTP surfaces otpError and stays on verifyingOtp',
      () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.sendOtp('inv-1'))
        .thenAnswer((_) async => const Right('••••••1234'));
    when(() => repo.registerAndAccept(
          'inv-1',
          firstName: any(named: 'firstName'),
          lastName: any(named: 'lastName'),
          email: any(named: 'email'),
          otpCode: any(named: 'otpCode'),
        )).thenAnswer((_) async => const Left(ValidationFailure('کد اشتباه است')));
    final cubit = build();
    await cubit.load();
    await cubit.sendOtp();
    await cubit.register(
        firstName: 'Ali', lastName: 'Rezai', otpCode: '000000');
    expect(cubit.state.phase, RegisterAcceptPhase.verifyingOtp);
    expect(cubit.state.otpError, 'کد اشتباه است');
    await cubit.close();
  });

  test('backToInfo: returns to enteringInfo and clears the OTP error', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.sendOtp('inv-1'))
        .thenAnswer((_) async => const Right('••••••1234'));
    final cubit = build();
    await cubit.load();
    await cubit.sendOtp();
    cubit.backToInfo();
    expect(cubit.state.phase, RegisterAcceptPhase.enteringInfo);
    expect(cubit.state.otpError, isNull);
    await cubit.close();
  });
}
