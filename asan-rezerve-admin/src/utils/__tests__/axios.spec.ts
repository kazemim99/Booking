import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('ant-design-vue', () => ({ message: { error: vi.fn(), success: vi.fn(), warning: vi.fn() } }))

import apiClient from '../axios'

/**
 * The API wraps every 2xx body in { success, statusCode, message, data, metadata }
 * (ApiResponseMiddleware). Only the two paginated endpoints ever unwrapped it; every other
 * call handed the envelope itself to the UI as though it were the resource.
 *
 * Concretely, that is why a provider's gallery rendered empty even for a provider with real
 * uploaded images: `getProviderById` returned the envelope, so `provider.id` was undefined
 * and the follow-up gallery request was issued for `undefined`.
 */
const runResponseInterceptor = (body: unknown) => {
  // Interceptor handlers are registered in order; ours is the first response interceptor.
  const [handler] = (apiClient.interceptors.response as unknown as {
    handlers: Array<{ fulfilled: (r: unknown) => unknown }>
  }).handlers

  return handler!.fulfilled({ data: body, status: 200, config: {}, headers: {} }) as { data: unknown }
}

const envelope = (data: unknown) => ({
  success: true,
  statusCode: 200,
  message: 'Request completed successfully',
  data,
  metadata: { requestId: 'x', timestamp: '2026-08-11T00:00:00Z' },
})

describe('api envelope unwrapping', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('hands the caller the payload, not the envelope', () => {
    const provider = { id: 'a8346c06-b292-46cb-8951-d1f8bf711ed2', businessName: 'آرایشگاه نهال' }

    const result = runResponseInterceptor(envelope(provider))

    expect(result.data).toEqual(provider)
  })

  it('exposes the resource id that the gallery lookup depends on', () => {
    const result = runResponseInterceptor(
      envelope({ id: 'a8346c06-b292-46cb-8951-d1f8bf711ed2', businessName: 'آرایشگاه نهال' }),
    )

    // Pre-fix this read `undefined`, so the gallery was fetched for `/providers/undefined/gallery`.
    expect((result.data as { id: string }).id).toBe('a8346c06-b292-46cb-8951-d1f8bf711ed2')
  })

  it('unwraps array payloads such as a provider gallery', () => {
    const images = [{ id: 'img-1', isPrimary: true }, { id: 'img-2', isPrimary: false }]

    const result = runResponseInterceptor(envelope(images))

    expect(result.data).toEqual(images)
    expect(Array.isArray(result.data)).toBe(true)
  })

  it('unwraps a paginated payload without flattening it', () => {
    const page = { items: [{ id: 'p1' }], totalCount: 15, pageNumber: 1, pageSize: 10, totalPages: 2 }

    const result = runResponseInterceptor(envelope(page))

    expect(result.data).toEqual(page)
  })

  it('preserves a null payload rather than substituting the envelope', () => {
    const result = runResponseInterceptor(envelope(null))

    expect(result.data).toBeNull()
  })

  it('leaves an unenveloped body untouched', () => {
    // 204s and any non-wrapped response must pass through unchanged.
    const raw = { id: 'p1', businessName: 'Salon' }

    expect(runResponseInterceptor(raw).data).toEqual(raw)
  })

  it('does not unwrap a payload that merely has a data property', () => {
    // Envelope detection requires all three markers; a resource with its own `data`
    // field must not be mistaken for an envelope and silently gutted.
    const resource = { id: 'p1', data: 'something' }

    expect(runResponseInterceptor(resource).data).toEqual(resource)
  })

  it('leaves an empty-string body (204 No Content) alone', () => {
    expect(runResponseInterceptor('').data).toBe('')
  })
})
