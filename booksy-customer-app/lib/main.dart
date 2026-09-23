import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:go_router/go_router.dart';
import 'config/routes/app_router.dart';
import 'core/push/firebase_push_token_source.dart';
import 'core/push/push_message_router.dart';
import 'features/notifications/presentation/inbox_cubit.dart';
import 'config/theme/app_theme.dart';
import 'core/constants/app_strings.dart';
import 'core/di/injection.dart';
import 'core/widgets/app_viewport_frame.dart';
import 'features/auth/presentation/bloc/auth_bloc.dart';
import 'features/auth/presentation/bloc/auth_event.dart';
import 'features/home/presentation/bloc/home_bloc.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Configure dependency injection
  await configureDependencies();

  runApp(BooksyCustomerApp(
    authBloc: getIt<AuthBloc>()..add(const CheckAuthStatusEvent()),
  ));
}

class BooksyCustomerApp extends StatefulWidget {
  final AuthBloc authBloc;

  const BooksyCustomerApp({super.key, required this.authBloc});

  @override
  State<BooksyCustomerApp> createState() => _BooksyCustomerAppState();
}

class _BooksyCustomerAppState extends State<BooksyCustomerApp> {
  late final GoRouter _router = AppRouter.create(widget.authBloc);

  @override
  void initState() {
    super.initState();
    // Taps on push notifications route through the same router. A no-op on web and on builds without Firebase.
    PushMessageRouter.attach(
      source: getIt<FirebasePushTokenSource>(),
      router: _router,
      inbox: getIt<InboxCubit>(),
    );
  }

  @override
  void dispose() {
    _router.dispose();
    widget.authBloc.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ScreenUtilInit(
      designSize: const Size(375, 812),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) {
        return MultiBlocProvider(
          providers: [
            BlocProvider.value(value: widget.authBloc),
            BlocProvider(create: (context) => getIt<HomeBloc>()),
          ],
          child: MaterialApp.router(
            // On the web this is the browser tab's title once the app runs (index.html has the same text before).
            title: AppStrings.appDocumentTitle,
            debugShowCheckedModeBanner: false,
            // RTL Support for Persian/Arabic
            locale: const Locale('fa', 'IR'),
            supportedLocales: const [
              Locale('fa', 'IR'), // Persian
              Locale('en', 'US'), // English
            ],
            localizationsDelegates: const [
              GlobalMaterialLocalizations.delegate,
              GlobalWidgetsLocalizations.delegate,
              GlobalCupertinoLocalizations.delegate,
            ],
            builder: buildAppShell,
            theme: AppTheme.light,
            routerConfig: _router,
          ),
        );
      },
    );
  }
}

/// Wraps every route (MaterialApp.builder): right-to-left throughout, and phone-shaped on wide screens.
Widget buildAppShell(BuildContext context, Widget? child) {
  return Directionality(
    textDirection: TextDirection.rtl,
    child: AppViewportFrame(child: child!),
  );
}
