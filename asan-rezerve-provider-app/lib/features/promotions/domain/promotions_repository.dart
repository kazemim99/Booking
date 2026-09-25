import 'package:dartz/dartz.dart';

import '../../../core/errors/failures.dart';
import 'promotion.dart';

/// The salon's own promotions and its participation in platform campaigns. Every call is scoped to the signed-in
/// salon; a refusal carries the server's Persian reason.
abstract class PromotionsRepository {
  Future<Either<Failure, List<Promotion>>> list();

  Future<Either<Failure, Promotion>> create(PromotionDraft draft);

  Future<Either<Failure, Promotion>> update(String promotionId, PromotionDraft draft);

  Future<Either<Failure, Promotion>> change(String promotionId, PromotionAction action);

  Future<Either<Failure, List<CampaignOffer>>> campaigns();

  Future<Either<Failure, CampaignOffer>> setJoined(String campaignId, {required bool join});
}
