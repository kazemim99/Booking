import { describe, expect, it } from 'vitest'
import { persianServerMessage, serverMessage } from '../server-message'

/** The customer's words from any of the API's error shapes (openspec/changes/_inline/customer-reviews-and-nahal-seed). */
describe('serverMessage', () => {
  it('reads the host envelope', () => {
    expect(serverMessage({ success: false, message: 'به نظر خودتان نمی‌توانید رأی بدهید.' })).toBe(
      'به نظر خودتان نمی‌توانید رأی بدهید.',
    )
  })

  it("reads a controller's list of errors — never the object printed", () => {
    expect(
      serverMessage({ success: false, errors: [{ code: 'ERR_CONFLICT', message: 'برای این نوبت قبلاً نظر ثبت کرده‌اید.' }] }),
    ).toBe('برای این نوبت قبلاً نظر ثبت کرده‌اید.')
  })

  it("reads a validation failure's field words, not the English headline", () => {
    expect(
      serverMessage({
        message: "Validation failed for property 'Comment': متن نظر باید دست‌کم ۱۰ نویسه باشد.",
        error: { errors: { Comment: ['متن نظر باید دست‌کم ۱۰ نویسه باشد.'] } },
      }),
    ).toBe('متن نظر باید دست‌کم ۱۰ نویسه باشد.')
  })

  it('keeps a state refusal message over the metadata beside it', () => {
    expect(
      serverMessage({ message: 'فقط به نظرهای منتشرشده می‌توانید رأی بدهید.', error: { errors: { aggregateName: ['Review'] } } }),
    ).toBe('فقط به نظرهای منتشرشده می‌توانید رأی بدهید.')
  })

  it('has nothing to say for nothing', () => {
    expect(serverMessage(null)).toBeNull()
    expect(serverMessage('<html>')).toBeNull()
    expect(serverMessage({ success: false })).toBeNull()
  })

  it('only passes Persian text on to customers', () => {
    expect(persianServerMessage({ message: 'Booking with ID 1 not found' })).toBeNull()
    expect(persianServerMessage({ message: 'این نوبت پیدا نشد.' })).toBe('این نوبت پیدا نشد.')
  })
})
