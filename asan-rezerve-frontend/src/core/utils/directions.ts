/**
 * «مسیریابی»: where a salon's coordinates can be opened for directions (reviews-and-reschedule-round2 item 5).
 *
 * First — only on a phone — the phone's own chooser of installed map apps: a `geo:` link on Android (the system asks
 * which installed app to use), Apple Maps on iOS (which hands off to the default maps app). Then the web routes of the
 * apps Iranians actually use: نشان, بلد, Google Maps, Waze. Pure, so the choice can be tested without a browser.
 */

export type MobilePlatform = 'android' | 'ios' | null

export interface DirectionsOption {
  id: 'device' | 'neshan' | 'balad' | 'google' | 'waze'
  label: string
  url: string
}

export function detectMobilePlatform(userAgent: string): MobilePlatform {
  if (/Android/i.test(userAgent)) return 'android'
  if (/iPhone|iPad|iPod/i.test(userAgent)) return 'ios'
  return null
}

export function directionsOptions(latitude: number, longitude: number, userAgent = ''): DirectionsOption[] {
  const at = `${latitude},${longitude}`
  const options: DirectionsOption[] = []

  const platform = detectMobilePlatform(userAgent)
  if (platform === 'android') {
    options.push({ id: 'device', label: 'برنامه‌های مسیریابی گوشی', url: `geo:${at}?q=${at}` })
  } else if (platform === 'ios') {
    options.push({ id: 'device', label: 'برنامه‌های مسیریابی گوشی', url: `https://maps.apple.com/?daddr=${at}` })
  }

  options.push(
    { id: 'neshan', label: 'نشان', url: `https://neshan.org/maps/@${at},16z` },
    { id: 'balad', label: 'بلد', url: `https://balad.ir/location?latitude=${latitude}&longitude=${longitude}&zoom=16` },
    { id: 'google', label: 'گوگل‌مپ', url: `https://www.google.com/maps/dir/?api=1&destination=${at}` },
    { id: 'waze', label: 'ویز', url: `https://waze.com/ul?ll=${at}&navigate=yes` },
  )
  return options
}

/** With no coordinates on file, the address text is all there is: a Google Maps search for it. */
export function addressSearchUrl(address: string): string {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`
}
