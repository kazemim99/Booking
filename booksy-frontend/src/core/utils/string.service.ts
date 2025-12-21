/**
 * String formatting and manipulation utilities
 * Centralized string operations used across components
 */

/**
 * Get initials from a full name
 * @param fullName - Full name (e.g., "John Doe")
 * @returns Initials (e.g., "JD")
 */
export function getNameInitials(fullName: string): string {
  if (!fullName || !fullName.trim()) return '??'

  const names = fullName.trim().split(/\s+/)
  if (names.length >= 2) {
    return `${names[0][0]}${names[1][0]}`.toUpperCase()
  }
  return fullName.substring(0, 2).toUpperCase()
}

/**
 * Capitalize first letter of a string
 * @param text - Text to capitalize
 * @returns Capitalized text
 */
export function capitalize(text: string): string {
  if (!text) return ''
  return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase()
}

/**
 * Capitalize each word in a string
 * @param text - Text to capitalize
 * @returns Title-cased text
 */
export function titleCase(text: string): string {
  if (!text) return ''
  return text
    .split(/\s+/)
    .map(word => capitalize(word))
    .join(' ')
}

/**
 * Convert camelCase to human-readable format
 * @param camelCase - camelCase string
 * @returns Human-readable format (e.g., "camelCase" -> "Camel Case")
 */
export function camelCaseToHuman(camelCase: string): string {
  if (!camelCase) return ''
  return camelCase
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (str) => str.toUpperCase())
    .trim()
}

/**
 * Convert snake_case to human-readable format
 * @param snakeCase - snake_case string
 * @returns Human-readable format (e.g., "snake_case" -> "Snake Case")
 */
export function snakeCaseToHuman(snakeCase: string): string {
  if (!snakeCase) return ''
  return snakeCase
    .split('_')
    .map(word => capitalize(word))
    .join(' ')
}

/**
 * Truncate text with ellipsis
 * @param text - Text to truncate
 * @param maxLength - Maximum length before truncation
 * @param suffix - Suffix to append (default: "...")
 * @returns Truncated text
 */
export function truncate(text: string, maxLength: number = 50, suffix: string = '...'): string {
  if (!text) return ''
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength - suffix.length) + suffix
}

/**
 * Remove leading and trailing whitespace
 * @param text - Text to trim
 * @returns Trimmed text
 */
export function trim(text: string): string {
  return text ? text.trim() : ''
}

/**
 * Remove all whitespace
 * @param text - Text to process
 * @returns Text without whitespace
 */
export function removeWhitespace(text: string): string {
  return text ? text.replace(/\s+/g, '') : ''
}

/**
 * Repeat a string
 * @param text - Text to repeat
 * @param count - Number of repetitions
 * @returns Repeated text
 */
export function repeat(text: string, count: number): string {
  return text.repeat(Math.max(0, count))
}

/**
 * Pad string to a certain length
 * @param text - Text to pad
 * @param length - Target length
 * @param padChar - Character to pad with (default: " ")
 * @returns Padded text
 */
export function padStart(text: string, length: number, padChar: string = ' '): string {
  return text.padStart(length, padChar)
}

/**
 * Pad string to end to a certain length
 * @param text - Text to pad
 * @param length - Target length
 * @param padChar - Character to pad with (default: " ")
 * @returns Padded text
 */
export function padEnd(text: string, length: number, padChar: string = ' '): string {
  return text.padEnd(length, padChar)
}

/**
 * Check if string is empty or whitespace only
 * @param text - Text to check
 * @returns True if empty or whitespace only
 */
export function isEmpty(text: string): boolean {
  return !text || !text.trim()
}

/**
 * Check if string contains any text
 * @param text - Text to check
 * @returns True if contains text
 */
export function isNotEmpty(text: string): boolean {
  return !isEmpty(text)
}

/**
 * Reverse a string
 * @param text - Text to reverse
 * @returns Reversed text
 */
export function reverse(text: string): string {
  return text ? text.split('').reverse().join('') : ''
}

/**
 * Check if string starts with substring (case-insensitive)
 * @param text - Text to check
 * @param substring - Substring to find
 * @returns True if text starts with substring
 */
export function startsWithIgnoreCase(text: string, substring: string): boolean {
  return text ? text.toLowerCase().startsWith(substring.toLowerCase()) : false
}

/**
 * Check if string ends with substring (case-insensitive)
 * @param text - Text to check
 * @param substring - Substring to find
 * @returns True if text ends with substring
 */
export function endsWithIgnoreCase(text: string, substring: string): boolean {
  return text ? text.toLowerCase().endsWith(substring.toLowerCase()) : false
}

/**
 * Check if string includes substring (case-insensitive)
 * @param text - Text to check
 * @param substring - Substring to find
 * @returns True if text includes substring
 */
export function includesIgnoreCase(text: string, substring: string): boolean {
  return text ? text.toLowerCase().includes(substring.toLowerCase()) : false
}

/**
 * Replace all occurrences of a substring
 * @param text - Original text
 * @param search - Substring to find
 * @param replacement - Replacement string
 * @returns Text with replacements
 */
export function replaceAll(text: string, search: string, replacement: string): string {
  if (!text) return ''
  return text.split(search).join(replacement)
}
