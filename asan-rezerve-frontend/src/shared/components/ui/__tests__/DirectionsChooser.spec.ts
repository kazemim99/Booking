import { describe, it, expect, afterEach } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import DirectionsChooser from '../DirectionsChooser.vue'

/**
 * reviews-and-reschedule-round2 item 5: «مسیریابی» asks which app — the phone's own chooser first (on a phone), then
 * نشان / بلد / گوگل‌مپ / ویز — in an accessible dialog.
 */

const ANDROID = 'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/126.0 Mobile Safari/537.36'
const DESKTOP = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36'

let wrapper: VueWrapper | null = null
afterEach(() => {
  wrapper?.unmount()
  wrapper = null
  document.body.innerHTML = ''
})

const open = (userAgent: string) => {
  wrapper = mount(DirectionsChooser, {
    props: { isOpen: true, latitude: 35.7, longitude: 51.4, userAgent },
    attachTo: document.body,
  })
  return wrapper
}

const dialog = () => document.body.querySelector('[role="dialog"]') as HTMLElement
const links = () => Array.from(dialog().querySelectorAll('a')) as HTMLAnchorElement[]

describe('DirectionsChooser', () => {
  it('is a titled dialog asking which app', () => {
    open(DESKTOP)
    expect(dialog()).not.toBeNull()
    expect(dialog().textContent).toContain('با کدام برنامه مسیریابی شود؟')
  })

  it('on a phone, the phone’s own chooser comes first, then the web apps', () => {
    open(ANDROID)
    expect(links().map((a) => a.querySelector('.directions-chooser__label')!.textContent)).toEqual([
      'برنامه‌های مسیریابی گوشی',
      'نشان',
      'بلد',
      'گوگل‌مپ',
      'ویز',
    ])
    expect(links()[0].getAttribute('href')).toBe('geo:35.7,51.4?q=35.7,51.4')
    expect(links()[1].getAttribute('target')).toBe('_blank')
  })

  it('on a desktop, there is no phone chooser', () => {
    open(DESKTOP)
    expect(links()[0].getAttribute('data-test')).toBe('directions-neshan')
  })

  it('the first option takes focus when it opens', async () => {
    open(ANDROID)
    await wrapper!.vm.$nextTick()
    expect(document.activeElement).toBe(links()[0])
  })

  it('Esc closes it', async () => {
    open(DESKTOP)
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(wrapper!.emitted('close')).toHaveLength(1)
  })

  it('choosing an app closes it', async () => {
    open(DESKTOP)
    links()[0].dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }))
    expect(wrapper!.emitted('close')).toHaveLength(1)
  })
})
