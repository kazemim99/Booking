import { describe, expect, it } from 'vitest'
import { toLoginEmail } from '../login-identifier'

// The backend signs in by email only. The admin asked to type a plain username
// ("kazemi.mst"), so the admin panel maps a value without "@" to the account's
// email on the product domain (decision 2026-09-19); a full email passes through.
describe('toLoginEmail', () => {
  it('turns a bare username into the account email', () => {
    expect(toLoginEmail('kazemi.mst')).toBe('kazemi.mst@nahalkmi.ir')
  })

  it('leaves a full email untouched', () => {
    expect(toLoginEmail('someone@example.com')).toBe('someone@example.com')
  })

  it('ignores surrounding spaces and letter case, as mobile keyboards add them', () => {
    expect(toLoginEmail('  Kazemi.MST ')).toBe('kazemi.mst@nahalkmi.ir')
  })

  it('leaves an empty value empty, so "required" validation still fires', () => {
    expect(toLoginEmail('   ')).toBe('')
  })
})
