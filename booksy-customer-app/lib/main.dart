import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'core/di/injection.dart';
import 'features/auth/presentation/bloc/auth_bloc.dart';
import 'features/auth/presentation/bloc/auth_event.dart';
import 'features/auth/presentation/pages/splash_page.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Configure dependency injection
  await configureDependencies();

  runApp(const BooksyCustomerApp());
}

class BooksyCustomerApp extends StatelessWidget {
  const BooksyCustomerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ScreenUtilInit(
      designSize: const Size(375, 812),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) {
        return MultiBlocProvider(
          providers: [
            BlocProvider(
              create: (context) => getIt<AuthBloc>()
                ..add(const CheckAuthStatusEvent()),
            ),
          ],
          child: MaterialApp(
            title: 'Booksy Customer',
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
            builder: (context, child) {
              return Directionality(
                textDirection: TextDirection.rtl,
                child: child!,
              );
            },
            theme: ThemeData(
              primarySwatch: Colors.purple,
              fontFamily: 'Vazir',
              textTheme: const TextTheme(
                bodyLarge: TextStyle(fontSize: 16),
                bodyMedium: TextStyle(fontSize: 14),
                bodySmall: TextStyle(fontSize: 12),
              ),
              useMaterial3: true,
            ),
            home: child,
          ),
        );
      },
      child: const SplashPage(),
    );
  }
}
