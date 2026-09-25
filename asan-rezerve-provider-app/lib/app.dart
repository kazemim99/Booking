import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import 'config/routes/app_router.dart';
import 'config/theme/app_theme.dart';
import 'core/constants/app_strings.dart';
import 'core/di/injection.dart';
import 'core/push/firebase_push_token_source.dart';
import 'core/push/push_message_router.dart';
import 'features/notifications/presentation/inbox_cubit.dart';
import 'features/auth/presentation/bloc/auth_bloc.dart';
import 'features/auth/presentation/bloc/auth_event.dart';

/// Root widget. Provides the singleton [AuthBloc] to the whole tree and wires
/// the go_router that reacts to auth state.
class ProviderApp extends StatelessWidget {
  const ProviderApp({super.key});

  /// Lets a push that arrives while the app is on screen show a snackbar from outside the widget tree.
  static final _messenger = GlobalKey<ScaffoldMessengerState>();

  @override
  Widget build(BuildContext context) {
    final authBloc = getIt<AuthBloc>()..add(const AuthStatusChecked());
    final router = AppRouter.create(authBloc);

    // Taps on push notifications route through the same router. A no-op on builds without Firebase (on the web:
    // without the Firebase Web app's dart-defines).
    PushMessageRouter.attach(
      source: getIt<FirebasePushTokenSource>(),
      router: router,
      inbox: getIt<InboxCubit>(),
      messenger: _messenger,
    );

    return BlocProvider<AuthBloc>.value(
      value: authBloc,
      child: MaterialApp.router(
        scaffoldMessengerKey: _messenger,
        title: AppStrings.appName,
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light,
        routerConfig: router,
        locale: const Locale('fa'),
        supportedLocales: const [Locale('fa'), Locale('en')],
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        builder: (context, child) => Directionality(
          textDirection: TextDirection.rtl,
          child: child ?? const SizedBox.shrink(),
        ),
      ),
    );
  }
}
