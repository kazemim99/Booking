/**
 * The words an error response carries for the customer, whichever of the API's three shapes it came in — null when
 * it carries none (openspec/changes/_inline/customer-reviews-and-nahal-seed):
 *
 * - the host's envelope: `{ message, error: { code, message, errors: { field: [..] } } }`;
 * - a controller's own `{ success: false, errors: [ { code, message, field } ] }` (creating a review answers this way);
 * - a bare `{ message }` or `{ errors: { field: [..] } }`.
 *
 * A validation failure's headline is the server's English wrapper («Validation failed for property …»); the
 * customer's words are in the per-field list, so that is read instead.
 */
export function serverMessage(data: unknown): string | null {
  if (!data || typeof data !== 'object') return null
  const body = data as Record<string, unknown>

  const errors = body.errors
  if (Array.isArray(errors) && errors.length > 0) {
    const first = errors[0]
    const text = typeof first === 'object' && first ? text_((first as Record<string, unknown>).message) : text_(first)
    if (text) return text
  }

  const error = body.error && typeof body.error === 'object' ? (body.error as Record<string, unknown>) : null
  const message = text_(body.message) ?? text_(error?.message)
  if (!message || message.startsWith('Validation failed')) {
    const fields = errors && typeof errors === 'object' && !Array.isArray(errors) ? errors : error?.errors
    if (fields && typeof fields === 'object') {
      for (const value of Object.values(fields as Record<string, unknown>)) {
        const text = Array.isArray(value) ? text_(value[0]) : text_(value)
        if (text) return text
      }
    }
  }
  return message
}

/** The server's reason, when it is written for a Persian reader; English (developer) text is not shown to customers. */
export function persianServerMessage(data: unknown): string | null {
  const message = serverMessage(data)
  return message && /[؀-ۿ]/.test(message) ? message : null
}

function text_(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value.trim() : null
}
