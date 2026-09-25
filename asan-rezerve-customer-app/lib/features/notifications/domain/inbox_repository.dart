import 'package:dartz/dartz.dart';

import '../../../core/errors/failures.dart';
import 'inbox_item.dart';

/// The signed-in customer's notification inbox. Every call is scoped to the caller by the backend; there is no
/// shape of this contract that reads somebody else's notifications.
abstract class InboxRepository {
  Future<Either<Failure, InboxResult>> fetchPage({int pageNumber = 1, int pageSize = 20});

  /// Just the badge's number, without loading a page.
  Future<Either<Failure, int>> unreadCount();

  Future<Either<Failure, Unit>> markRead(String id);

  Future<Either<Failure, Unit>> markAllRead();
}
