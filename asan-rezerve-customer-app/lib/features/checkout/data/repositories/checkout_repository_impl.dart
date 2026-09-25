import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/dio_failure_mapper.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/checkout_entities.dart';
import '../../domain/repositories/checkout_repository.dart';
import '../datasources/checkout_remote_datasource.dart';

class CheckoutRepositoryImpl implements CheckoutRepository {
  final CheckoutRemoteDataSource remoteDataSource;

  CheckoutRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, BookingPaymentSnapshot>> getBookingPaymentSnapshot(String bookingId) async {
    try {
      final json = await remoteDataSource.getBooking(bookingId);
      return Right(_parseBookingSnapshot(bookingId, json));
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) {
        return const Left(NotFoundFailure(AppStrings.genericError));
      }
      return Left(mapDioFailure(e));
    } catch (_) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, PaymentIntent>> createPayment({
    required String bookingId,
    required String providerId,
    required double amount,
    required String idempotencyKey,
    String? description,
    String? mobile,
    String? email,
  }) async {
    try {
      final json = await remoteDataSource.createZarinPalPayment(
        bookingId: bookingId,
        providerId: providerId,
        amount: amount,
        idempotencyKey: idempotencyKey,
        description: description,
        mobile: mobile,
        email: email,
      );

      // The server reports gateway refusal in-band (isSuccessful=false) rather than as an HTTP error.
      final successful = json['isSuccessful'] as bool? ?? json['success'] as bool? ?? true;
      final authority = (json['authority'] ?? json['Authority']) as String? ?? '';
      final paymentUrl = (json['paymentUrl'] ?? json['PaymentUrl']) as String? ?? '';

      if (!successful || authority.isEmpty || paymentUrl.isEmpty) {
        final message = json['errorMessage'] as String? ?? json['message'] as String? ?? AppStrings.genericError;
        return Left(GatewayFailure(message));
      }

      return Right(PaymentIntent(
        authority: authority,
        paymentUrl: paymentUrl,
        paymentId: (json['paymentId'] ?? json['PaymentId'])?.toString(),
      ));
    } on DioException catch (e) {
      // 409 = the server's idempotency reservation rejected a duplicate/in-flight request. Never retry blindly:
      // the caller must re-read state, because the original request may already have charged.
      if (e.response?.statusCode == 409) {
        return const Left(DuplicateRequestFailure(AppStrings.genericError));
      }
      return Left(mapDioFailure(e));
    } catch (_) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, PaymentStatusResult>> verifyPayment({
    required String authority,
    String status = 'OK',
    String? idempotencyKey,
  }) async {
    try {
      final json = await remoteDataSource.verifyZarinPalPayment(
        authority: authority,
        status: status,
        idempotencyKey: idempotencyKey,
      );
      return Right(_parseVerifyResult(json));
    } on DioException catch (e) {
      // The verify endpoint answers "not paid" with a 400; that is a definitive *unpaid* answer, not a transport
      // error, so surface it as a payment status rather than a failure the UI would offer to retry blindly.
      if (e.response?.statusCode == 400) {
        return Right(PaymentStatusResult(
          status: 'Failed',
          failureReason: _extractMessage(e.response?.data) ?? AppStrings.genericError,
        ));
      }
      return Left(mapDioFailure(e));
    } catch (_) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, PaymentStatusResult>> getPayment(String paymentId) async {
    try {
      final json = await remoteDataSource.getPayment(paymentId);
      return Right(PaymentStatusResult(
        paymentId: (json['paymentId'] ?? json['id'])?.toString() ?? paymentId,
        bookingId: (json['bookingId'])?.toString(),
        status: (json['status'] ?? json['paymentStatus'])?.toString() ?? 'Unknown',
        refNumber: (json['refNumber'] ?? json['refId'])?.toString(),
      ));
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) {
        return const Left(NotFoundFailure(AppStrings.genericError));
      }
      return Left(mapDioFailure(e));
    } catch (_) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  // ---------------------------------------------------------------- parsing

  static BookingPaymentSnapshot _parseBookingSnapshot(String bookingId, Map<String, dynamic> json) {
    final payment = (json['paymentInfo'] ?? json['PaymentInfo']) as Map<String, dynamic>? ?? const {};
    return BookingPaymentSnapshot(
      bookingId: (json['bookingId'] ?? json['id'])?.toString() ?? bookingId,
      bookingStatus: (json['status'] ?? json['bookingStatus'])?.toString() ?? 'Unknown',
      totalAmount: _toDouble(payment['totalAmount']),
      depositAmount: _toDouble(payment['depositAmount']),
      paidAmount: _toDouble(payment['paidAmount']),
      paymentStatus: (payment['status'])?.toString() ?? 'Unknown',
    );
  }

  static PaymentStatusResult _parseVerifyResult(Map<String, dynamic> json) {
    final successful = json['isSuccessful'] as bool? ?? json['success'] as bool? ?? false;
    final reported = (json['paymentStatus'] ?? json['status'])?.toString();
    return PaymentStatusResult(
      paymentId: (json['paymentId'])?.toString(),
      bookingId: (json['bookingId'])?.toString(),
      // Trust an explicit server status when present; otherwise derive from the success flag.
      status: reported ?? (successful ? 'Paid' : 'Failed'),
      refNumber: (json['refNumber'] ?? json['refId'])?.toString(),
      failureReason: (json['failureReason'] ?? json['errorMessage'] ?? json['message'])?.toString(),
    );
  }

  static String? _extractMessage(dynamic data) {
    if (data is Map && data['message'] is String) return data['message'] as String;
    if (data is Map && data['errors'] is String) return data['errors'] as String;
    return null;
  }

  static double _toDouble(dynamic value) {
    if (value is num) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? 0;
    return 0;
  }
}
