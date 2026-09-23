import 'package:booksy_customer_app/config/theme/app_text_styles.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/features/search/presentation/widgets/map_provider_card.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// MapProviderCard.bandHeight sizes the map carousel from AppTextStyles.bodySemibold (the name) and
/// AppTextStyles.small (meta line and address), while the card itself draws with the theme's titleMedium and
/// bodySmall. The band is only right while the theme maps those roles to those tokens; if either side moves, the
/// card clips inside a band that is too short.
void main() {
  test('the card is drawn with the text tokens its band is sized from', () {
    final text = AppTheme.light.textTheme;
    expect(text.titleMedium!.fontSize, AppTextStyles.bodySemibold.fontSize);
    expect(text.titleMedium!.height, AppTextStyles.bodySemibold.height);
    expect(text.bodySmall!.fontSize, AppTextStyles.small.fontSize);
    expect(text.bodySmall!.height, AppTextStyles.small.height);
  });

  test('the band grows with the text scale', () {
    expect(
      MapProviderCard.bandHeight(const TextScaler.linear(1.3)),
      greaterThan(MapProviderCard.bandHeight(TextScaler.noScaling)),
    );
  });
}
