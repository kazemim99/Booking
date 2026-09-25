import 'package:dartz/dartz.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/errors/failures.dart';
import '../../domain/entities/promotion_entities.dart';
import '../../domain/repositories/booking_repository.dart';

enum QuoteStatus { loading, loaded, failed }

class PriceQuoteState extends Equatable {
  final QuoteStatus status;
  final PriceQuote? quote;

  /// The code the booking will carry: set only when the server applied it.
  final String? appliedCode;
  final bool applying;

  /// What the customer is told about the code they typed; [messageOk] says whether it is good news.
  final String? message;
  final bool messageOk;

  const PriceQuoteState({
    this.status = QuoteStatus.loading,
    this.quote,
    this.appliedCode,
    this.applying = false,
    this.message,
    this.messageOk = false,
  });

  PriceQuoteState copyWith({
    QuoteStatus? status,
    PriceQuote? quote,
    String? Function()? appliedCode,
    bool? applying,
    String? Function()? message,
    bool? messageOk,
  }) =>
      PriceQuoteState(
        status: status ?? this.status,
        quote: quote ?? this.quote,
        appliedCode: appliedCode != null ? appliedCode() : this.appliedCode,
        applying: applying ?? this.applying,
        message: message != null ? message() : this.message,
        messageOk: messageOk ?? this.messageOk,
      );

  @override
  List<Object?> get props => [status, quote, appliedCode, applying, message, messageOk];
}

/// The confirm step's price, as the server computes it (add-discounts-and-campaigns): subtotal, the one discount
/// applied, total, and what happened to a code. A failed quote falls back to the list price, never a blank.
class PriceQuoteCubit extends Cubit<PriceQuoteState> {
  final BookingRepository _repository;
  final String providerId;
  final List<String> serviceIds;
  final DateTime startTime;

  /// Bumped per request so a slower, older answer never lands over a newer one.
  int _requestId = 0;

  PriceQuoteCubit(
    this._repository, {
    required this.providerId,
    required this.serviceIds,
    required this.startTime,
  }) : super(const PriceQuoteState());

  Future<void> load() async {
    final id = ++_requestId;
    emit(state.copyWith(status: QuoteStatus.loading));
    final result = await _quote(state.appliedCode);
    if (isClosed || id != _requestId) return;
    result.fold(
      (_) => emit(state.copyWith(status: QuoteStatus.failed)),
      (quote) => emit(state.copyWith(status: QuoteStatus.loaded, quote: quote)),
    );
  }

  Future<void> applyCode(String raw, {required String failedMessage}) async {
    final code = raw.trim();
    if (code.isEmpty) return;
    final id = ++_requestId;
    emit(state.copyWith(applying: true, message: () => null));
    final result = await _quote(code);
    if (isClosed || id != _requestId) return;
    result.fold(
      (_) => emit(state.copyWith(applying: false, message: () => failedMessage, messageOk: false)),
      (quote) => emit(state.copyWith(
        status: QuoteStatus.loaded,
        quote: quote,
        applying: false,
        // Only a code the server applied rides on the booking; a worse or refused one would change nothing.
        appliedCode: () => quote.codeOutcome == CodeOutcome.applied ? code : null,
        message: () => quote.codeMessage,
        messageOk: quote.codeOutcome == CodeOutcome.applied || quote.codeOutcome == CodeOutcome.betterOfferApplied,
      )),
    );
  }

  Future<void> clearCode() async {
    emit(state.copyWith(appliedCode: () => null, message: () => null));
    await load();
  }

  Future<Either<Failure, PriceQuote>> _quote(String? code) => _repository.quote(
        providerId: providerId,
        serviceIds: serviceIds,
        startTime: startTime,
        promotionCode: code,
      );
}
