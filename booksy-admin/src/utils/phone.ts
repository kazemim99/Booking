// Left-to-right isolate ... pop: keeps the digits (and any "+") in reading order inside the
// right-to-left admin layout, where "+989…" otherwise displays as "989…+".
const LRI = '⁦'
const PDI = '⁩'

/**
 * A phone number as admins read it. Numbers are stored in E.164 (+989123135143); an Iranian number
 * is shown in the local form everyone reads and dials (0912 313 5143). Anything else is shown as
 * given. Always direction-isolated. (Admin feedback, 2026-09-19.)
 */
export function formatPhone(phone: string | null | undefined): string {
  if (!phone) return ''
  const digits = phone.replace(/[^\d+]/g, '')

  let national: string | null = null
  if (digits.startsWith('+98')) national = '0' + digits.slice(3)
  else if (digits.startsWith('0098')) national = '0' + digits.slice(4)
  else if (/^98\d{10}$/.test(digits)) national = '0' + digits.slice(2)
  else if (/^0\d{10}$/.test(digits)) national = digits

  const shown =
    national && /^0\d{10}$/.test(national)
      ? `${national.slice(0, 4)} ${national.slice(4, 7)} ${national.slice(7)}`
      : phone.trim()

  return `${LRI}${shown}${PDI}`
}
