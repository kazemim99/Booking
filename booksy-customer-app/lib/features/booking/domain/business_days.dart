import 'entities/booking_entities.dart';

/// Which weekdays a salon is shut, read from its weekly hours.
///
/// [BusinessHour.dayOfWeek] reaches the app as a Persian day name (the repository turns the server's number into
/// one), spelled with or without the zero-width non-joiner — «سه‌شنبه» and «سه شنبه» are the same day — so names are
/// compared after dropping joiners, spaces and the Arabic forms of «ی» and «ک». English names are accepted too, in
/// case a payload ever carries them. A day that cannot be read is never treated as closed: offering a day the salon
/// turns out to be shut costs the customer one tap, hiding a day it is open costs the salon a booking.
class BusinessDays {
  BusinessDays._();

  /// [DateTime.weekday] values (Monday = 1 … Sunday = 7) the salon marks closed.
  static Set<int> closedWeekdays(List<BusinessHour> hours) => {
        for (final hour in hours)
          if (hour.isClosed)
            if (weekdayOf(hour.dayOfWeek) case final int weekday) weekday,
      };

  /// The [DateTime.weekday] a day name stands for, or null when it is not one.
  static int? weekdayOf(String name) => _weekdays[_normalise(name)];

  static String _normalise(String name) => name
      .replaceAll('‌', '')
      .replaceAll(RegExp(r'\s'), '')
      .replaceAll('ي', 'ی')
      .replaceAll('ك', 'ک')
      .toLowerCase();

  static const Map<String, int> _weekdays = {
    'شنبه': DateTime.saturday,
    'یکشنبه': DateTime.sunday,
    'دوشنبه': DateTime.monday,
    'سهشنبه': DateTime.tuesday,
    'چهارشنبه': DateTime.wednesday,
    'پنجشنبه': DateTime.thursday,
    'جمعه': DateTime.friday,
    'saturday': DateTime.saturday,
    'sunday': DateTime.sunday,
    'monday': DateTime.monday,
    'tuesday': DateTime.tuesday,
    'wednesday': DateTime.wednesday,
    'thursday': DateTime.thursday,
    'friday': DateTime.friday,
  };
}
