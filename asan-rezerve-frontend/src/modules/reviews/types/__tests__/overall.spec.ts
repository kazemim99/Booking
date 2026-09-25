import { describe, it, expect } from 'vitest'
import { overallFromAspects } from '../reviews.types'

/**
 * reviews-and-reschedule-round2, D1: there is no separate overall any more — it is the four aspects' average, to the
 * nearest half star, the same rule the API applies when an overall is not sent.
 */
describe('overallFromAspects', () => {
  it.each([
    [{ cleanliness: 5, skill: 5, punctuality: 5, conduct: 5 }, 5],
    [{ cleanliness: 4, skill: 3, punctuality: 3, conduct: 3 }, 3.5], // 3.25 rounds up to the half
    [{ cleanliness: 4, skill: 4, punctuality: 4, conduct: 3 }, 4], // 3.75 rounds up to the whole
    [{ cleanliness: 5, skill: 4, punctuality: 4, conduct: 5 }, 4.5],
    [{ cleanliness: 1, skill: 1, punctuality: 1, conduct: 2 }, 1.5], // 1.25
  ])('%o → %d', (aspects, overall) => {
    expect(overallFromAspects(aspects)).toBe(overall)
  })

  it('is null until all four are rated', () => {
    expect(overallFromAspects({ cleanliness: 5, skill: 4, punctuality: 4 })).toBeNull()
    expect(overallFromAspects({ cleanliness: 5, skill: 4, punctuality: 4, conduct: null })).toBeNull()
    expect(overallFromAspects({})).toBeNull()
  })
})
