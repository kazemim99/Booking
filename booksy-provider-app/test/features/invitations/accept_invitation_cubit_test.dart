import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/invitations/domain/invitation_repository.dart';
import 'package:booksy_provider_app/features/invitations/domain/invitation_summary.dart';
import 'package:booksy_provider_app/features/invitations/presentation/accept_invitation_cubit.dart';
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

  setUp(() => repo = _MockRepo());

  AcceptInvitationCubit build() => AcceptInvitationCubit(repo, 'inv-1');

  test('load: ready when the summary resolves', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    final cubit = build();
    await cubit.load();
    expect(cubit.state.phase, AcceptPhase.ready);
    expect(cubit.state.summary, summary);
    await cubit.close();
  });

  test('load: notFound when the invitation does not exist', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(null));
    final cubit = build();
    await cubit.load();
    expect(cubit.state.phase, AcceptPhase.notFound);
    await cubit.close();
  });

  test('accept: becomes accepted on success', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.accept('inv-1'))
        .thenAnswer((_) async => const Right(null));
    final cubit = build();
    await cubit.load();
    await cubit.accept();
    expect(cubit.state.phase, AcceptPhase.accepted);
    await cubit.close();
  });

  test('accept: a 401 asks the user to log in and keeps the summary', () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.accept('inv-1'))
        .thenAnswer((_) async => const Left(AuthFailure('unauth')));
    final cubit = build();
    await cubit.load();
    await cubit.accept();
    expect(cubit.state.phase, AcceptPhase.ready);
    expect(cubit.state.needsLogin, isTrue);
    expect(cubit.state.summary, summary);
    await cubit.close();
  });

  test('accept: a domain refusal surfaces its message (self-invite / member)',
      () async {
    when(() => repo.fetchSummary('inv-1'))
        .thenAnswer((_) async => const Right(summary));
    when(() => repo.accept('inv-1'))
        .thenAnswer((_) async => const Left(ValidationFailure('شما عضو هستید')));
    final cubit = build();
    await cubit.load();
    await cubit.accept();
    expect(cubit.state.phase, AcceptPhase.ready);
    expect(cubit.state.error, 'شما عضو هستید');
    expect(cubit.state.needsLogin, isFalse);
    await cubit.close();
  });
}
