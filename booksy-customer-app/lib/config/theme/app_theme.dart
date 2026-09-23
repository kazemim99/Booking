import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'app_colors.dart';
import 'app_text_styles.dart';
import 'app_tokens.dart';

/// Single source of visual truth for the app — aligned to the Provider app's
/// Coliride visual language: flat & shadowless, borders over shadows, blue
/// chrome, navy ink text, green success/selection accent. RTL-first (Persian).
///
/// Screens and feature widgets must consume Theme.of(context) or the shared
/// components in core/widgets — never AppColors/raw hex directly.
class AppTheme {
  AppTheme._();

  static ThemeData get light {
    const colorScheme = ColorScheme(
      brightness: Brightness.light,
      primary: AppColors.primary,
      onPrimary: Colors.white,
      primaryContainer: AppColors.primaryTint,
      onPrimaryContainer: Colors.white,
      secondary: AppColors.accent, // green — selection/success accent
      onSecondary: Colors.white,
      // Set explicitly: unset, Flutter falls back to `secondary` (the green accent) and the selected
      // segment / map notice / progress track became green with 2.38:1 text.
      secondaryContainer: AppColors.accentContainer,
      onSecondaryContainer: AppColors.onAccentContainer,
      // The AA red (6.47:1 with white). The coral AppColors.error stays for badges and fills.
      error: AppColors.errorText,
      onError: Colors.white,
      surface: AppColors.surface,
      onSurface: AppColors.textPrimary,
      onSurfaceVariant: AppColors.textSecondary,
      outline: AppColors.borderFocus,
      outlineVariant: AppColors.divider,
      surfaceContainerHighest: AppColors.surfaceSoft,
    );

    final textTheme = const TextTheme(
      displaySmall: AppTextStyles.h1,
      headlineMedium: AppTextStyles.h2,
      headlineSmall: AppTextStyles.h2,
      titleLarge: AppTextStyles.h3,
      titleMedium: AppTextStyles.bodySemibold,
      titleSmall: AppTextStyles.captionMedium,
      bodyLarge: AppTextStyles.body,
      bodyMedium: AppTextStyles.caption,
      bodySmall: AppTextStyles.small,
      labelLarge: AppTextStyles.button,
      labelMedium: AppTextStyles.buttonSmall,
      labelSmall: AppTextStyles.small,
    ).apply(fontFamily: AppTextStyles.fontFamily);

    OutlineInputBorder fieldBorder(Color color, double width) =>
        OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.field),
          borderSide: BorderSide(color: color, width: width),
        );

    final buttonShape = RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppRadius.button),
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      fontFamily: AppTextStyles.fontFamily,
      textTheme: textTheme,
      scaffoldBackgroundColor: AppColors.background,
      dividerColor: AppColors.divider,
      splashFactory: InkRipple.splashFactory,

      // Blue chrome, white content, flat, light status-bar icons over the blue.
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.appBar,
        foregroundColor: Colors.white,
        elevation: AppElevation.none,
        scrolledUnderElevation: AppElevation.none,
        centerTitle: true,
        titleTextStyle: TextStyle(
          fontFamily: AppTextStyles.fontFamily,
          fontSize: 18,
          fontWeight: AppTextStyles.bold,
          color: Colors.white,
          height: 1.4,
        ),
        iconTheme: IconThemeData(color: Colors.white),
        actionsIconTheme: IconThemeData(color: Colors.white),
        systemOverlayStyle: SystemUiOverlayStyle.light,
      ),
      // White RTL-mirroring back icon supplied globally so screens never
      // override it per-page.
      actionIconTheme: ActionIconThemeData(
        backButtonIconBuilder: (context) =>
            const Icon(Icons.arrow_back, color: Colors.white),
      ),

      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          disabledBackgroundColor: AppColors.border,
          disabledForegroundColor: AppColors.textTertiary,
          minimumSize: const Size(AppTouchTarget.min, AppTouchTarget.min),
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.sm,
          ),
          shape: buttonShape,
          textStyle: AppTextStyles.button,
          elevation: AppElevation.none,
        ),
      ),

      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          side: const BorderSide(color: AppColors.primary, width: 2),
          minimumSize: const Size(AppTouchTarget.min, AppTouchTarget.min),
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.sm,
          ),
          shape: buttonShape,
          textStyle: AppTextStyles.button,
        ),
      ),

      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.textPrimary,
          minimumSize: const Size(AppTouchTarget.min, AppTouchTarget.min),
          textStyle: AppTextStyles.buttonSmall.copyWith(
            fontWeight: AppTextStyles.bold,
          ),
        ),
      ),

      // Non-filled bordered inputs (Coliride): grey resting/focus borders,
      // darker red error border.
      inputDecorationTheme: InputDecorationTheme(
        filled: false,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        border: fieldBorder(AppColors.border, 1.9),
        enabledBorder: fieldBorder(AppColors.border, 1.9),
        focusedBorder: fieldBorder(AppColors.borderFocus, 2.1),
        errorBorder: fieldBorder(AppColors.inputErrorBorder, 1.9),
        focusedErrorBorder: fieldBorder(AppColors.inputErrorBorder, 2.1),
        disabledBorder: fieldBorder(AppColors.divider, 1.9),
        labelStyle: const TextStyle(
          fontFamily: AppTextStyles.fontFamily,
          color: AppColors.textPrimary,
          fontSize: 14,
          fontWeight: AppTextStyles.bold,
        ),
        // Hint ≥ 4.5:1 and icons ≥ 3:1 on white and on the soft pill fill — darker than the
        // provider app's Coliride values on purpose (see AppColors.hint).
        hintStyle: AppTextStyles.caption.copyWith(color: AppColors.hint),
        errorStyle: AppTextStyles.small.copyWith(color: AppColors.errorText),
        helperMaxLines: 2,
        prefixIconColor: AppColors.fieldIcon,
        suffixIconColor: AppColors.fieldIcon,
        iconColor: AppColors.fieldIcon,
      ),

      // Flat card: no shadow, separated by a border.
      cardTheme: CardThemeData(
        color: AppColors.surface,
        elevation: AppElevation.none,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.card),
          side: const BorderSide(color: AppColors.border),
        ),
      ),

      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: AppColors.surface,
        modalBackgroundColor: AppColors.surface,
        elevation: AppElevation.none,
        modalElevation: AppElevation.none,
        showDragHandle: true,
        dragHandleColor: AppColors.border,
        shape: RoundedRectangleBorder(
          borderRadius:
              BorderRadius.vertical(top: Radius.circular(AppRadius.bottomSheet)),
        ),
      ),

      // Flat Material nav bar (the shell draws AppBottomBar; this keeps any NavigationBar in step
      // with it: white labels in both states, pale-blue inactive icons at ≥ 3:1).
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: AppColors.appBar,
        indicatorColor: AppColors.navIndicator,
        height: 64,
        elevation: AppElevation.none,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            size: AppIconSize.md,
            color: states.contains(WidgetState.selected)
                ? AppColors.navIconActive
                : AppColors.navIconInactive,
          ),
        ),
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => AppTextStyles.small.copyWith(
            fontWeight: states.contains(WidgetState.selected)
                ? AppTextStyles.bold
                : AppTextStyles.regular,
            color: AppColors.navLabel,
          ),
        ),
      ),

      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: AppColors.textPrimary,
        contentTextStyle: AppTextStyles.caption.copyWith(color: Colors.white),
        actionTextColor: AppColors.accentOnDark,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.snackbar),
        ),
      ),

      chipTheme: ChipThemeData(
        backgroundColor: AppColors.surface,
        selectedColor: AppColors.primary,
        labelStyle: AppTextStyles.captionMedium,
        secondaryLabelStyle:
            AppTextStyles.captionMedium.copyWith(color: Colors.white),
        side: const BorderSide(color: AppColors.border),
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.sm,
          vertical: AppSpacing.xs,
        ),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.full),
        ),
      ),

      // Flat dialog: no shadow, panel radius.
      dialogTheme: DialogThemeData(
        backgroundColor: AppColors.surface,
        elevation: AppElevation.none,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.panel),
        ),
        titleTextStyle: AppTextStyles.h3,
        contentTextStyle: AppTextStyles.body,
      ),

      // Green selection controls (Coliride), in the AA green so the check and the thumb show.
      checkboxTheme: CheckboxThemeData(
        side: const BorderSide(color: AppColors.borderFocus, width: 2),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(5)),
        fillColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? AppColors.accentStrong
              : Colors.white,
        ),
      ),
      switchTheme: SwitchThemeData(
        thumbColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? Colors.white
              : AppColors.borderFocus,
        ),
        trackColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? AppColors.accentStrong
              : AppColors.border,
        ),
        trackOutlineColor: const WidgetStatePropertyAll(Colors.transparent),
      ),

      // Green active tab label + 2px green indicator over a hairline divider — the AA green
      // (5.48:1); the accent green was 2.38:1 on white.
      tabBarTheme: const TabBarThemeData(
        labelColor: AppColors.accentStrong,
        unselectedLabelColor: AppColors.textPrimary,
        labelStyle: TextStyle(
          fontFamily: AppTextStyles.fontFamily,
          fontSize: 16,
          fontWeight: AppTextStyles.bold,
        ),
        unselectedLabelStyle: TextStyle(
          fontFamily: AppTextStyles.fontFamily,
          fontSize: 16,
          fontWeight: AppTextStyles.bold,
        ),
        dividerColor: AppColors.border,
        indicatorSize: TabBarIndicatorSize.tab,
        indicator: BoxDecoration(
          border: Border(
            bottom: BorderSide(color: AppColors.accentStrong, width: 2),
          ),
        ),
      ),

      progressIndicatorTheme: const ProgressIndicatorThemeData(
        color: AppColors.primary,
      ),

      dividerTheme: const DividerThemeData(
        color: AppColors.divider,
        thickness: 1,
        space: 1,
      ),

      listTileTheme: const ListTileThemeData(
        contentPadding: EdgeInsets.symmetric(horizontal: AppSpacing.md),
        minVerticalPadding: AppSpacing.sm,
        titleTextStyle: AppTextStyles.bodyMedium,
        subtitleTextStyle: AppTextStyles.caption,
        iconColor: AppColors.textSecondary,
      ),
    );
  }
}
