import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../../../core/utils/date_formatter.dart';
import '../../../../core/utils/price_formatter.dart';
import '../../domain/entities/upcoming_booking.dart';

class UpcomingBookingsWidget extends StatelessWidget {
  final List<UpcomingBooking> bookings;
  final Function(UpcomingBooking) onBookingTap;
  final Function(UpcomingBooking) onDirectionsTap;
  final VoidCallback onViewAllTap;

  const UpcomingBookingsWidget({
    super.key,
    required this.bookings,
    required this.onBookingTap,
    required this.onDirectionsTap,
    required this.onViewAllTap,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: EdgeInsets.symmetric(horizontal: 16.w),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'رزروهای پیش‌رو',
                style: AppTextStyles.h3,
              ),
              if (bookings.isNotEmpty)
                TextButton(
                  onPressed: onViewAllTap,
                  child: Text(
                    'مشاهده همه',
                    style: AppTextStyles.buttonSmall.copyWith(
                      color: AppColors.primary,
                    ),
                  ),
                ),
            ],
          ),
        ),
        SizedBox(height: 12.h),
        if (bookings.isEmpty)
          _EmptyBookingsState()
        else
          ...bookings.map((booking) => _BookingCard(
                booking: booking,
                onTap: () => onBookingTap(booking),
                onDirectionsTap: () => onDirectionsTap(booking),
              )),
      ],
    );
  }
}

class _BookingCard extends StatelessWidget {
  final UpcomingBooking booking;
  final VoidCallback onTap;
  final VoidCallback onDirectionsTap;

  const _BookingCard({
    required this.booking,
    required this.onTap,
    required this.onDirectionsTap,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: EdgeInsets.symmetric(horizontal: 16.w, vertical: 6.h),
      padding: EdgeInsets.all(16.w),
      decoration: BoxDecoration(
        color: AppColors.background,
        border: Border.all(color: AppColors.border),
        borderRadius: BorderRadius.circular(16.r),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Provider and Service
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      booking.providerName,
                      style: AppTextStyles.h3,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    SizedBox(height: 4.h),
                    Text(
                      booking.serviceName,
                      style: AppTextStyles.caption,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
              // Status Badge
              Container(
                padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 6.h),
                decoration: BoxDecoration(
                  color: _getStatusColor(booking.status).withOpacity(0.1),
                  borderRadius: BorderRadius.circular(8.r),
                ),
                child: Text(
                  _getStatusText(booking.status),
                  style: AppTextStyles.small.copyWith(
                    color: _getStatusColor(booking.status),
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
          SizedBox(height: 12.h),
          // Date and Time
          Row(
            children: [
              Icon(Icons.calendar_today, size: 16.w, color: AppColors.textSecondary),
              SizedBox(width: 8.w),
              Text(
                booking.dateTime.formatPersianFull(),
                style: AppTextStyles.caption,
              ),
            ],
          ),
          SizedBox(height: 8.h),
          Row(
            children: [
              Icon(Icons.access_time, size: 16.w, color: AppColors.textSecondary),
              SizedBox(width: 8.w),
              Text(
                booking.dateTime.formatPersianTime(),
                style: AppTextStyles.caption,
              ),
            ],
          ),
          SizedBox(height: 8.h),
          // Price
          Row(
            children: [
              Icon(Icons.payments, size: 16.w, color: AppColors.textSecondary),
              SizedBox(width: 8.w),
              Text(
                booking.price.formatPrice(),
                style: AppTextStyles.caption.copyWith(
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          SizedBox(height: 12.h),
          // Action Buttons
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: onTap,
                  style: OutlinedButton.styleFrom(
                    side: BorderSide(color: AppColors.primary, width: 1.5),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12.r),
                    ),
                    padding: EdgeInsets.symmetric(vertical: 12.h),
                  ),
                  child: Text(
                    'مشاهده جزئیات',
                    style: AppTextStyles.buttonSmall.copyWith(
                      color: AppColors.primary,
                    ),
                  ),
                ),
              ),
              if (booking.providerLat != null && booking.providerLng != null) ...[
                SizedBox(width: 12.w),
                OutlinedButton.icon(
                  onPressed: onDirectionsTap,
                  icon: Icon(Icons.directions, size: 18.w, color: AppColors.primary),
                  label: Text(
                    'مسیریابی',
                    style: AppTextStyles.buttonSmall.copyWith(
                      color: AppColors.primary,
                    ),
                  ),
                  style: OutlinedButton.styleFrom(
                    side: BorderSide(color: AppColors.primary, width: 1.5),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12.r),
                    ),
                    padding: EdgeInsets.symmetric(vertical: 12.h, horizontal: 16.w),
                  ),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return AppColors.success;
      case 'pending':
        return AppColors.warning;
      case 'cancelled':
        return AppColors.error;
      default:
        return AppColors.info;
    }
  }

  String _getStatusText(String status) {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return 'تایید شده';
      case 'pending':
        return 'در انتظار تایید';
      case 'cancelled':
        return 'لغو شده';
      default:
        return status;
    }
  }
}

class _EmptyBookingsState extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      margin: EdgeInsets.symmetric(horizontal: 16.w),
      padding: EdgeInsets.all(32.w),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16.r),
      ),
      child: Column(
        children: [
          Icon(
            Icons.calendar_today_outlined,
            size: 48.w,
            color: AppColors.textTertiary,
          ),
          SizedBox(height: 16.h),
          Text(
            'شما نوبت آینده‌ای ندارید',
            style: AppTextStyles.body.copyWith(
              color: AppColors.textSecondary,
            ),
            textAlign: TextAlign.center,
          ),
          SizedBox(height: 16.h),
          ElevatedButton(
            onPressed: () {
              // Navigate to search
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12.r),
              ),
              padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
              elevation: 0,
            ),
            child: Text(
              'رزرو نوبت جدید',
              style: AppTextStyles.button.copyWith(color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }
}
