import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:pull_to_refresh/pull_to_refresh.dart';
import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../bloc/home_bloc.dart';
import '../bloc/home_event.dart';
import '../bloc/home_state.dart';

class HomePageNew extends StatefulWidget {
  const HomePageNew({super.key});

  @override
  State<HomePageNew> createState() => _HomePageNewState();
}

class _HomePageNewState extends State<HomePageNew> {
  final RefreshController _refreshController = RefreshController();

  @override
  void initState() {
    super.initState();
    // Load data on init
    context.read<HomeBloc>().add(const LoadHomeData());
  }

  @override
  void dispose() {
    _refreshController.dispose();
    super.dispose();
  }

  void _onRefresh() {
    context.read<HomeBloc>().add(const RefreshHomeData());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        backgroundColor: AppColors.background,
        elevation: 0,
        toolbarHeight: 0, // Remove app bar completely
      ),
      body: BlocConsumer<HomeBloc, HomeState>(
        listener: (context, state) {
          if (state is HomeLoaded || state is HomeError) {
            _refreshController.refreshCompleted();
          }
        },
        builder: (context, state) {
          if (state is HomeLoading) {
            return const Center(
              child: CircularProgressIndicator(
                valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
              ),
            );
          }

          if (state is HomeError) {
            return _ErrorView(
              message: state.message,
              onRetry: () {
                context.read<HomeBloc>().add(const LoadHomeData());
              },
            );
          }

          if (state is HomeLoaded) {
            return SmartRefresher(
              controller: _refreshController,
              onRefresh: _onRefresh,
              header: const WaterDropHeader(
                waterDropColor: AppColors.primary,
              ),
              child: SingleChildScrollView(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SizedBox(height: 16.h),
                    // Search Bar
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 16.w),
                      child: GestureDetector(
                        onTap: () {
                          // TODO: Navigate to explore/search page
                        },
                        child: Container(
                          padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
                          decoration: BoxDecoration(
                            color: AppColors.surface,
                            borderRadius: BorderRadius.circular(24.r), // More rounded for pill shape
                            border: Border.all(color: AppColors.border),
                          ),
                          child: Row(
                            children: [
                              Icon(
                                Icons.search,
                                color: AppColors.textTertiary,
                                size: 20.w,
                              ),
                              SizedBox(width: 12.w),
                              Expanded(
                                child: Text(
                                  'جستجوی سالن، آرایشگاه، خدمات زیبایی...',
                                  style: AppTextStyles.body.copyWith(
                                    color: AppColors.textTertiary,
                                    fontSize: 15.sp,
                                  ),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    SizedBox(height: 12.h), // Reduced from 24.h
                    // Visited and Favorites Section
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
                      child: Row(
                        children: [
                          Text(
                            'بازدید شده و علاقه‌مندی‌ها',
                            style: AppTextStyles.h3.copyWith(
                              color: AppColors.textPrimary, // Changed from textSecondary for stronger hierarchy
                            ),
                          ),
                          SizedBox(width: 6.w),
                          GestureDetector(
                            onTap: () {
                              // TODO: Show info about visited and favorites
                            },
                            child: Icon(
                              Icons.info_outline,
                              size: 18.w, // Reduced from 20.w
                              color: AppColors.textTertiary, // Lighter color
                            ),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(height: 8.h),
                    // Visited and Favorites Content
                    _buildRecentlyVisitedAndFavorites(
                      state.recentlyVisitedProviders,
                      state.favoriteProviders,
                    ),
                    SizedBox(height: 24.h),
                  ],
                ),
              ),
            );
          }

          return const SizedBox.shrink();
        },
      ),
    );
  }

  Widget _buildRecentlyVisitedAndFavorites(
    List recentlyVisited,
    List favorites,
  ) {
    final hasData = recentlyVisited.isNotEmpty || favorites.isNotEmpty;

    if (!hasData) {
      // Empty state
      return Container(
        height: 200.h,
        margin: EdgeInsets.symmetric(horizontal: 16.w),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(12.r),
          boxShadow: [
            BoxShadow(
              color: AppColors.shadowLight,
              offset: const Offset(0, 1),
              blurRadius: 3,
              spreadRadius: 0,
            ),
          ],
        ),
        child: Center(
          child: Text(
            'هنوز سالنی را بازدید نکرده‌اید',
            style: AppTextStyles.body.copyWith(
              color: AppColors.textSecondary,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
      );
    }

    // Display horizontal scrollable list
    return SizedBox(
      height: 140.h,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        padding: EdgeInsets.symmetric(horizontal: 16.w),
        itemCount: recentlyVisited.length + favorites.length,
        itemBuilder: (context, index) {
          if (index < recentlyVisited.length) {
            final provider = recentlyVisited[index];
            return _ProviderCard(
              providerName: provider.providerName,
              city: provider.city,
              rating: provider.averageRating,
              isRecent: true,
            );
          } else {
            final provider = favorites[index - recentlyVisited.length];
            return _ProviderCard(
              providerName: provider.providerName,
              city: provider.city,
              rating: provider.averageRating,
              isRecent: false,
            );
          }
        },
      ),
    );
  }
}

class _ProviderCard extends StatelessWidget {
  final String providerName;
  final String? city;
  final double? rating;
  final bool isRecent;

  const _ProviderCard({
    required this.providerName,
    this.city,
    this.rating,
    required this.isRecent,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 160.w,
      margin: EdgeInsets.only(left: 12.w),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(12.r),
        boxShadow: [
          BoxShadow(
            color: AppColors.shadowLight,
            offset: const Offset(0, 1),
            blurRadius: 3,
            spreadRadius: 0,
          ),
        ],
      ),
      child: Padding(
        padding: EdgeInsets.all(12.w),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Row(
              children: [
                Container(
                  width: 40.w,
                  height: 40.w,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Icon(
                    Icons.store,
                    size: 20.w,
                    color: AppColors.primary,
                  ),
                ),
                const Spacer(),
                if (isRecent)
                  Container(
                    padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
                    decoration: BoxDecoration(
                      color: AppColors.info.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(4.r),
                    ),
                    child: Text(
                      'اخیر',
                      style: AppTextStyles.small.copyWith(
                        color: AppColors.info,
                        fontSize: 10.sp,
                      ),
                    ),
                  )
                else
                  Icon(
                    Icons.favorite,
                    size: 16.w,
                    color: AppColors.error,
                  ),
              ],
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  providerName,
                  style: AppTextStyles.bodyMedium.copyWith(
                    fontSize: 14.sp,
                    color: AppColors.textPrimary,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (city != null) ...[
                  SizedBox(height: 4.h),
                  Row(
                    children: [
                      Icon(
                        Icons.location_on,
                        size: 12.w,
                        color: AppColors.textTertiary,
                      ),
                      SizedBox(width: 2.w),
                      Expanded(
                        child: Text(
                          city!,
                          style: AppTextStyles.small.copyWith(
                            color: AppColors.textTertiary,
                            fontSize: 11.sp,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                ],
                if (rating != null) ...[
                  SizedBox(height: 4.h),
                  Row(
                    children: [
                      Icon(
                        Icons.star,
                        size: 12.w,
                        color: Colors.amber,
                      ),
                      SizedBox(width: 2.w),
                      Text(
                        rating!.toStringAsFixed(1),
                        style: AppTextStyles.small.copyWith(
                          color: AppColors.textSecondary,
                          fontSize: 11.sp,
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const _ErrorView({
    required this.message,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.all(24.w),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.error_outline,
              size: 64.w,
              color: AppColors.error,
            ),
            SizedBox(height: 16.h),
            Text(
              message,
              style: AppTextStyles.body.copyWith(
                color: AppColors.textSecondary,
              ),
              textAlign: TextAlign.center,
            ),
            SizedBox(height: 24.h),
            ElevatedButton(
              onPressed: onRetry,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                ),
                padding: EdgeInsets.symmetric(horizontal: 32.w, vertical: 14.h),
                elevation: 0,
              ),
              child: Text(
                'تلاش مجدد',
                style: AppTextStyles.button.copyWith(color: Colors.white),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
