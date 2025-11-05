// iranianHolidays.ts - Iranian public holidays based on Jalali calendar
// These holidays are fixed in the Jalali calendar and recur annually

import { jalaliToGregorian, gregorianToJalali } from './dateHelpers'

export interface IranianHoliday {
  jalaliMonth: number // 1-12
  jalaliDay: number // 1-31
  nameEn: string
  nameFa: string
  isOfficial: boolean // Official government holiday
}

/**
 * Official Iranian public holidays (fixed dates in Jalali calendar)
 * These dates remain the same every year in the Jalali calendar
 */
export const IRANIAN_PUBLIC_HOLIDAYS: IranianHoliday[] = [
  // Nowruz (Persian New Year) - 4 days
  {
    jalaliMonth: 1,
    jalaliDay: 1,
    nameEn: 'Nowruz (Persian New Year) - Day 1',
    nameFa: 'عید نوروز - روز اول',
    isOfficial: true
  },
  {
    jalaliMonth: 1,
    jalaliDay: 2,
    nameEn: 'Nowruz (Persian New Year) - Day 2',
    nameFa: 'عید نوروز - روز دوم',
    isOfficial: true
  },
  {
    jalaliMonth: 1,
    jalaliDay: 3,
    nameEn: 'Nowruz (Persian New Year) - Day 3',
    nameFa: 'عید نوروز - روز سوم',
    isOfficial: true
  },
  {
    jalaliMonth: 1,
    jalaliDay: 4,
    nameEn: 'Nowruz (Persian New Year) - Day 4',
    nameFa: 'عید نوروز - روز چهارم',
    isOfficial: true
  },
  // Sizdah Be-dar (Nature Day)
  {
    jalaliMonth: 1,
    jalaliDay: 13,
    nameEn: 'Sizdah Be-dar (Nature Day)',
    nameFa: 'سیزده به‌در (روز طبیعت)',
    isOfficial: true
  },
  // Death of Khomeini
  {
    jalaliMonth: 3,
    jalaliDay: 14,
    nameEn: 'Demise of Ayatollah Khomeini',
    nameFa: 'رحلت امام خمینی',
    isOfficial: true
  },
  // 15 Khordad Uprising
  {
    jalaliMonth: 3,
    jalaliDay: 15,
    nameEn: '15 Khordad Uprising',
    nameFa: 'قیام پانزده خرداد',
    isOfficial: true
  },
  // Islamic Revolution Day
  {
    jalaliMonth: 11,
    jalaliDay: 22,
    nameEn: 'Islamic Revolution Day',
    nameFa: 'پیروزی انقلاب اسلامی',
    isOfficial: true
  },
  // Oil Nationalization Day
  {
    jalaliMonth: 12,
    jalaliDay: 29,
    nameEn: 'Oil Nationalization Day',
    nameFa: 'روز ملی شدن صنعت نفت',
    isOfficial: true
  }
]

/**
 * Islamic holidays (based on Hijri calendar - these are approximate and need lunar calendar calculation)
 * Note: These dates shift in the Gregorian/Jalali calendar each year
 * For accurate implementation, you would need a Hijri calendar library
 * or an API that provides the exact dates for each year
 */
export const ISLAMIC_HOLIDAYS_NOTES = `
Islamic holidays shift each year in the Jalali calendar:
- Tasua (9th of Muharram)
- Ashura (10th of Muharram)
- Arbaeen (20th of Safar)
- Demise of Prophet Muhammad (28th of Safar)
- Martyrdom of Imam Reza (29th/30th of Safar)
- Birthday of Prophet Muhammad & Imam Sadiq (17th of Rabi' al-Awwal)
- Martyrdom of Imam Ali (21st of Ramadan)
- Eid al-Fitr (1st-2nd of Shawwal)
- Martyrdom of Imam Sadiq (25th of Shawwal)
- Eid al-Adha (10th of Dhu al-Hijjah)
- Eid al-Ghadir (18th of Dhu al-Hijjah)

For production use, integrate with a Hijri calendar API or library.
`

/**
 * Check if a given Gregorian date is an Iranian public holiday
 */
export function isIranianPublicHoliday(date: Date): IranianHoliday | null {
  const jalali = gregorianToJalali(date.getFullYear(), date.getMonth() + 1, date.getDate())

  const holiday = IRANIAN_PUBLIC_HOLIDAYS.find(
    h => h.jalaliMonth === jalali.jm && h.jalaliDay === jalali.jd
  )

  return holiday || null
}

/**
 * Get all Iranian public holidays for a given Gregorian year
 */
export function getIranianHolidaysForYear(gregorianYear: number): Array<{ date: Date; holiday: IranianHoliday }> {
  const holidays: Array<{ date: Date; holiday: IranianHoliday }> = []

  // We need to check multiple Jalali years since they don't align with Gregorian years
  // A Gregorian year typically spans two Jalali years
  const startDate = new Date(gregorianYear, 0, 1)
  const endDate = new Date(gregorianYear, 11, 31)

  const startJalali = gregorianToJalali(startDate.getFullYear(), 1, 1)
  const endJalali = gregorianToJalali(endDate.getFullYear(), 12, 31)

  // Check all holidays in the relevant Jalali years
  for (let jalaliYear = startJalali.jy; jalaliYear <= endJalali.jy + 1; jalaliYear++) {
    for (const holiday of IRANIAN_PUBLIC_HOLIDAYS) {
      try {
        const gregorian = jalaliToGregorian(jalaliYear, holiday.jalaliMonth, holiday.jalaliDay)
        const holidayDate = new Date(gregorian.gy, gregorian.gm - 1, gregorian.gd)

        // Only include if it falls within the Gregorian year
        if (holidayDate >= startDate && holidayDate <= endDate) {
          holidays.push({ date: holidayDate, holiday })
        }
      } catch (error) {
        // Skip invalid dates
        console.warn(`Invalid Jalali date: ${jalaliYear}/${holiday.jalaliMonth}/${holiday.jalaliDay}`)
      }
    }
  }

  return holidays.sort((a, b) => a.date.getTime() - b.date.getTime())
}

/**
 * Get all Iranian public holidays for a given month
 */
export function getIranianHolidaysForMonth(year: number, month: number): Array<{ date: Date; holiday: IranianHoliday }> {
  const startDate = new Date(year, month, 1)
  const endDate = new Date(year, month + 1, 0) // Last day of month

  const yearHolidays = getIranianHolidaysForYear(year)

  return yearHolidays.filter(({ date }) => date >= startDate && date <= endDate)
}
