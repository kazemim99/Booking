import { test } from 'node:test'
import assert from 'node:assert/strict'
import { createAdminClient } from '../src/client.mjs'
import { defineTools } from '../src/tools.mjs'

/** A fetch that records calls and answers from a table. */
function fakeFetch(responses) {
  const calls = []
  const fetchImpl = async (url, init) => {
    calls.push({ url: String(url), method: init.method, headers: init.headers, body: init.body ? JSON.parse(init.body) : undefined })
    const path = new URL(url).pathname
    const answer = responses[`${init.method} ${path}`] ?? { status: 404, body: { message: 'not found' } }
    return new Response(answer.body === undefined ? null : JSON.stringify(answer.body), { status: answer.status ?? 200 })
  }
  return { fetchImpl, calls }
}

const envelope = (data) => ({ success: true, statusCode: 200, message: 'ok', data, metadata: {} })
const BASE = 'https://api.example/api/v1'

test('read-only by default: no tool changes the running system', () => {
  const client = createAdminClient({ baseUrl: BASE, token: 't', fetchImpl: async () => new Response('{}') })

  const names = defineTools(client).map((t) => t.name)

  assert.deepEqual(names.sort(), ['get_cache_stats', 'get_digest', 'get_log_event', 'get_overview', 'get_trace', 'list_log_levels', 'search_logs'])
  assert.ok(defineTools(client).every((t) => t.readOnly))
})

test('writes are offered only when allowed', () => {
  const client = createAdminClient({ baseUrl: BASE, token: 't', fetchImpl: async () => new Response('{}') })

  const names = defineTools(client, { allowWrites: true }).map((t) => t.name)

  assert.ok(names.includes('set_log_level') && names.includes('reset_log_level') && names.includes('invalidate_cache'))
})

test('search_logs calls the admin API with the bearer token and unwraps the envelope', async () => {
  const { fetchImpl, calls } = fakeFetch({
    'GET /api/v1/admin/observability/logs': { body: envelope({ items: [{ id: 1 }], totalCount: 1, page: 1, pageSize: 50 }) },
  })
  const tools = defineTools(createAdminClient({ baseUrl: BASE + '/', token: 'abc', fetchImpl }))

  const result = await tools.find((t) => t.name === 'search_logs').handler({ minLevel: 'Error', search: 'booking', statusCode: undefined })

  assert.equal(calls[0].method, 'GET')
  assert.equal(calls[0].url, `${BASE}/admin/observability/logs?minLevel=Error&search=booking`)
  assert.equal(calls[0].headers.Authorization, 'Bearer abc')
  assert.equal(JSON.parse(result.content[0].text).totalCount, 1)
})

test('get_digest returns the markdown itself', async () => {
  const { fetchImpl, calls } = fakeFetch({
    'GET /api/v1/admin/observability/digest': { body: envelope({ markdown: '# AsanRezerve system digest' }) },
  })
  const tools = defineTools(createAdminClient({ baseUrl: BASE, token: 't', fetchImpl }))

  const result = await tools.find((t) => t.name === 'get_digest').handler({ source: 'AsanRezerve.ServiceCatalog' })

  assert.equal(result.content[0].text, '# AsanRezerve system digest')
  assert.match(calls[0].url, /source=AsanRezerve\.ServiceCatalog/)
  assert.match(calls[0].url, /format=markdown/)
})

test('get_trace encodes the trace id into the path', async () => {
  const { fetchImpl, calls } = fakeFetch({
    'GET /api/v1/admin/observability/logs/trace/4bf92f35': { body: envelope([]) },
  })
  const tools = defineTools(createAdminClient({ baseUrl: BASE, token: 't', fetchImpl }))

  await tools.find((t) => t.name === 'get_trace').handler({ traceId: '4bf92f35' })

  assert.equal(calls[0].url, `${BASE}/admin/observability/logs/trace/4bf92f35`)
})

test('set_log_level sends the category, level and duration', async () => {
  const { fetchImpl, calls } = fakeFetch({
    'PUT /api/v1/admin/observability/log-levels': { body: envelope({ category: 'AsanRezerve', effectiveLevel: 'Debug' }) },
  })
  const tools = defineTools(createAdminClient({ baseUrl: BASE, token: 't', fetchImpl }), { allowWrites: true })

  await tools.find((t) => t.name === 'set_log_level').handler({ category: 'AsanRezerve', level: 'Debug', durationMinutes: 15 })
  await tools.find((t) => t.name === 'set_log_level').handler({ category: 'Default', level: 'Warning' })

  assert.deepEqual(calls[0].body, { category: 'AsanRezerve', level: 'Debug', durationMinutes: 15 })
  assert.deepEqual(calls[1].body, { category: 'Default', level: 'Warning', durationMinutes: null })
})

test('invalidate_cache refuses an empty request', async () => {
  const tools = defineTools(createAdminClient({ baseUrl: BASE, token: 't', fetchImpl: async () => new Response(null, { status: 204 }) }), { allowWrites: true })

  await assert.rejects(() => tools.find((t) => t.name === 'invalidate_cache').handler({}), /Give a tag/)
})

test('an expired token becomes an actionable message', async () => {
  const tools = defineTools(createAdminClient({ baseUrl: BASE, token: 'old', fetchImpl: async () => new Response(null, { status: 401 }) }))

  await assert.rejects(() => tools.find((t) => t.name === 'get_overview').handler({}), /ASANREZERVE_ADMIN_TOKEN/)
})

test('a non-admin token is named as such', async () => {
  const tools = defineTools(createAdminClient({ baseUrl: BASE, token: 'x', fetchImpl: async () => new Response(null, { status: 403 }) }))

  await assert.rejects(() => tools.find((t) => t.name === 'list_log_levels').handler({}), /AdminOnly/)
})

test('a missing configuration fails with what to set', () => {
  assert.throws(() => createAdminClient({ baseUrl: '', token: 't' }), /ASANREZERVE_API_URL/)
  assert.throws(() => createAdminClient({ baseUrl: BASE, token: '' }), /ASANREZERVE_ADMIN_TOKEN/)
})
