import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../../../auth/presentation/pages/login_page.dart';
import '../../../home/presentation/pages/home_page_new.dart';
import '../../../search/presentation/pages/explore_page.dart';
import '../../../bookings/presentation/pages/appointments_page.dart';

class MainNavigationPage extends StatefulWidget {
  const MainNavigationPage({super.key});

  @override
  State<MainNavigationPage> createState() => _MainNavigationPageState();
}

class _MainNavigationPageState extends State<MainNavigationPage> {
  int _currentIndex = 0;

  void _onTabTapped(int index) {
    setState(() {
      _currentIndex = index;
    });
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AuthBloc, AuthState>(
      builder: (context, authState) {
        final isAuthenticated = authState is Authenticated;

        // Build pages list - only profile page changes based on auth
        final List<Widget> pages = [
          const HomePageNew(),
          const ExplorePage(),
          const AppointmentsPage(),
          // Show LoginPage for guests, Profile for authenticated users
          isAuthenticated
              ? const Center(child: Text('Profile Page - TODO')) // TODO: Replace with actual ProfilePage
              : const LoginPage(),
        ];

        return Scaffold(
          body: IndexedStack(
            index: _currentIndex,
            children: pages,
          ),
          bottomNavigationBar: Container(
            decoration: const BoxDecoration(
              border: Border(
                top: BorderSide(
                  color: Color(0x14000000), // 8% black opacity
                  width: 1,
                ),
              ),
            ),
            child: BottomNavigationBar(
              currentIndex: _currentIndex,
              type: BottomNavigationBarType.fixed,
              backgroundColor: Colors.white,
              selectedItemColor: const Color(0xDE000000), // 87% black (textPrimary equivalent)
              unselectedItemColor: const Color(0x61000000), // 38% black (lighter)
              selectedLabelStyle: AppTextStyles.small.copyWith(
                fontSize: 12,
                fontWeight: FontWeight.w500,
              ),
              unselectedLabelStyle: AppTextStyles.small.copyWith(
                fontSize: 12,
                fontWeight: FontWeight.w400,
              ),
              iconSize: 22, // Reduced from default 24
              elevation: 0,
              items: [
                const BottomNavigationBarItem(
                  icon: Icon(Icons.home_outlined),
                  activeIcon: Icon(Icons.home),
                  label: 'خانه',
                ),
                const BottomNavigationBarItem(
                  icon: Icon(Icons.search),
                  label: 'جستجو',
                ),
                const BottomNavigationBarItem(
                  icon: Icon(Icons.calendar_today_outlined),
                  activeIcon: Icon(Icons.calendar_today),
                  label: 'نوبت‌ها',
                ),
                BottomNavigationBarItem(
                  icon: const Icon(Icons.person_outline),
                  activeIcon: const Icon(Icons.person),
                  label: isAuthenticated ? 'پروفایل' : 'پروفایل',
                ),
              ],
              onTap: _onTabTapped,
            ),
          ),
        );
      },
    );
  }
}
