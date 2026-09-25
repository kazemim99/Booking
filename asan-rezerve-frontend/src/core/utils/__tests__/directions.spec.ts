import { describe, it, expect } from 'vitest'
import { addressSearchUrl, detectMobilePlatform, directionsOptions } from '../directions'

/**
 * reviews-and-reschedule-round2 item 5: «مسیریابی» first offers the phone's own chooser of installed map apps, then
 * نشان / بلد / گوگل‌مپ / ویز.
 */

const ANDROID = 'Mozilla/5.0 (Linux; Android 14; SM-A546E) AppleWebKit/537.36 Chrome/126.0 Mobile Safari/537.36'
const IPHONE = 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 Version/17.5 Mobile/15E148 Safari/604.1'
const DESKTOP = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36'

describe('detectMobilePlatform', () => {
  it('knows Android, iOS, and everything else', () => {
    expect(detectMobilePlatform(ANDROID)).toBe('android')
    expect(detectMobilePlatform(IPHONE)).toBe('ios')
    expect(detectMobilePlatform('Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X)')).toBe('ios')
    expect(detectMobilePlatform(DESKTOP)).toBeNull()
  })
})

describe('directionsOptions', () => {
  const lat = 35.6892
  const lng = 51.389

  it('on Android, offers the phone’s chooser first as a geo: link', () => {
    const [first] = directionsOptions(lat, lng, ANDROID)
    expect(first).toEqual({ id: 'device', label: 'برنامه‌های مسیریابی گوشی', url: 'geo:35.6892,51.389?q=35.6892,51.389' })
  })

  it('on iOS, offers Apple Maps first', () => {
    const [first] = directionsOptions(lat, lng, IPHONE)
    expect(first.id).toBe('device')
    expect(first.url).toBe('https://maps.apple.com/?daddr=35.6892,51.389')
  })

  it('on a desktop there is no phone chooser', () => {
    expect(directionsOptions(lat, lng, DESKTOP).map((o) => o.id)).toEqual(['neshan', 'balad', 'google', 'waze'])
  })

  it('then نشان, بلد, گوگل‌مپ and ویز, each with the salon’s coordinates', () => {
    const options = directionsOptions(lat, lng, ANDROID)
    expect(options.map((o) => o.label)).toEqual(['برنامه‌های مسیریابی گوشی', 'نشان', 'بلد', 'گوگل‌مپ', 'ویز'])
    expect(options.slice(1).map((o) => o.url)).toEqual([
      'https://neshan.org/maps/@35.6892,51.389,16z',
      'https://balad.ir/location?latitude=35.6892&longitude=51.389&zoom=16',
      'https://www.google.com/maps/dir/?api=1&destination=35.6892,51.389',
      'https://waze.com/ul?ll=35.6892,51.389&navigate=yes',
    ])
  })
})

describe('addressSearchUrl', () => {
  it('searches the address text when there are no coordinates', () => {
    expect(addressSearchUrl('خیابان ولیعصر، تهران')).toBe(
      `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent('خیابان ولیعصر، تهران')}`,
    )
  })
})
