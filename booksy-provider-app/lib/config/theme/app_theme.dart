import 'package:flutter/material.dart';
import 'app_tokens.dart';

/// Application theme — Coliride visual language applied to the Booking
/// provider app: flat & shadowless, borders over shadows, blue chrome,
/// navy ink text, green success accent. RTL-first (Persian).
class AppTheme {
  AppTheme._();

  static ThemeData get light {
    const colorScheme = ColorScheme(
      brightness: Brightness.light,
      primary: AppColors.primary,
      onPrimary: Colors.white,
      primaryContainer: AppColors.success,
      onPrimaryContainer: Colors.white,
      secondary: AppColors.success,
      onSecondary: Colors.white,
      error: AppColors.danger,
      onError: Colors.white,
      surface: Colors.white,
      onSurface: AppColors.ink,
      outline: AppColors.borderFocus,
      outlineVariant: AppColors.divider,
    );

    OutlineInputBorder fieldBorder(Color color, double width) =>
        OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.field),
          borderSide: BorderSide(color: color, width: width),
        );

    final buttonShape = RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppRadius.button),
    );
    const buttonTextStyle = TextStyle(
      fontSize: AppDimens.buttonFontSize,
      fontWeight: FontWeight.bold,
      fontFamily: 'Vazir',
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      // Vazir — matches the Vue web app's Persian font.
      fontFamily: 'Vazir',
      scaffoldBackgroundColor: Colors.white,
      appBarTheme: const AppBarTheme(
        centerTitle: true,
        elevation: 0,
        backgroundColor: AppColors.appBar,
        foregroundColor: Colors.white,
      ),
      // White back icon on the blue chrome, supplied globally so screens
      // never override it per-page. Icons.arrow_back mirrors under RTL.
      actionIconTheme: ActionIconThemeData(
        backButtonIconBuilder: (context) =>
            const Icon(Icons.arrow_back, color: Colors.white),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size.fromHeight(AppDimens.buttonHeight),
          elevation: 0,
          shape: buttonShape,
          textStyle: buttonTextStyle,
          disabledBackgroundColor: AppColors.disabled,
          disabledForegroundColor: Colors.white,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size.fromHeight(AppDimens.buttonHeight),
          shape: buttonShape,
          textStyle: buttonTextStyle,
          foregroundColor: AppColors.ink,
          side: const BorderSide(color: AppColors.borderFocus, width: 1.5),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          shape: buttonShape,
          foregroundColor: AppColors.ink,
          textStyle: const TextStyle(
            fontWeight: FontWeight.bold,
            fontFamily: 'Vazir',
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        border: fieldBorder(AppColors.border, AppDimens.inputBorderWidth),
        enabledBorder:
            fieldBorder(AppColors.border, AppDimens.inputBorderWidth),
        focusedBorder:
            fieldBorder(AppColors.borderFocus, AppDimens.inputFocusBorderWidth),
        errorBorder: fieldBorder(AppColors.danger, AppDimens.inputBorderWidth),
        focusedErrorBorder:
            fieldBorder(AppColors.danger, AppDimens.inputFocusBorderWidth),
        disabledBorder:
            fieldBorder(AppColors.divider, AppDimens.inputBorderWidth),
        labelStyle: const TextStyle(
          color: AppColors.ink,
          fontSize: AppDimens.fieldLabelFontSize,
          fontWeight: FontWeight.bold,
        ),
        hintStyle: const TextStyle(color: AppColors.hint),
        helperMaxLines: 2,
        prefixIconColor: AppColors.icon,
        suffixIconColor: AppColors.icon,
        iconColor: AppColors.icon,
      ),
      checkboxTheme: CheckboxThemeData(
        side: const BorderSide(color: AppColors.borderFocus, width: 2),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(5),
        ),
        fillColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? AppColors.success
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
              ? AppColors.success
              : AppColors.border,
        ),
        trackOutlineColor: const WidgetStatePropertyAll(Colors.transparent),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.card),
          side: const BorderSide(color: AppColors.border),
        ),
      ),
      dialogTheme: DialogThemeData(
        elevation: 0,
        backgroundColor: Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.panel),
        ),
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(AppRadius.bottomSheet),
          ),
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.snackbar),
        ),
      ),
      dividerTheme: const DividerThemeData(color: AppColors.divider),
    );
  }
}
