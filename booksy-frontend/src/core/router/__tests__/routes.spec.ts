import { describe, it, expect } from 'vitest'
import { createRouter, createMemoryHistory, type RouteRecordRaw } from 'vue-router'

import { routes } from '@/core/router'

/**
 * Regression protection for a live defect: `/customer/my-bookings` and
 * `/customer/providers` both 404'd for every signed-in customer.
 *
 * Cause: route NAMES must be globally unique. vue-router's `addRoute()` *removes* any
 * existing record carrying the same name, and `createRouter` adds records in array
 * order — so `provider.routes.ts` (spread after `customer.routes.ts`) reusing the names
 * `MyBookings` and `ProviderList` silently deleted the customer records at startup. The
 * paths then fell through to the `/:pathMatch(.*)*` catch-all.
 *
 * Nothing failed loudly: no warning, no build error — the routes were simply gone. These
 * tests pin both the general rule and the two specific paths that regressed.
 */

/** Every named record in the tree, as `name` → the paths that claim it. */
function collectNames(
  records: readonly RouteRecordRaw[],
  parentPath = '',
  acc = new Map<string, string[]>(),
): Map<string, string[]> {
  for (const record of records) {
    const fullPath = record.path.startsWith('/')
      ? record.path
      : `${parentPath.replace(/\/$/, '')}/${record.path}`

    if (record.name) {
      const key = String(record.name)
      acc.set(key, [...(acc.get(key) ?? []), fullPath])
    }
    if (record.children?.length) {
      collectNames(record.children, fullPath, acc)
    }
  }
  return acc
}

describe('router route table', () => {
  it('never reuses a route name (a duplicate silently deletes the earlier route)', () => {
    const duplicates = [...collectNames(routes)]
      .filter(([, paths]) => paths.length > 1)
      .map(([name, paths]) => `${name} -> ${paths.join(', ')}`)

    expect(duplicates).toEqual([])
  })

  // Guard-free router: we are asserting the route TABLE, not navigation/auth behaviour.
  const resolver = createRouter({ history: createMemoryHistory(), routes })

  it.each([
    ['/customer/my-bookings', 'MyBookings'],
    ['/customer/providers', 'ProviderList'],
    ['/provider/my-bookings', 'StaffMyBookings'],
    ['/providers', 'ProviderBrowse'],
  ])('resolves %s to the %s record rather than the 404 catch-all', (path, expectedName) => {
    const resolved = resolver.resolve(path)

    expect(resolved.name).toBe(expectedName)
    expect(resolved.matched.at(-1)?.path).not.toBe('/:pathMatch(.*)*')
  })
})
